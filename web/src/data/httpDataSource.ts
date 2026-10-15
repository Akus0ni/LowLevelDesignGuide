import { AccessRequestOutbox } from './serverStatus/accessRequestOutbox';
import { ServerStatusTracker } from './serverStatus/serverStatusTracker';
import {
  AppError,
  type AccessRequestInput,
  type AccessRequestOutcome,
  type AppInfo,
  type DataSource,
  type ServerStatus,
  type UserSummary,
} from './types';

type Fetch = typeof fetch;

/** Status wiring for browser dev mode; without a status source the server is assumed online. */
export interface HttpStatusOptions {
  /** Starts listening (e.g. ntfy) and returns a stop function. */
  connectStatus?: (tracker: ServerStatusTracker) => () => void;
  tracker?: ServerStatusTracker;
  outbox?: AccessRequestOutbox;
}

const EVALUATE_INTERVAL_MS = 60_000;
const GATEWAY_ERRORS = new Set([502, 503, 504]);
const NETWORK_ERROR = 'network.unavailable';

interface AuthResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  user: UserSummary;
}

/**
 * Browser strategy. The access token lives only in memory; the refresh token is an httpOnly cookie the
 * API sets (we tell it we're the web client), so scripts can never read it.
 */
export class HttpDataSource implements DataSource {
  readonly kind = 'http';
  private readonly baseUrl: string;
  private readonly fetchFn: Fetch;
  private accessToken: string | null = null;
  private refreshInFlight: Promise<UserSummary | null> | null = null;
  private readonly tracker: ServerStatusTracker;
  private readonly outbox: AccessRequestOutbox;
  private readonly connectStatus?: (tracker: ServerStatusTracker) => () => void;
  private readonly sentListeners = new Set<(email: string) => void>();
  private stopStatus: (() => void) | null = null;
  private statusSubscribers = 0;

  constructor(baseUrl = '', fetchFn: Fetch = (...args) => fetch(...args), options: HttpStatusOptions = {}) {
    this.baseUrl = baseUrl;
    this.fetchFn = fetchFn;
    this.connectStatus = options.connectStatus;
    this.tracker =
      options.tracker ?? new ServerStatusTracker(Date.now, options.connectStatus ? 'unknown' : 'online');
    this.outbox = options.outbox ?? new AccessRequestOutbox();
    this.tracker.subscribe((status) => {
      if (status.state === 'online') void this.flushOutbox();
    });
  }

  getServerStatus(): Promise<ServerStatus> {
    return Promise.resolve(this.tracker.current);
  }

  /** The first subscriber starts listening for status (and sends anything saved); the last one stops it. */
  onServerStatusChanged(listener: (status: ServerStatus) => void): () => void {
    const unsubscribe = this.tracker.subscribe(listener);
    if (this.statusSubscribers++ === 0) this.startStatus();
    return () => {
      unsubscribe();
      if (--this.statusSubscribers === 0) this.stopStatus?.();
    };
  }

  onAccessRequestSent(listener: (email: string) => void): () => void {
    this.sentListeners.add(listener);
    return () => this.sentListeners.delete(listener);
  }

  async getInfo(): Promise<AppInfo> {
    return (await this.send('GET', '/api/info')) as AppInfo;
  }

  async requestAccess(input: AccessRequestInput): Promise<AccessRequestOutcome> {
    if (this.tracker.current.state !== 'offline') {
      try {
        await this.send('POST', '/api/access-requests', input);
        return { queued: false };
      } catch (error) {
        if (!isUnreachable(error)) throw error;
      }
    }
    this.outbox.enqueue(input);
    return { queued: true };
  }

  async login(email: string, password: string): Promise<UserSummary> {
    return this.acceptSession(await this.send('POST', '/api/auth/login', { email, password }));
  }

