import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { STORAGE_FULL } from '../../data/notes/guestNotes';
import { AppError, type Note } from '../../data/types';
import { fakeDataSource } from '../../test/fakeDataSource';
import { FLUSH_DELAY_MS } from '../shared/writeBehind';
import { deleteNote, editNote, newNote } from './noteRecords';
import { NotesStore } from './notesStore';

const NOW = new Date('2026-10-20T09:00:00Z');
const stored: Note = newNote({ lessonId: 'srp', anchor: 'the-rule' }, 'Stored #weak', '2026-10-19T09:00:00Z', 'stored');

function createStore(overrides = {}) {
  const source = fakeDataSource({
    getNotes: vi.fn().mockResolvedValue({ notes: [stored], bookmarks: [] }),
    ...overrides,
  });
  const events = new EventTarget();
  const store = new NotesStore(source, { now: () => NOW, events });
  return { source, store, events };
}

describe('NotesStore', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('loads the owner’s notes and writes edits after a quiet second, the latest edit of each note once', async () => {
    const { store, source } = createStore();
    await store.ready;
    const listener = vi.fn();
    store.subscribe(listener);

    const written = newNote({ lessonId: 'srp', anchor: '' }, 'a', store.stamp(), 'n-1');
    store.putNote(written);
    store.putNote(editNote(written, 'ab', store.stamp()));
    store.putNote(deleteNote(stored, store.stamp()));

    expect(store.getSnapshot().byId.get('n-1')?.body).toBe('ab');
    expect(listener).toHaveBeenCalledTimes(3);
    expect(source.saveNotes).not.toHaveBeenCalled();
    await vi.advanceTimersByTimeAsync(FLUSH_DELAY_MS);
    expect(vi.mocked(source.saveNotes).mock.calls[0][0].map((n) => [n.id, n.body, n.deletedAt !== null])).toEqual([
      ['n-1', 'ab', false],
      ['stored', 'Stored #weak', true],
    ]);
  });

  it('applies edits made before the notes loaded on top of them', async () => {
    let resolve!: (value: { notes: Note[]; bookmarks: [] }) => void;
    const { store } = createStore({ getNotes: vi.fn(() => new Promise((r) => (resolve = r))) });

    store.putNote(newNote({ lessonId: 'ocp', anchor: '' }, 'early', NOW.toISOString(), 'early'));
    resolve({ notes: [stored], bookmarks: [] });
    await store.ready;

    expect([...store.getSnapshot().byId.keys()]).toEqual(['stored', 'early']);
  });

  it('bookmarks and removes, writing the latest state of each bookmark', async () => {
    const { store, source } = createStore();
    await store.ready;
    const target = { kind: 'section' as const, lessonId: 'srp', anchor: 'the-rule' };

    store.toggleBookmark(target, true);
    store.toggleBookmark({ kind: 'cheatSheet', lessonId: 'srp', anchor: '' }, true);
    store.toggleBookmark(target, false);
    await store.flush();

    const saved = vi.mocked(source.saveBookmarks).mock.calls[0][0];
    expect(saved).toHaveLength(2);
    expect(saved.find((b) => b.kind === 'section')?.deletedAt).toBe(NOW.toISOString());
  });

  it('keeps writes that failed for a passing reason, and retries them', async () => {
    const saveNotes = vi.fn().mockRejectedValueOnce(new AppError('network.unavailable', 'offline')).mockResolvedValue(undefined);
    const { store } = createStore({ saveNotes });
    await store.ready;

    store.putNote(newNote({ lessonId: 'srp', anchor: '' }, 'kept', store.stamp(), 'n-1'));
    await store.flush();
    await store.flush();

    expect(saveNotes).toHaveBeenCalledTimes(2);
    expect(saveNotes.mock.calls[1][0][0].id).toBe('n-1');
  });

  it('reports a write no retry can fix once, and keeps the text in memory', async () => {
    const saveNotes = vi.fn().mockRejectedValue(new AppError(STORAGE_FULL, "Couldn't save your note on this device."));
    const { store } = createStore({ saveNotes });
    await store.ready;
    const problems = vi.fn();
    store.onProblem(problems);

    store.putNote(newNote({ lessonId: 'srp', anchor: '' }, 'big', store.stamp(), 'n-1'));
    await store.flush();
    await store.flush();

    expect(problems).toHaveBeenCalledExactlyOnceWith({ code: STORAGE_FULL, message: "Couldn't save your note on this device." });
    expect(saveNotes).toHaveBeenCalledTimes(1);
    expect(store.getSnapshot().byId.get('n-1')?.body).toBe('big');
  });

  it('writes what is left when the page is hidden and when disposed', async () => {
    const { store, source, events } = createStore();
    await store.ready;
    store.putNote(newNote({ lessonId: 'srp', anchor: '' }, 'a', store.stamp(), 'n-1'));

    events.dispatchEvent(new Event('pagehide'));
    await vi.advanceTimersByTimeAsync(0);
    store.putNote(newNote({ lessonId: 'srp', anchor: '' }, 'b', store.stamp(), 'n-2'));
    await store.dispose();

    expect(vi.mocked(source.saveNotes).mock.calls.map((call) => call[0][0].id)).toEqual(['n-1', 'n-2']);
  });
});
