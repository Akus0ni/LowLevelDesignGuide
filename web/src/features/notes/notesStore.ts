import { STORAGE_FULL } from '../../data/notes/guestNotes';
import { AppError, type Bookmark, type DataSource, type Note } from '../../data/types';
import { WriteBehind } from '../shared/writeBehind';
import { bookmarkKey, bookmarkTarget, setBookmark } from './noteRecords';

export interface NotesState {
  byId: ReadonlyMap<string, Note>;
  bookmarks: ReadonlyMap<string, Bookmark>;
}

/** A write the store won't retry: over a limit, a bad record, or a full browser storage. Shown once to the learner. */
export type NotesProblem = { code: string; message: string };

interface StoreOptions {
  now?: () => Date;
  events?: EventTarget;
}

/**
 * One owner's notes and bookmarks in memory (spec 007 plan §2.5), shaped like `ProgressStore`: every change updates the
 * snapshot at once, so the lesson, the outline, Revise and the Notes tab re-render together (Observer), then the
 * record is queued and written behind (`WriteBehind`). A write that fails for a reason a retry can't fix is dropped
 * from the queue and reported; the record stays in memory, so the learner's text is still on screen.
 */
export class NotesStore {
  readonly ready: Promise<void>;
  private state: NotesState = { byId: new Map(), bookmarks: new Map() };
  private readonly listeners = new Set<() => void>();
  private readonly problemListeners = new Set<(problem: NotesProblem) => void>();
  private readonly queuedNotes = new Map<string, Note>();
  private readonly queuedBookmarks = new Map<string, Bookmark>();
  private readonly source: DataSource;
  private readonly now: () => Date;
  private readonly writer: WriteBehind;
  private loaded = false;
  private readonly early: (() => void)[] = [];

  constructor(source: DataSource, options: StoreOptions = {}) {
    this.source = source;
    this.now = options.now ?? (() => new Date());
    this.writer = new WriteBehind(
      () => this.writeQueued(),
      options.events ?? (typeof window === 'undefined' ? undefined : window),
    );
    this.ready = this.load();
  }

  readonly subscribe = (listener: () => void): (() => void) => {
    this.listeners.add(listener);
    return () => this.listeners.delete(listener);
  };

  readonly getSnapshot = (): NotesState => this.state;

  onProblem(listener: (problem: NotesProblem) => void): () => void {
    this.problemListeners.add(listener);
    return () => this.problemListeners.delete(listener);
  }

  /** The current time as an ISO string: every edit is stamped with it. */
  stamp(): string {
    return this.now().toISOString();
  }

  /** Saves a note as it is now (new, edited, deleted or restored). */
  putNote(note: Note): void {
    this.whenLoaded(() => {
      if (this.state.byId.get(note.id) === note) return;
      const byId = new Map(this.state.byId);
      byId.set(note.id, note);
      this.state = { ...this.state, byId };
      this.queuedNotes.set(note.id, note);
      this.changed();
    });
  }

  /** Bookmarks or un-bookmarks a lesson, a section or a cheat sheet. */
  toggleBookmark(target: ReturnType<typeof bookmarkTarget>, on: boolean): void {
    const now = this.stamp();
    this.whenLoaded(() => {
      const key = bookmarkKey(target);
      const bookmark = setBookmark(this.state.bookmarks.get(key), target, on, now);
      const bookmarks = new Map(this.state.bookmarks);
      bookmarks.set(key, bookmark);
      this.state = { ...this.state, bookmarks };
      this.queuedBookmarks.set(key, bookmark);
      this.changed();
    });
  }

  flush(): Promise<void> {
    return this.writer.flush();
  }

  /** Before the owner changes: stop listening and write what is left. */
  dispose(): Promise<void> {
    return this.writer.dispose();
  }

  private async load(): Promise<void> {
    try {
      const snapshot = await this.source.getNotes();
      this.state = {
        byId: new Map(snapshot.notes.map((note) => [note.id, note])),
        bookmarks: new Map(snapshot.bookmarks.map((bookmark) => [bookmarkKey(bookmark), bookmark])),
      };
    } catch {
      // Notes couldn't be read: the rest of the app works, and new notes are kept and retried.
    }
    this.loaded = true;
    this.early.splice(0).forEach((run) => run());
    this.notify();
  }

  private whenLoaded(run: () => void): void {
    if (this.loaded) run();
    else this.early.push(run);
  }

  private changed(): void {
    this.notify();
    this.writer.schedule();
  }

  private notify(): void {
    this.listeners.forEach((listener) => listener());
  }

  private async writeQueued(): Promise<void> {
    await this.writeBatch(this.queuedNotes, (notes) => this.source.saveNotes(notes), (note) => note.id);
    await this.writeBatch(this.queuedBookmarks, (bookmarks) => this.source.saveBookmarks(bookmarks), bookmarkKey);
  }

  private async writeBatch<T>(queue: Map<string, T>, save: (records: T[]) => Promise<void>, keyOf: (record: T) => string) {
    const records = [...queue.values()];
    if (records.length === 0) return;
    queue.clear();
    try {
      await save(records);
    } catch (error) {
      if (isFinal(error)) {
        this.problemListeners.forEach((listener) => listener({ code: error.code, message: error.message }));
        return;
      }
      // Offline or busy: keep them queued (unless a newer edit has been queued meanwhile).
      records.forEach((record) => {
        if (!queue.has(keyOf(record))) queue.set(keyOf(record), record);
      });
    }
  }
}

/** Errors a retry can't fix. */
function isFinal(error: unknown): error is AppError {
  return error instanceof AppError && (error.code === STORAGE_FULL || error.code.startsWith('validation.'));
}
