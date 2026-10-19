import { describe, expect, it, vi } from 'vitest';
import { BridgeDataSource } from '../bridgeDataSource';
import { HttpDataSource } from '../httpDataSource';
import type { LessonProgress } from '../types';
import { GuestProgress, newer } from './guestProgress';

function memoryStore() {
  const items = new Map<string, string>();
  return {
    getItem: (key: string) => items.get(key) ?? null,
    setItem: (key: string, value: string) => void items.set(key, value),
    removeItem: (key: string) => void items.delete(key),
  };
}

const opened = (lessonId: string, updatedAt = '2026-10-19T09:00:00Z'): LessonProgress => ({
  lessonId,
  status: 'inProgress',
  doneBy: null,
  completedAt: null,
  readSections: [],
  position: null,
  lastReadAt: updatedAt,
  lastRevisedAt: null,
  updatedAt,
  deviceId: '',
});

const user = { id: 'u1', email: 'a@example.com', displayName: 'A', roles: ['Learner'], mustChangePassword: false };
const session = { accessToken: 'token-1', accessTokenExpiresAt: '2026-10-20T00:00:00Z', user };
const json = (body: unknown, status = 200) => new Response(JSON.stringify(body), { status });
const noContent = () => new Response(null, { status: 204 });

describe('GuestProgress', () => {
  it('keeps the later edit of a lesson', () => {
    const guest = new GuestProgress(memoryStore());

    guest.saveLessons([opened('srp', '2026-10-19T09:05:00Z')]);
    guest.saveLessons([{ ...opened('srp', '2026-10-19T09:01:00Z'), status: 'done' }]);

    expect(guest.get().lessons).toEqual([opened('srp', '2026-10-19T09:05:00Z')]);
  });

  it('keeps each day once, in order', () => {
    const guest = new GuestProgress(memoryStore());

    ['2026-10-19', '2026-10-17', '2026-10-19'].forEach((day) => guest.addDay(day));

    expect(guest.get().activityDays).toEqual(['2026-10-17', '2026-10-19']);
  });

  it('creates the browser id once', () => {
    const guest = new GuestProgress(memoryStore());

    expect(guest.deviceId()).toBe(guest.deviceId());
  });

  it('works without storage', () => {
    const guest = new GuestProgress(null);

    guest.saveLessons([opened('srp')]);

    expect(guest.isEmpty).toBe(true);
  });

  it('lets the incoming record win a tie', () => {
    const incoming = { ...opened('srp'), status: 'done' as const };

    expect(newer(opened('srp'), incoming)).toBe(incoming);
  });
});

describe('HttpDataSource progress', () => {
  it('keeps a signed-out reader’s progress as the guest, stamped with the browser id', async () => {
    const guest = new GuestProgress(memoryStore());
    const fetchFn = vi.fn();
    const source = new HttpDataSource('', fetchFn, { guest });

    await source.saveLessonProgress([opened('srp')]);
    await source.recordActivity('2026-10-19');

    expect(fetchFn).not.toHaveBeenCalled();
    const snapshot = await source.getProgress();
    expect(snapshot.lessons[0].deviceId).toBe(guest.deviceId());
    expect(snapshot.activityDays).toEqual(['2026-10-19']);
  });

  it('hands the guest’s progress to the user at sign-in, then forgets it', async () => {
    const guest = new GuestProgress(memoryStore());
    guest.saveLessons([opened('srp')]);
    const fetchFn = vi.fn().mockResolvedValueOnce(json(session)).mockResolvedValueOnce(json({ lessons: [], activityDays: [] }));
    const source = new HttpDataSource('', fetchFn, { guest });

    await source.login('a@example.com', 'pw');

    const [url, init] = fetchFn.mock.calls[1];
    expect(url).toBe('/api/progress/adopt');
    expect(init.headers.Authorization).toBe('Bearer token-1');
    expect(JSON.parse(init.body).lessons[0].lessonId).toBe('srp');
    expect(guest.isEmpty).toBe(true);
  });

  it('keeps the guest copy when the hand-over fails', async () => {
    const guest = new GuestProgress(memoryStore());
    guest.saveLessons([opened('srp')]);
    const fetchFn = vi.fn().mockResolvedValueOnce(json(session)).mockResolvedValueOnce(json({ code: 'x' }, 403));
    const source = new HttpDataSource('', fetchFn, { guest });

    await expect(source.login('a@example.com', 'pw')).resolves.toEqual(user);
    expect(guest.isEmpty).toBe(false);
  });

  it('uses the API once signed in, waiting for a session restore in flight', async () => {
    const fetchFn = vi
      .fn()
      .mockResolvedValueOnce(json(session))
      .mockResolvedValueOnce(json({ lessons: [opened('ocp')], activityDays: [] }))
      .mockResolvedValueOnce(noContent());
    const source = new HttpDataSource('', fetchFn, { guest: new GuestProgress(memoryStore()) });

    void source.restoreSession();
    const snapshot = await source.getProgress();
    await source.saveLessonProgress([opened('srp')]);

    expect(snapshot.lessons.map((l) => l.lessonId)).toEqual(['ocp']);
    expect(fetchFn.mock.calls.map(([url, init]) => `${init.method} ${url}`)).toEqual([
      'POST /api/auth/refresh',
      'GET /api/progress',
      'PUT /api/progress/lessons',
    ]);
  });
});

describe('BridgeDataSource progress', () => {
  it('sends records as one JSON string and reads PascalCase snapshots', async () => {
    const invoke = vi
      .fn()
      .mockResolvedValueOnce({ Ok: true, Value: true })
      .mockResolvedValueOnce({ Ok: true, Value: { Lessons: [{ LessonId: 'srp', Status: 'done' }], ActivityDays: [] } });
    const source = new BridgeDataSource({ InvokeDotNet: invoke });

    await source.saveLessonProgress([opened('srp')]);
    const snapshot = await source.getProgress();

    expect(invoke.mock.calls[0][0]).toBe('SaveLessonProgress');
    expect(JSON.parse(invoke.mock.calls[0][1][0])[0].lessonId).toBe('srp');
    expect(snapshot.lessons[0]).toMatchObject({ lessonId: 'srp', status: 'done' });
  });

  it('records a day', async () => {
    const invoke = vi.fn().mockResolvedValue({ ok: true, value: true });

    await new BridgeDataSource({ InvokeDotNet: invoke }).recordActivity('2026-10-19');

    expect(invoke).toHaveBeenCalledWith('RecordActivity', ['2026-10-19']);
  });
});
