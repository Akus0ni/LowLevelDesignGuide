import type { DataSource, LessonProgress } from '../../data/types';
import { takeLegacyReading, type LegacyReading } from './legacyImport';
import { localDay, type ProgressById } from './progressMath';

/** Quiet time before queued changes are written: scrolling saves a position many times a minute. */
export const FLUSH_DELAY_MS = 1000;

export interface ProgressState {
  byId: ProgressById;
  activityDays: readonly string[];
}

export type LessonChange = (record: LessonProgress | undefined, lessonId: string, now: string) => LessonProgress;

interface StoreOptions {
  now?: () => Date;
  events?: EventTarget;
  legacy?: () => Map<string, LegacyReading>;
}

/**
 * One owner's progress in memory (spec 005 plan §2.6). Every change updates the snapshot at once, so Learn, the module
 * page, Revise and Profile re-render together (Observer, via `useSyncExternalStore`), then the record is queued and
 * written after a quiet second, when the page is hidden, or before the owner changes. A failed write stays queued.
 */
export class ProgressStore {
  readonly ready: Promise<void>;
  private state: ProgressState = { byId: new Map(), activityDays: [] };
  private readonly listeners = new Set<() => void>();
  private readonly queuedLessons = new Map<string, LessonProgress>();
  private readonly queuedDays = new Set<string>();
  private readonly source: DataSource;
  private readonly now: () => Date;
  private readonly events?: EventTarget;
  private timer: ReturnType<typeof setTimeout> | undefined;
  private flushing: Promise<void> = Promise.resolve();
  /** Changes asked for before the owner's progress loaded; applied on top of it, in order, once it has. */
  private early: (() => void)[] | null = [];

  constructor(source: DataSource, options: StoreOptions = {}) {
    this.source = source;
    this.now = options.now ?? (() => new Date());
    this.events = options.events ?? (typeof window === 'undefined' ? undefined : window);
    this.ready = this.load(options.legacy ?? takeLegacyReading);
    this.events?.addEventListener('visibilitychange', this.flushWhenHidden);
    this.events?.addEventListener('pagehide', this.flushNow);
  }

  readonly subscribe = (listener: () => void): (() => void) => {
    this.listeners.add(listener);
    return () => this.listeners.delete(listener);
  };

  readonly getSnapshot = (): ProgressState => this.state;

  /** Applies a change from `lessonProgress.ts` to one lesson; nothing is saved when the record didn't change. */
  change(lessonId: string, apply: LessonChange): void {
    const now = this.now().toISOString();
    this.whenLoaded(() => {
      const before = this.state.byId.get(lessonId);
      const after = apply(before, lessonId, now);
      if (after !== before) this.put([after]);
    });
  }

  /** The same change for several lessons in one update (e.g. a module reset). */
  changeAll(lessonIds: readonly string[], apply: LessonChange): void {
    const now = this.now().toISOString();
    this.whenLoaded(() => this.put(lessonIds.map((id) => apply(this.state.byId.get(id), id, now))));
  }

  /** Remembers that the learner studied today (local date), once per day. */
  recordActivity(): void {
    const today = localDay(this.now());
    this.whenLoaded(() => this.addDay(today));
  }

  private addDay(today: string): void {
    if (this.state.activityDays.includes(today)) return;
    this.state = { ...this.state, activityDays: [...this.state.activityDays, today].sort() };
    this.queuedDays.add(today);
    this.notifyAndSchedule();
  }

  /** Writes everything queued. Safe to call any time; calls run one after another. */
  flush(): Promise<void> {
    this.flushing = this.flushing.then(() => this.writeQueued());
    return this.flushing;
  }

  /** Before the owner changes: stop listening and write what is left. */
  dispose(): Promise<void> {
    clearTimeout(this.timer);
    this.events?.removeEventListener('visibilitychange', this.flushWhenHidden);
    this.events?.removeEventListener('pagehide', this.flushNow);
    return this.flush();
  }

  private async load(takeLegacy: () => Map<string, LegacyReading>): Promise<void> {
    try {
      const snapshot = await this.source.getProgress();
      this.state = {
        byId: new Map(snapshot.lessons.map((lesson) => [lesson.lessonId, lesson])),
        activityDays: snapshot.activityDays,
      };
    } catch {
      // Progress couldn't be read: reading still works, and changes from now on are kept and retried.
    }
    this.importLegacy(takeLegacy());
    const early = this.early ?? [];
    this.early = null;
    early.forEach((run) => run());
    this.notify();
  }

  private whenLoaded(run: () => void): void {
    if (this.early) this.early.push(run);
    else run();
  }

  /** Spec 003's device-only ticks and positions become in-progress records for whoever owns progress now. */
  private importLegacy(legacy: Map<string, LegacyReading>): void {
    const now = this.now().toISOString();
    const imported = [...legacy]
      .filter(([lessonId]) => !this.state.byId.has(lessonId))
      .map(([lessonId, reading]): LessonProgress => ({
        lessonId,
        status: 'inProgress',
        doneBy: null,
        completedAt: null,
        readSections: reading.readSections,
        position: reading.position,
        lastReadAt: now,
        lastRevisedAt: null,
        updatedAt: now,
        deviceId: '',
      }));
    if (imported.length > 0) this.put(imported);
  }

  private put(records: LessonProgress[]): void {
    const byId = new Map(this.state.byId);
    records.forEach((record) => {
      byId.set(record.lessonId, record);
      this.queuedLessons.set(record.lessonId, record);
    });
    this.state = { ...this.state, byId };
    this.notifyAndSchedule();
  }

  private notifyAndSchedule(): void {
    this.notify();
    clearTimeout(this.timer);
    this.timer = setTimeout(this.flushNow, FLUSH_DELAY_MS);
  }

  private notify(): void {
    this.listeners.forEach((listener) => listener());
  }

  private async writeQueued(): Promise<void> {
    const lessons = [...this.queuedLessons.values()];
    const days = [...this.queuedDays];
    this.queuedLessons.clear();
    this.queuedDays.clear();
    try {
      if (lessons.length > 0) await this.source.saveLessonProgress(lessons);
    } catch {
      lessons.forEach((record) => {
        if (!this.queuedLessons.has(record.lessonId)) this.queuedLessons.set(record.lessonId, record);
      });
    }
    for (const day of days) {
      try {
        await this.source.recordActivity(day);
      } catch {
        this.queuedDays.add(day);
      }
    }
  }

  private readonly flushNow = () => void this.flush();

  private readonly flushWhenHidden = () => {
    if (document.visibilityState === 'hidden') void this.flush();
  };
}
