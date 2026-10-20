import { describe, expect, it, vi } from 'vitest';
import { BridgeDataSource } from '../bridgeDataSource';
import { HttpDataSource } from '../httpDataSource';
import { GuestProgress } from '../progress/guestProgress';
import { AppError, type Bookmark, type Note } from '../types';
import { batchesOf, MAX_RECORDS_PER_WRITE } from './batches';
import { GuestNotes, STORAGE_FULL } from './guestNotes';

function memoryStore() {
  const items = new Map<string, string>();
  return {
    getItem: (key: string) => items.get(key) ?? null,
    setItem: (key: string, value: string) => void items.set(key, value),
    removeItem: (key: string) => void items.delete(key),
  };
}

const note = (body: string, updatedAt = '2026-10-20T09:00:00Z', id = 'n-1'): Note => ({
  id,
  lessonId: 'srp',
  anchor: '',
  body,
  createdAt: '2026-10-20T09:00:00Z',
  updatedAt,
  deletedAt: null,
  deviceId: '',
});

const bookmark = (kind: Bookmark['kind'], updatedAt = '2026-10-20T09:00:00Z', anchor = ''): Bookmark => ({
  kind,
  lessonId: 'srp',
  anchor,
  createdAt: '2026-10-20T09:00:00Z',
  updatedAt,
  deletedAt: null,
  deviceId: '',
});

const user = { id: 'u1', email: 'a@example.com', displayName: 'A', roles: ['Learner'], mustChangePassword: false };
const session = { accessToken: 'token-1', accessTokenExpiresAt: '2026-10-20T00:00:00Z', user };
const json = (body: unknown, status = 200) => new Response(JSON.stringify(body), { status });
const noContent = () => new Response(null, { status: 204 });
const emptyProgressGuest = () => new GuestProgress(memoryStore());

describe('GuestNotes', () => {
  it('keeps the later edit of a note, and bookmarks one per kind, lesson and section', () => {
    const guest = new GuestNotes(memoryStore());

    guest.saveNotes([note('newer', '2026-10-20T09:05:00Z')]);
    guest.saveNotes([note('older', '2026-10-20T09:01:00Z')]);
    guest.saveBookmarks([bookmark('lesson'), bookmark('section', undefined, 'the-rule'), bookmark('cheatSheet')]);
    guest.saveBookmarks([{ ...bookmark('lesson', '2026-10-20T09:02:00Z'), deletedAt: '2026-10-20T09:02:00Z' }]);

    expect(guest.get().notes.map((n) => n.body)).toEqual(['newer']);
    expect(guest.get().bookmarks).toHaveLength(3);
    expect(guest.get().bookmarks[0].deletedAt).not.toBeNull();
  });

  it('says so when the browser storage is full', () => {
    const full = { ...memoryStore(), setItem: () => { throw new DOMException('full', 'QuotaExceededError'); } };

    const save = () => new GuestNotes(full).saveNotes([note('big')]);

    expect(save).toThrow(AppError);
    expect(save).toThrow(expect.objectContaining({ code: STORAGE_FULL }));
  });
});

describe('batchesOf', () => {
  it('keeps each request under 500 records and the byte budget, never splitting a record', () => {
    const many = Array.from({ length: MAX_RECORDS_PER_WRITE + 1 }, (_, i) => note('x', undefined, `n-${i}`));
    expect(batchesOf(many).map((b) => b.length)).toEqual([MAX_RECORDS_PER_WRITE, 1]);

    const large = [note('é'.repeat(20_000)), note('é'.repeat(20_000)), note('small')];
    const bytes = new TextEncoder().encode(JSON.stringify(large[0])).length;
    expect(batchesOf(large, bytes + 400).map((b) => b.length)).toEqual([1, 2]);
    expect(batchesOf([])).toEqual([]);
  });
});

