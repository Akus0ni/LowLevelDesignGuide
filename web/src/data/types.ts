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

/** Mirrors Lld.Contracts.ModuleSummary (spec 003). */
export interface ModuleSummary {
  slug: string;
  title: string;
  summary: string;
  order: number;
  lessonCount: number;
}

/** Mirrors Lld.Contracts.LessonSummary. */
export interface LessonSummary {
  /** Stable id: progress, notes and bookmarks will refer to it. */
  id: string;
  moduleSlug: string;
  /** Folder name, used in routes. */
  slug: string;
  title: string;
  summary: string;
  order: number;
  readingMinutes: number;
  tags: string[];
}

/** Mirrors Lld.Contracts.ModuleDetail. */
export interface ModuleDetail {
  slug: string;
  title: string;
  summary: string;
  lessons: LessonSummary[];
}

/** An h2 or h3 of a lesson; `anchor` follows GitHub's heading-id rules (github-slugger). */
export interface OutlineEntry {
  level: 2 | 3;
  text: string;
  anchor: string;
}

/** A neighbouring lesson in the same module. */
export interface LessonLink {
  id: string;
  moduleSlug: string;
  slug: string;
  title: string;
}

/** Mirrors Lld.Contracts.LessonDetail. Links in `bodyMarkdown` are already app routes or full URLs. */
export interface LessonDetail extends LessonSummary {
  moduleTitle: string;
  bodyMarkdown: string;
  outline: OutlineEntry[];
  cheatSheetTitle: string;
  previous: LessonLink | null;
  next: LessonLink | null;
}

/** Mirrors Lld.Contracts.CheatSheetSummary. */
export interface CheatSheetSummary {
  lessonId: string;
  moduleSlug: string;
  moduleTitle: string;
  lessonSlug: string;
  lessonTitle: string;
  title: string;
}

/** Mirrors Lld.Contracts.CheatSheetDetail. */
export interface CheatSheetDetail extends CheatSheetSummary {
  bodyMarkdown: string;
}

/** Stable content error codes (Lld.Contracts.ContentErrorCodes + the app's own). */
export const ContentErrors = {
  notFound: 'content.not_found',
  invalid: 'content.invalid',
  unavailable: 'content.unavailable',
} as const;

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

  // Content (spec 003): read-only, open to everyone. In the app it comes from the device; offline is fine.
  getModules(): Promise<ModuleSummary[]>;
  /** Rejects with `content.not_found` for an unknown module. */
  getModule(slug: string): Promise<ModuleDetail>;
  getLesson(id: string): Promise<LessonDetail>;
  /** Every cheat sheet in module and lesson order, or one module's. */
  getCheatSheets(moduleSlug?: string): Promise<CheatSheetSummary[]>;
  getCheatSheet(lessonId: string): Promise<CheatSheetDetail>;
  /** Opens an http(s) link outside the app: the system browser in MAUI, a new tab in the browser. */
  openExternal(url: string): Promise<void>;
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
