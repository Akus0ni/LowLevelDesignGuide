import { createContext, use, useContext, useMemo, useSyncExternalStore } from 'react';
import type { BookmarkKind, Note } from '../../data/types';
import { bookmarkKey, bookmarkTarget, isLive } from './noteRecords';
import type { NotesState, NotesStore } from './notesStore';

export const NotesContext = createContext<NotesStore | null>(null);

export function useNotesStore(): NotesStore {
  const store = useContext(NotesContext);
  if (!store) throw new Error('useNotesStore must be used inside <NotesProvider>');
  return store;
}

/** The owner's notes and bookmarks. Suspends until they have loaded (a local read on the device). */
export function useNotes(): NotesState {
  const store = useNotesStore();
  use(store.ready);
  return useSyncExternalStore(store.subscribe, store.getSnapshot);
}

/** Every live note, oldest first. */
export function useAllNotes(): Note[] {
  const { byId } = useNotes();
  return useMemo(() => [...byId.values()].filter(isLive).sort((a, b) => a.createdAt.localeCompare(b.createdAt)), [byId]);
}

/** A lesson's live notes, oldest first. */
export function useLessonNotes(lessonId: string): Note[] {
  const notes = useAllNotes();
  return useMemo(() => notes.filter((note) => note.lessonId === lessonId), [notes, lessonId]);
}

/** What the reader marks per section: how many notes it has, and whether it is bookmarked. */
export interface SectionMarks {
  notes: ReadonlyMap<string, number>;
  bookmarked: ReadonlySet<string>;
}

export function useSectionMarks(lessonId: string): SectionMarks {
  const notes = useLessonNotes(lessonId);
  const { bookmarks } = useNotes();
  return useMemo(() => {
    const counts = new Map<string, number>();
    notes.filter((note) => note.anchor).forEach((note) => counts.set(note.anchor, (counts.get(note.anchor) ?? 0) + 1));
    const bookmarked = new Set(
      [...bookmarks.values()]
        .filter((bookmark) => isLive(bookmark) && bookmark.kind === 'section' && bookmark.lessonId === lessonId)
        .map((bookmark) => bookmark.anchor),
    );
    return { notes: counts, bookmarked };
  }, [notes, bookmarks, lessonId]);
}

/** Whether a lesson, section or cheat sheet is bookmarked, and a toggle for it. */
export function useBookmark(kind: BookmarkKind, lessonId: string, anchor = ''): [boolean, (on: boolean) => void] {
  const store = useNotesStore();
  const { bookmarks } = useNotes();
  const target = bookmarkTarget(kind, lessonId, anchor);
  const stored = bookmarks.get(bookmarkKey(target));
  const on = stored !== undefined && isLive(stored);
  return [on, (next: boolean) => store.toggleBookmark(target, next)];
}
