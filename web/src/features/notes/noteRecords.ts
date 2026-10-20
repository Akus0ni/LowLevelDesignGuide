import { bookmarkKey } from '../../data/notes/guestNotes';
import type { Bookmark, BookmarkKind, Note } from '../../data/types';

export { bookmarkKey };

/** Same numbers as Lld.Domain NoteLimits, for hints and for not sending what the store would refuse. */
export const NOTE_LIMITS = { maxBodyLength: 20_000, maxNotesPerLesson: 100 } as const;

/** Where a note or bookmark sits: a lesson, and a section in it ('' = the whole lesson). */
export interface Place {
  lessonId: string;
  anchor: string;
}

// Pure edits (spec 007 §3.1): each returns a new record with `updatedAt` = now, so the later edit wins everywhere.

export function newNote(place: Place, body: string, now: string, id: string = crypto.randomUUID()): Note {
  return { id, ...place, body, createdAt: now, updatedAt: now, deletedAt: null, deviceId: '' };
}

export function editNote(note: Note, body: string, now: string): Note {
  return note.body === body ? note : { ...note, body, updatedAt: now };
}

/** A soft delete: the record stays so it can be undone now, and synced later (spec 010). */
export function deleteNote(note: Note, now: string): Note {
  return { ...note, deletedAt: now, updatedAt: now };
}

export function restoreNote(note: Note, now: string): Note {
  return { ...note, deletedAt: null, updatedAt: now };
}

export const isLive = (record: { deletedAt: string | null }): boolean => record.deletedAt === null;

/** An empty note is never kept (spec §3.1). */
export const isBlank = (body: string): boolean => body.trim().length === 0;

export function bookmarkTarget(kind: BookmarkKind, lessonId: string, anchor = ''): Pick<Bookmark, 'kind' | 'lessonId' | 'anchor'> {
  return { kind, lessonId, anchor: kind === 'section' ? anchor : '' };
}

/** Bookmarks or un-bookmarks; `existing` is the stored record for the same target, live or removed. */
export function setBookmark(
  existing: Bookmark | undefined,
  target: Pick<Bookmark, 'kind' | 'lessonId' | 'anchor'>,
  on: boolean,
  now: string,
): Bookmark {
  if (existing) return { ...existing, deletedAt: on ? null : now, updatedAt: now };
  return { ...target, createdAt: now, updatedAt: now, deletedAt: on ? null : now, deviceId: '' };
}
