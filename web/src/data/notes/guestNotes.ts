import { AppError, type Bookmark, type Note, type NotesSnapshot } from '../types';

type KeyValueStore = Pick<Storage, 'getItem' | 'setItem' | 'removeItem'>;

const NOTES_KEY = 'lld.notes.guest';

export const STORAGE_FULL = 'notes.storage_full';

export const emptyNotes = (): NotesSnapshot => ({ notes: [], bookmarks: [] });

/** The bookmark's identity: at most one per kind, lesson and section (as Lld.Domain BookmarkKey). */
export const bookmarkKey = (bookmark: Pick<Bookmark, 'kind' | 'lessonId' | 'anchor'>): string =>
  `${bookmark.kind}|${bookmark.lessonId}|${bookmark.anchor}`;

/** The later edit wins; on a tie the incoming record wins (as Lld.Domain NoteRules.Newer). */
export function newerRecord<T extends { updatedAt: string }>(stored: T, incoming: T): T {
  return Date.parse(incoming.updatedAt) >= Date.parse(stored.updatedAt) ? incoming : stored;
}

/**
 * Browser dev mode, signed out: the guest's notes and bookmarks in localStorage (spec 007 §3.8). They are handed to the
 * API when someone signs in, then cleared. Unlike progress, a write that doesn't fit throws: a lost note must be said.
 */
export class GuestNotes {
  private readonly store: KeyValueStore | null;

  constructor(store: KeyValueStore | null = safeLocalStorage()) {
    this.store = store;
  }

  get(): NotesSnapshot {
    try {
      const raw = this.store?.getItem(NOTES_KEY);
      return raw ? (JSON.parse(raw) as NotesSnapshot) : emptyNotes();
    } catch {
      return emptyNotes();
    }
  }

  get isEmpty(): boolean {
    const { notes, bookmarks } = this.get();
    return notes.length === 0 && bookmarks.length === 0;
  }

  saveNotes(records: Note[]): void {
    const snapshot = this.get();
    this.write({ ...snapshot, notes: upsert(snapshot.notes, records, (note) => note.id) });
  }

  saveBookmarks(records: Bookmark[]): void {
    const snapshot = this.get();
    this.write({ ...snapshot, bookmarks: upsert(snapshot.bookmarks, records, bookmarkKey) });
  }

  clear(): void {
    try {
      this.store?.removeItem(NOTES_KEY);
    } catch {
      // Nothing to clear.
    }
  }

  private write(snapshot: NotesSnapshot): void {
    if (!this.store) return;
    try {
      this.store.setItem(NOTES_KEY, JSON.stringify(snapshot));
    } catch {
      throw new AppError(STORAGE_FULL, "Couldn't save your note on this device.");
    }
  }
}

function upsert<T extends { updatedAt: string }>(stored: T[], incoming: T[], keyOf: (record: T) => string): T[] {
  const byKey = new Map(stored.map((record) => [keyOf(record), record]));
  incoming.forEach((record) => {
    const existing = byKey.get(keyOf(record));
    byKey.set(keyOf(record), existing ? newerRecord(existing, record) : record);
  });
  return [...byKey.values()];
}

function safeLocalStorage(): KeyValueStore | null {
  try {
    return globalThis.localStorage ?? null;
  } catch {
    return null;
  }
}
