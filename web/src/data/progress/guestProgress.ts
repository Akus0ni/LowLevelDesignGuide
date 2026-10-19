import type { LessonProgress, ProgressSnapshot } from '../types';

type KeyValueStore = Pick<Storage, 'getItem' | 'setItem' | 'removeItem'>;

const PROGRESS_KEY = 'lld.progress.guest';
const DEVICE_KEY = 'lld.deviceId';

export const emptySnapshot = (): ProgressSnapshot => ({ lessons: [], activityDays: [] });

/** Per lesson the later edit wins; on a tie the incoming record wins (as Lld.Domain ProgressRules.Newer). */
export function newer(stored: LessonProgress, incoming: LessonProgress): LessonProgress {
  return Date.parse(incoming.updatedAt) >= Date.parse(stored.updatedAt) ? incoming : stored;
}

/**
 * Browser dev mode, signed out: the guest's progress in localStorage (spec 005 §3.9). It is handed to the API when
 * someone signs in, then cleared. Storage can be missing (private mode); then nothing is kept, and reading still works.
 */
export class GuestProgress {
  private readonly store: KeyValueStore | null;

  constructor(store: KeyValueStore | null = safeLocalStorage()) {
    this.store = store;
  }

  get(): ProgressSnapshot {
    try {
      const raw = this.store?.getItem(PROGRESS_KEY);
      return raw ? (JSON.parse(raw) as ProgressSnapshot) : emptySnapshot();
    } catch {
      return emptySnapshot();
    }
  }

  get isEmpty(): boolean {
    const { lessons, activityDays } = this.get();
    return lessons.length === 0 && activityDays.length === 0;
  }

  saveLessons(records: LessonProgress[]): void {
    const snapshot = this.get();
    const byId = new Map(snapshot.lessons.map((lesson) => [lesson.lessonId, lesson]));
    records.forEach((record) => {
      const stored = byId.get(record.lessonId);
      byId.set(record.lessonId, stored ? newer(stored, record) : record);
    });
    this.write({ ...snapshot, lessons: [...byId.values()] });
  }

  addDay(day: string): void {
    const snapshot = this.get();
    if (snapshot.activityDays.includes(day)) return;
    this.write({ ...snapshot, activityDays: [...snapshot.activityDays, day].sort() });
  }

  clear(): void {
    try {
      this.store?.removeItem(PROGRESS_KEY);
    } catch {
      // Nothing to clear.
    }
  }

  /** This browser's id, created once; stamped on records like the app's device id. */
  deviceId(): string {
    try {
      const stored = this.store?.getItem(DEVICE_KEY);
      if (stored) return stored;
      const created = crypto.randomUUID().split('-').join('');
      this.store?.setItem(DEVICE_KEY, created);
      return created;
    } catch {
      return 'browser';
    }
  }

  private write(snapshot: ProgressSnapshot): void {
    try {
      this.store?.setItem(PROGRESS_KEY, JSON.stringify(snapshot));
    } catch {
      // Storage full or blocked: progress is kept for this session only (the store holds it in memory).
    }
  }
}

function safeLocalStorage(): KeyValueStore | null {
  try {
    return globalThis.localStorage ?? null;
  } catch {
    return null;
  }
}
