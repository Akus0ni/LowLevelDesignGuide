import { AppError, type AccessRequestInput, type AppInfo, type DataSource, type UserSummary } from './types';

type Fetch = typeof fetch;

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

  constructor(baseUrl = '', fetchFn: Fetch = (...args) => fetch(...args)) {
    this.baseUrl = baseUrl;
    this.fetchFn = fetchFn;
  }

  async getInfo(): Promise<AppInfo> {
    return (await this.send('GET', '/api/info')) as AppInfo;
  }

  async requestAccess(input: AccessRequestInput): Promise<void> {
    await this.send('POST', '/api/access-requests', input);
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

    const response = await this.fetchFn(`${this.baseUrl}${path}`, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
      credentials: 'same-origin',
    });

    if (!response.ok) throw await toAppError(response);
    return response.status === 204 ? undefined : response.json();
  }
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
