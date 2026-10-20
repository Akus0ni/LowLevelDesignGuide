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
  /** The concept's face, one emoji (spec 004 §3.8); concept modules always have one. */
  icon?: string | null;
  /** A phrase to remember, at most 40 characters ("Ask, don't touch"). */
  hook?: string | null;
  /** Outline entries: the sections a reader ticks off (spec 005, "3 of 5 sections"). */
  sectionCount: number;
  /** Two letters for the periodic table of a pattern module, e.g. `St` (spec 006 §3.5). */
  symbol?: string | null;
}

/** Mirrors Lld.Contracts.ModuleDetail. */
export interface ModuleDetail {
  slug: string;
  title: string;
  summary: string;
  lessons: LessonSummary[];
  /** How the lessons relate (spec 004 §3.6); null when the module has no map. */
  map?: ModuleMap | null;
  /** Memory hooks such as SOLID (spec 004 §3.8); empty when none. */
  mnemonics?: Mnemonic[] | null;
  /** The "Which pattern?" decision tree (spec 006 §3.6); null when the module has none. */
  picker?: ModulePicker | null;
  /** Groups of lessons that are easy to confuse (spec 006 §3.7); empty when none. */
  lookalikes?: Lookalike[] | null;
}

/** Mirrors Lld.Contracts.ModulePicker: start at `start`, answer questions until an answer picks lessons. */
export interface ModulePicker {
  start: string;
  questions: PickerQuestion[];
}

export interface PickerQuestion {
  id: string;
  ask: string;
  answers: PickerAnswer[];
}

/** Leads to another question (`next`) or picks 1–2 lessons (slugs) with a one-line `why`. */
export interface PickerAnswer {
  label: string;
  next?: string | null;
  picks: string[];
  why?: string | null;
}

/** Mirrors Lld.Contracts.Lookalike: 2–3 lesson slugs, the one difference and the question that tells them apart. */
export interface Lookalike {
  lessons: string[];
  difference: string;
  ask: string;
}

/** Mirrors Lld.Contracts.Mnemonic: a word whose letters start the titles of `lessons` (slugs), in order. */
export interface Mnemonic {
  word: string;
  lessons: string[];
}

/** Mirrors Lld.Contracts.ModuleMap: lessons in titled groups, and labelled links between them. */
export interface ModuleMap {
  groups: MapGroup[];
  links: MapLink[];
}

/** `title` is empty for lessons outside any group; `lessons` are slugs in the order written. */
export interface MapGroup {
  title: string;
  lessons: string[];
}

/** "from label to", e.g. "ocp needs lsp" (slugs). */
export interface MapLink {
  from: string;
  to: string;
  label: string | null;
}

/** An h2 or h3 of a lesson; `anchor` follows GitHub's heading-id rules (github-slugger). */
export interface OutlineEntry {
  level: 2 | 3;
  text: string;
  anchor: string;
}

/** A type the lesson's code mentions, with its role (from the lesson's entities table). Mirrors Lld.Contracts.LessonTerm. */
export interface LessonTerm {
  term: string;
  role: string;
  /** Heading of the entities table, for "Read more". */
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
/** A detail has the whole outline, so it doesn't repeat the summary's section count. */
export interface LessonDetail extends Omit<LessonSummary, 'sectionCount'> {
  moduleTitle: string;
  bodyMarkdown: string;
  outline: OutlineEntry[];
  cheatSheetTitle: string;
  previous: LessonLink | null;
  next: LessonLink | null;
  /** Code-callout terms (spec 003 §3.8); empty when the lesson has no entities table. */
  terms: LessonTerm[];
}

/** Mirrors Lld.Contracts.CheatSheetSummary. */
export interface CheatSheetSummary {
  lessonId: string;
  moduleSlug: string;
  moduleTitle: string;
  lessonSlug: string;
  lessonTitle: string;
  title: string;
  /** The lesson's icon, when it has one. */
  icon?: string | null;
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

// ---- Progress (spec 005). Mirrors Lld.Contracts.LessonProgressDto / ProgressSnapshotDto. --------------------------

export type LessonStatus = 'notStarted' | 'inProgress' | 'done';

/** How a lesson became done: every section was read, or the learner marked it. */
export type DoneBy = 'auto' | 'manual';

export interface ReadingPosition {
  /** Nearest heading above the top of the screen ('' = above the first heading). */
  anchor: string;
  /** Pixels scrolled past that heading. */
  offset: number;
}

/** One learner's progress with one lesson. A lesson with no record is not started. Dates are ISO 8601 strings. */
export interface LessonProgress {
  lessonId: string;
  status: LessonStatus;
  doneBy: DoneBy | null;
  completedAt: string | null;
  /** Outline anchors already read (the outline's ticks). */
  readSections: string[];
  position: ReadingPosition | null;
  lastReadAt: string | null;
  lastRevisedAt: string | null;
  /** When this record last changed: the later edit wins. */
  updatedAt: string;
  /** Filled in by the data source (device id in the app, browser id in dev mode). */
  deviceId: string;
}

export interface ProgressSnapshot {
  lessons: LessonProgress[];
  /** Local days (yyyy-MM-dd) with reading or revising, oldest first. */
  activityDays: string[];
}

// ---- Notes and bookmarks (spec 007). Mirrors Lld.Contracts.NoteDto / BookmarkDto / NotesSnapshotDto. ---------------

/** A learner's Markdown note on a whole lesson (`anchor` '') or one section (its heading id). Tags live in the body. */
export interface Note {
  /** A UUID made when the note is first written. */
  id: string;
  lessonId: string;
  anchor: string;
  body: string;
  createdAt: string;
  /** When this record last changed: the later edit wins. */
  updatedAt: string;
  /** Set when deleted; the record stays so a later sync can carry the delete, and Undo clears it. */
  deletedAt: string | null;
  /** Filled in by the data source. */
  deviceId: string;
}

export type BookmarkKind = 'lesson' | 'section' | 'cheatSheet';

/** One per (kind, lesson, anchor); `anchor` is '' except for a section. Removing it sets `deletedAt`. */
export interface Bookmark {
  kind: BookmarkKind;
  lessonId: string;
  anchor: string;
  createdAt: string;
  updatedAt: string;
  deletedAt: string | null;
  deviceId: string;
}

/** Everything one learner has written or bookmarked, deleted records included, oldest first. */
export interface NotesSnapshot {
  notes: Note[];
  bookmarks: Bookmark[];
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

  // Progress (spec 005): the signed-in user's, or this device's guest's. The data source decides whose.
  getProgress(): Promise<ProgressSnapshot>;
  /** Saves records (reading, marks, resets); per lesson the later `updatedAt` wins. */
  saveLessonProgress(records: LessonProgress[]): Promise<void>;
  /** Remembers a local day (yyyy-MM-dd) with reading or revising. */
  recordActivity(day: string): Promise<void>;

  // Notes and bookmarks (spec 007): the same owner as progress. Writes are batches; per record the later edit wins.
  getNotes(): Promise<NotesSnapshot>;
  saveNotes(notes: Note[]): Promise<void>;
  saveBookmarks(bookmarks: Bookmark[]): Promise<void>;
  /** Hands the exported Markdown to the learner: a download in the browser, the share sheet in the app. */
  exportNotes(fileName: string, markdown: string): Promise<void>;
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
