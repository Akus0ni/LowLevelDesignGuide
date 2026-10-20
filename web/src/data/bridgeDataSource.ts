import { toCamelCaseDeep } from './camelCase';
import {
  AppError,
  type AccessRequestInput,
  type AccessRequestOutcome,
  type AppInfo,
  type CheatSheetDetail,
  type CheatSheetSummary,
  type DataSource,
  type HybridWebViewBridge,
  type LessonDetail,
  type ModuleDetail,
  type Bookmark,
  type LessonProgress,
  type Note,
  type NotesSnapshot,
  type ModuleSummary,
  type ProgressSnapshot,
  type ServerStatus,
  type UserSummary,
} from './types';

/** Raw messages C# pushes with HybridWebView.SendRawMessage (see Lld.App BridgeEvent). */
export const RAW_MESSAGE_EVENT = 'HybridWebViewMessageReceived';
type RawMessageEvent = CustomEvent<{ message: string }>;

type Raw = Record<string, unknown>;

/** C# may serialise with PascalCase; read either casing. */
function pick<T>(raw: Raw, camel: string): T {
  const pascal = camel[0].toUpperCase() + camel.slice(1);
  return (raw[camel] ?? raw[pascal]) as T;
}

function parse(raw: unknown): Raw {
  return (typeof raw === 'string' ? JSON.parse(raw) : raw) as Raw;
}

function toAppInfo(raw: unknown): AppInfo {
  const value = parse(raw);
  return {
    name: pick(value, 'name'),
    version: pick(value, 'version'),
    environment: pick(value, 'environment'),
  };
}

function toStatus(raw: Raw): ServerStatus {
  return {
    state: pick(raw, 'state'),
    reason: pick(raw, 'reason'),
    since: pick(raw, 'since'),
    quotaResetsAt: pick<string | null>(raw, 'quotaResetsAt') ?? undefined,
  };
}

function toUser(raw: Raw): UserSummary {
  return {
    id: pick(raw, 'id'),
    email: pick(raw, 'email'),
    displayName: pick(raw, 'displayName'),
    roles: pick(raw, 'roles') ?? [],
    mustChangePassword: Boolean(pick(raw, 'mustChangePassword')),
  };
}

/**
 * MAUI strategy: every call goes to BridgeService in C#, which holds the tokens in SecureStorage and talks
 * to the API. C# answers with a BridgeResult { ok, value, error { code, message } } instead of throwing.
 */
export class BridgeDataSource implements DataSource {
  readonly kind = 'bridge';
  private readonly bridge: HybridWebViewBridge;
  private readonly events: EventTarget;

  constructor(bridge: HybridWebViewBridge, events: EventTarget = window) {
    this.bridge = bridge;
    this.events = events;
  }

  async getServerStatus(): Promise<ServerStatus> {
    return toStatus(parse(await this.bridge.InvokeDotNet('GetServerStatus')));
  }

  onServerStatusChanged(listener: (status: ServerStatus) => void): () => void {
    return this.onRawMessage('serverStatus', (raw) => listener(toStatus(pick<Raw>(raw, 'status'))));
  }

  onAccessRequestSent(listener: (email: string) => void): () => void {
    return this.onRawMessage('accessRequestSent', (raw) => listener(pick(raw, 'email')));
  }

  async getInfo(): Promise<AppInfo> {
    return toAppInfo(await this.bridge.InvokeDotNet('GetInfo'));
  }

  async requestAccess(input: AccessRequestInput): Promise<AccessRequestOutcome> {
    const outcome = (await this.call('RequestAccess', [input.email, input.name, input.note ?? null])) as Raw;
    return { queued: Boolean(pick(outcome ?? {}, 'queued')) };
  }

  async login(email: string, password: string): Promise<UserSummary> {
    return toUser((await this.call('Login', [email, password])) as Raw);
  }

  async restoreSession(): Promise<UserSummary | null> {
    const value = await this.call('RestoreSession');
    return value ? toUser(value as Raw) : null;
  }

  async changePassword(currentPassword: string, newPassword: string): Promise<UserSummary> {
    return toUser((await this.call('ChangePassword', [currentPassword, newPassword])) as Raw);
  }

  async logout(): Promise<void> {
    await this.call('Logout');
  }

  async logoutAll(): Promise<void> {
    await this.call('LogoutAll');
  }

  // Content: read from the device database by C#, so it works offline.

  getModules(): Promise<ModuleSummary[]> {
    return this.content('GetModules');
  }

  getModule(slug: string): Promise<ModuleDetail> {
    return this.content('GetModule', [slug]);
  }

  getLesson(id: string): Promise<LessonDetail> {
    return this.content('GetLesson', [id]);
  }

  getCheatSheets(moduleSlug?: string): Promise<CheatSheetSummary[]> {
    return this.content('GetCheatSheets', [moduleSlug ?? null]);
  }

  getCheatSheet(lessonId: string): Promise<CheatSheetDetail> {
    return this.content('GetCheatSheet', [lessonId]);
  }

  async openExternal(url: string): Promise<void> {
    await this.call('OpenExternal', [url]);
  }

  // Progress: kept on the device by C#, which knows whose it is (the stored session's user, or the guest).

  async getProgress(): Promise<ProgressSnapshot> {
    return toCamelCaseDeep(await this.call('GetProgress')) as ProgressSnapshot;
  }

  async saveLessonProgress(records: LessonProgress[]): Promise<void> {
    // One JSON string argument: C# reads it with its own camelCase settings, whatever the bridge's defaults are.
    await this.call('SaveLessonProgress', [JSON.stringify(records)]);
  }

  async recordActivity(day: string): Promise<void> {
    await this.call('RecordActivity', [day]);
  }

  // Notes and bookmarks: on the device for the same owner as progress (spec 007).

  async getNotes(): Promise<NotesSnapshot> {
    return toCamelCaseDeep(await this.call('GetNotes')) as NotesSnapshot;
  }

  async saveNotes(notes: Note[]): Promise<void> {
    await this.call('SaveNotes', [JSON.stringify(notes)]);
  }

  async saveBookmarks(bookmarks: Bookmark[]): Promise<void> {
    await this.call('SaveBookmarks', [JSON.stringify(bookmarks)]);
  }

  async exportNotes(fileName: string, markdown: string): Promise<void> {
    await this.call('ShareNotes', [fileName, markdown]);
  }

  /** Content records are nested (outline, previous / next), so convert every key at once instead of picking. */
  private async content<T>(method: string, args: unknown[] = []): Promise<T> {
    return toCamelCaseDeep(await this.call(method, args)) as T;
  }

  /** Routes C# raw messages by their `type`; ignores anything else on the channel. */
  private onRawMessage(type: string, handle: (raw: Raw) => void): () => void {
    const handler = (event: Event) => {
      try {
        const raw = parse((event as RawMessageEvent).detail?.message);
        if (pick(raw, 'type') === type) handle(raw);
      } catch {
        // Not one of ours.
      }
    };
    this.events.addEventListener(RAW_MESSAGE_EVENT, handler);
    return () => this.events.removeEventListener(RAW_MESSAGE_EVENT, handler);
  }

  private async call(method: string, args: unknown[] = []): Promise<unknown> {
    const result = parse(await this.bridge.InvokeDotNet(method, args));
    if (pick<boolean>(result, 'ok')) return pick(result, 'value');

    const error = (pick<Raw>(result, 'error') ?? {}) as Raw;
    throw new AppError(
      pick(error, 'code') ?? 'bridge.failed',
      pick(error, 'message') ?? 'Something went wrong.',
    );
  }
}