describe('HttpDataSource notes', () => {
  it('keeps a signed-out reader’s notes and bookmarks as the guest, stamped with the browser id', async () => {
    const guestNotes = new GuestNotes(memoryStore());
    const guest = emptyProgressGuest();
    const fetchFn = vi.fn();
    const source = new HttpDataSource('', fetchFn, { guest, guestNotes });

    await source.saveNotes([note('mine')]);
    await source.saveBookmarks([bookmark('lesson')]);

    expect(fetchFn).not.toHaveBeenCalled();
    const snapshot = await source.getNotes();
    expect(snapshot.notes[0].deviceId).toBe(guest.deviceId());
    expect(snapshot.bookmarks).toHaveLength(1);
  });

  it('hands the guest’s notes to the user at sign-in after the progress, then forgets them', async () => {
    const guestNotes = new GuestNotes(memoryStore());
    guestNotes.saveNotes([note('guest’s')]);
    const fetchFn = vi.fn().mockResolvedValueOnce(json(session)).mockResolvedValueOnce(json({ notes: [], bookmarks: [] }));
    const source = new HttpDataSource('', fetchFn, { guest: emptyProgressGuest(), guestNotes });

    await source.login('a@example.com', 'pw');

    const [url, init] = fetchFn.mock.calls[1];
    expect(url).toBe('/api/notes/adopt');
    expect(JSON.parse(init.body).notes[0].body).toBe('guest’s');
    expect(guestNotes.isEmpty).toBe(true);
  });

  it('keeps the guest’s notes when the hand-over fails', async () => {
    const guestNotes = new GuestNotes(memoryStore());
    guestNotes.saveNotes([note('guest’s')]);
    const fetchFn = vi.fn().mockResolvedValueOnce(json(session)).mockResolvedValueOnce(json({ code: 'x' }, 400));
    const source = new HttpDataSource('', fetchFn, { guest: emptyProgressGuest(), guestNotes });

    await expect(source.login('a@example.com', 'pw')).resolves.toEqual(user);
    expect(guestNotes.isEmpty).toBe(false);
  });

  it('uses the API once signed in, one request per batch', async () => {
    const fetchFn = vi
      .fn()
      .mockResolvedValueOnce(json(session))
      .mockResolvedValueOnce(json({ notes: [note('server')], bookmarks: [] }))
      .mockResolvedValue(noContent());
    const source = new HttpDataSource('', fetchFn, { guest: emptyProgressGuest(), guestNotes: new GuestNotes(memoryStore()) });
    const many = Array.from({ length: MAX_RECORDS_PER_WRITE + 1 }, (_, i) => note('x', undefined, `n-${i}`));

    void source.restoreSession();
    const snapshot = await source.getNotes();
    await source.saveNotes(many);
    await source.saveBookmarks([bookmark('cheatSheet')]);

    expect(snapshot.notes[0].body).toBe('server');
    expect(fetchFn.mock.calls.map(([url, init]) => `${init.method} ${url}`)).toEqual([
      'POST /api/auth/refresh',
      'GET /api/notes',
      'PUT /api/notes',
      'PUT /api/notes',
      'PUT /api/bookmarks',
    ]);
  });

  it('exports by downloading the file', async () => {
    const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => {});
    URL.createObjectURL = vi.fn().mockReturnValue('blob:notes');
    URL.revokeObjectURL = vi.fn();

    await new HttpDataSource('', vi.fn()).exportNotes('lld-notes-2026-10-20.md', '# My LLD notes');

    expect(click).toHaveBeenCalledTimes(1);
    expect((click.mock.contexts[0] as HTMLAnchorElement).download).toBe('lld-notes-2026-10-20.md');
    click.mockRestore();
  });
});

describe('BridgeDataSource notes', () => {
  it('sends records as one JSON string, reads PascalCase snapshots and shares the export', async () => {
    const invoke = vi
      .fn()
      .mockResolvedValueOnce({ Ok: true, Value: true })
      .mockResolvedValueOnce({ Ok: true, Value: true })
      .mockResolvedValueOnce({ Ok: true, Value: { Notes: [{ Id: 'n-1', Body: 'mine' }], Bookmarks: [{ Kind: 'section' }] } })
      .mockResolvedValueOnce({ Ok: true, Value: true });
    const source = new BridgeDataSource({ InvokeDotNet: invoke });

    await source.saveNotes([note('mine')]);
    await source.saveBookmarks([bookmark('lesson')]);
    const snapshot = await source.getNotes();
    await source.exportNotes('lld-notes-2026-10-20.md', '# My LLD notes');

    expect(invoke.mock.calls.map(([method]) => method)).toEqual(['SaveNotes', 'SaveBookmarks', 'GetNotes', 'ShareNotes']);
    expect(JSON.parse(invoke.mock.calls[0][1][0])[0].body).toBe('mine');
    expect(snapshot).toMatchObject({ notes: [{ id: 'n-1', body: 'mine' }], bookmarks: [{ kind: 'section' }] });
    expect(invoke.mock.calls[3][1]).toEqual(['lld-notes-2026-10-20.md', '# My LLD notes']);
  });
});
