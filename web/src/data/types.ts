/** Mirrors Lld.Contracts.AppInfo. */
export interface AppInfo {
  name: string;
  version: string;
  environment: string;
}

/** Mirrors Lld.Contracts.UserSummary. */
export interface UserSummary {
  id: string;
  email: string;
  displayName: string;
  roles: string[];
  mustChangePassword: boolean;
}

export interface AccessRequestInput {
  email: string;
  name: string;
  note?: string;
}

/** Sent now, or saved on this device until the server is online (spec 002). */
export interface AccessRequestOutcome {
  queued: boolean;
}

/** What the app believes about the server (mirrors Lld.App ServerStatusView). */
export interface ServerStatus {
  state: 'unknown' | 'online' | 'offline';
  /** e.g. startup, shutdown, quota, unreachable, stale, no_signal, assumed */
  reason: string;
  since: string;
  /** Set when the server stopped for its monthly request budget. */
  quotaResetsAt?: string;
}

/** A failure from the API or the native bridge, with the stable error code (e.g. `auth.locked_out`). */
export class AppError extends Error {
  readonly code: string;
  readonly status?: number;

  constructor(code: string, message: string, status?: number) {
    super(message);
    this.name = 'AppError';
    this.code = code;
    this.status = status;
  }
}

/**
 * The only way UI code reads or writes data. Two strategies implement it:
 * HttpDataSource (browser, talks to the API) and BridgeDataSource (inside the MAUI app).
 * Implementations own token storage; the UI never sees a token.
 */
export interface DataSource {
  readonly kind: 'http' | 'bridge';
  getInfo(): Promise<AppInfo>;

  /** Sends the request, or saves it on the device while the server is offline. */
  requestAccess(input: AccessRequestInput): Promise<AccessRequestOutcome>;
  login(email: string, password: string): Promise<UserSummary>;
  /** Restores a session from a stored refresh token; null when signed out. */
  restoreSession(): Promise<UserSummary | null>;
  changePassword(currentPassword: string, newPassword: string): Promise<UserSummary>;
  logout(): Promise<void>;
  logoutAll(): Promise<void>;

  getServerStatus(): Promise<ServerStatus>;
  /** Live status changes; returns an unsubscribe function. */
  onServerStatusChanged(listener: (status: ServerStatus) => void): () => void;
  /** A request saved while offline reached the server. */
  onAccessRequestSent(listener: (email: string) => void): () => void;
}

/** Shape of window.HybridWebView, injected by _framework/hybridwebview.js inside the MAUI app. */
export interface HybridWebViewBridge {
  InvokeDotNet(methodName: string, paramValues?: unknown[]): Promise<unknown>;
}

declare global {
  interface Window {
    HybridWebView?: HybridWebViewBridge;
  }
}