  restoreSession(): Promise<UserSummary | null> {
    // Single-flight: concurrent callers share one refresh, so rotation never trips reuse detection.
    this.refreshInFlight ??= this.refresh().finally(() => (this.refreshInFlight = null));
    return this.refreshInFlight;
  }

  async changePassword(currentPassword: string, newPassword: string): Promise<UserSummary> {
    return this.acceptSession(
      await this.sendAuthorized('POST', '/api/auth/change-password', { currentPassword, newPassword }),
    );
  }

  async logout(): Promise<void> {
    await this.send('POST', '/api/auth/logout');
    this.accessToken = null;
  }

  async logoutAll(): Promise<void> {
    await this.sendAuthorized('POST', '/api/auth/logout-all');
    this.accessToken = null;
  }

  private startStatus(): void {
    const stopSource = this.connectStatus?.(this.tracker);
    const timer = setInterval(() => this.tracker.evaluate(), EVALUATE_INTERVAL_MS);
    void this.flushOutbox();
    this.stopStatus = () => {
      stopSource?.();
      clearInterval(timer);
      this.stopStatus = null;
    };
  }

  private flushOutbox(): Promise<void> {
    return this.outbox.flush(
      async (request) => {
        try {
          await this.send('POST', '/api/access-requests', request);
          return true;
        } catch (error) {
          if (isUnreachable(error)) return false;
          throw error;
        }
      },
      (request) => this.sentListeners.forEach((listener) => listener(request.email)),
    );
  }

  private async refresh(): Promise<UserSummary | null> {
    try {
      return this.acceptSession(await this.send('POST', '/api/auth/refresh'));
    } catch (error) {
      if (error instanceof AppError && error.status === 401) {
        this.accessToken = null;
        return null;
      }
      throw error;
    }
  }

  private acceptSession(body: unknown): UserSummary {
    const session = body as AuthResponse;
    this.accessToken = session.accessToken;
    return session.user;
  }

  /** Sends with the access token; on 401 refreshes once and retries. */
  private async sendAuthorized(method: string, path: string, body?: unknown): Promise<unknown> {
    try {
      return await this.send(method, path, body, true);
    } catch (error) {
      if (!(error instanceof AppError) || error.status !== 401) throw error;
      if (!(await this.restoreSession())) throw error;
      return this.send(method, path, body, true);
    }
  }

  private async send(method: string, path: string, body?: unknown, authorized = false): Promise<unknown> {
    const headers: Record<string, string> = { 'X-Lld-Client': 'web' };
    if (body !== undefined) headers['Content-Type'] = 'application/json';
    if (authorized && this.accessToken) headers.Authorization = `Bearer ${this.accessToken}`;

    let response: Response;
    try {
      response = await this.fetchFn(`${this.baseUrl}${path}`, {
        method,
        headers,
        body: body === undefined ? undefined : JSON.stringify(body),
        credentials: 'same-origin',
      });
    } catch {
      // A crash or power cut sends no "offline" message: a failed call is how we notice.
      this.tracker.reportCallFailed();
      throw new AppError(NETWORK_ERROR, "Can't reach the server. Check your connection and try again.");
    }

    if (GATEWAY_ERRORS.has(response.status)) this.tracker.reportCallFailed();
    else this.tracker.reportCallSucceeded();

    if (!response.ok) throw await toAppError(response);
    return response.status === 204 ? undefined : response.json();
  }
}

function isUnreachable(error: unknown): boolean {
  return (
    error instanceof AppError &&
    (error.code === NETWORK_ERROR || (error.status !== undefined && GATEWAY_ERRORS.has(error.status)))
  );
}

async function toAppError(response: Response): Promise<AppError> {
  try {
    const problem = (await response.json()) as { code?: string; title?: string };
    return new AppError(
      problem.code ?? `http.${response.status}`,
      problem.title ?? response.statusText,
      response.status,
    );
  } catch {
    return new AppError(`http.${response.status}`, `Request failed (${response.status})`, response.status);
  }
}
