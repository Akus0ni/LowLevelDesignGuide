import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { LessonProgress } from '../../data/types';
import { fakeDataSource } from '../../test/fakeDataSource';
import { takeLegacyReading } from './legacyImport';
import { markedDone, opened } from './lessonProgress';
import { FLUSH_DELAY_MS, ProgressStore } from './progressStore';

const NOW = new Date('2026-10-19T09:00:00Z');
const stored: LessonProgress = {
  lessonId: 'ocp',
  status: 'done',
  doneBy: 'manual',
  completedAt: '2026-10-18T09:00:00Z',
  readSections: [],
  position: null,
  lastReadAt: null,
  lastRevisedAt: null,
  updatedAt: '2026-10-18T09:00:00Z',
  deviceId: 'd',
};

function createStore(overrides = {}) {
  const source = fakeDataSource({
    getProgress: vi.fn().mockResolvedValue({ lessons: [stored], activityDays: ['2026-10-18'] }),
    ...overrides,
  });
  const store = new ProgressStore(source, { now: () => NOW, events: new EventTarget(), legacy: () => new Map() });
  return { source, store };
}

describe('ProgressStore', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('loads the owner’s progress', async () => {
    const { store } = createStore();

    await store.ready;

    expect(store.getSnapshot().byId.get('ocp')).toEqual(stored);
    expect(store.getSnapshot().activityDays).toEqual(['2026-10-18']);
  });

  it('updates at once and writes after a quiet second, in one batch', async () => {
    const { store, source } = createStore();
    await store.ready;
    const listener = vi.fn();
    store.subscribe(listener);

    store.change('srp', opened);
    store.change('lsp', markedDone);

    expect(store.getSnapshot().byId.get('srp')?.status).toBe('inProgress');
    expect(listener).toHaveBeenCalledTimes(2);
    expect(source.saveLessonProgress).not.toHaveBeenCalled();
    await vi.advanceTimersByTimeAsync(FLUSH_DELAY_MS);
    expect(source.saveLessonProgress).toHaveBeenCalledTimes(1);
    expect(vi.mocked(source.saveLessonProgress).mock.calls[0][0].map((r) => r.lessonId)).toEqual(['srp', 'lsp']);
  });

  it('does not save a change that changed nothing', async () => {
    const { store, source } = createStore();
    await store.ready;

    store.change('ocp', markedDone);
    await store.flush();

    expect(source.saveLessonProgress).not.toHaveBeenCalled();
  });

  it('keeps a failed write queued and sends it with the next one', async () => {
    const save = vi.fn().mockRejectedValueOnce(new Error('offline')).mockResolvedValue(undefined);
    const { store } = createStore({ saveLessonProgress: save });
    await store.ready;

    store.change('srp', opened);
    await store.flush();
    store.change('lsp', opened);
    await store.flush();

    expect(save.mock.calls[1][0].map((r: LessonProgress) => r.lessonId)).toEqual(['srp', 'lsp']);
  });

  it('records today once', async () => {
    const { store, source } = createStore();
    await store.ready;

    store.recordActivity();
    store.recordActivity();
    await store.flush();

    expect(store.getSnapshot().activityDays).toEqual(['2026-10-18', localToday()]);
    expect(source.recordActivity).toHaveBeenCalledTimes(1);
  });

  it('keeps changes made while loading on top of what loads', async () => {
    let finish: (value: unknown) => void = () => {};
    const { store } = createStore({ getProgress: vi.fn(() => new Promise((resolve) => (finish = resolve))) });

    store.change('ocp', opened);
    finish({ lessons: [stored], activityDays: [] });
    await store.ready;

    expect(store.getSnapshot().byId.get('ocp')?.status).toBe('done');
    expect(store.getSnapshot().byId.get('ocp')?.lastReadAt).toBe(NOW.toISOString());
  });

  it('still works when progress cannot be read', async () => {
    const { store } = createStore({ getProgress: vi.fn().mockRejectedValue(new Error('broken')) });

    await store.ready;
    store.change('srp', opened);

    expect(store.getSnapshot().byId.get('srp')?.status).toBe('inProgress');
  });

  it('writes what is left when disposed', async () => {
    const { store, source } = createStore();
    await store.ready;

    store.change('srp', opened);
    await store.dispose();

    expect(source.saveLessonProgress).toHaveBeenCalledTimes(1);
  });
});

describe('legacy import (spec 003 localStorage)', () => {
  it('turns old ticks and positions into in-progress records once', async () => {
    localStorage.setItem('lld.reader.read.srp', JSON.stringify(['rule', 'picture']));
    localStorage.setItem('lld.reader.position.srp', JSON.stringify({ anchor: 'picture', offset: 30 }));
    localStorage.setItem('lld.reader.position.ocp', JSON.stringify({ anchor: 'rule', offset: 0 }));
    localStorage.setItem('lld.reader.textSize', JSON.stringify('l'));
    const source = fakeDataSource({ getProgress: vi.fn().mockResolvedValue({ lessons: [stored], activityDays: [] }) });

    const store = new ProgressStore(source, { now: () => NOW, events: new EventTarget(), legacy: takeLegacyReading });
    await store.ready;

    expect(store.getSnapshot().byId.get('srp')).toMatchObject({
      status: 'inProgress',
      readSections: ['rule', 'picture'],
      position: { anchor: 'picture', offset: 30 },
    });
    expect(store.getSnapshot().byId.get('ocp')).toEqual(stored);
    expect(localStorage.getItem('lld.reader.read.srp')).toBeNull();
    expect(localStorage.getItem('lld.reader.textSize')).not.toBeNull();
    expect(takeLegacyReading().size).toBe(0);
  });
});

function localToday(): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${NOW.getFullYear()}-${pad(NOW.getMonth() + 1)}-${pad(NOW.getDate())}`;
}
