import type { LessonProgress, LessonStatus, LessonSummary, ModuleDetail } from '../../data/types';

/** Progress by lesson id. A lesson with no record is not started. */
export type ProgressById = ReadonlyMap<string, LessonProgress>;

export interface Count {
  done: number;
  total: number;
}

export const statusOf = (byId: ProgressById, lessonId: string): LessonStatus => byId.get(lessonId)?.status ?? 'notStarted';

/** Sections read, never more than the lesson has today (ticks of headings that were since renamed don't count). */
export function sectionsRead(byId: ProgressById, lesson: Pick<LessonSummary, 'id' | 'sectionCount'>): number {
  return Math.min(byId.get(lesson.id)?.readSections.length ?? 0, lesson.sectionCount);
}

/** Done lessons out of those in the content now; progress for lessons that left the content is ignored. */
export function countDone(byId: ProgressById, lessons: readonly Pick<LessonSummary, 'id'>[]): Count {
  return { done: lessons.filter((lesson) => statusOf(byId, lesson.id) === 'done').length, total: lessons.length };
}

export interface ContinueTarget {
  /** `continue`: a lesson in progress; `start`: the next one not started. */
  kind: 'continue' | 'start';
  lesson: LessonSummary;
}

/**
 * What the Continue card opens (spec 005 §3.3): the in-progress lesson read most recently; else the first lesson not
 * started, in curriculum order; null once everything is done.
 */
export function continueTarget(byId: ProgressById, modules: readonly ModuleDetail[]): ContinueTarget | null {
  const lessons = modules.flatMap((module) => module.lessons);
  const latest = lessons
    .filter((lesson) => statusOf(byId, lesson.id) === 'inProgress')
    .sort((a, b) => readAt(byId, b) - readAt(byId, a))[0];
  if (latest) return { kind: 'continue', lesson: latest };

  const next = lessons.find((lesson) => statusOf(byId, lesson.id) === 'notStarted');
  return next ? { kind: 'start', lesson: next } : null;
}

const readAt = (byId: ProgressById, lesson: LessonSummary) => Date.parse(byId.get(lesson.id)?.lastReadAt ?? '') || 0;

export interface ActivityDay {
  /** yyyy-MM-dd, local. */
  day: string;
  /** One letter, Monday first. */
  label: string;
  active: boolean;
  isToday: boolean;
}

const WEEKDAY_LETTERS = ['M', 'T', 'W', 'T', 'F', 'S', 'S'];

/** The learner's local date as yyyy-MM-dd. */
export function localDay(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

/** This week, Monday to Sunday, with the days that had reading or revising (spec 005 §3.6). It never "breaks". */
export function activityWeek(activityDays: readonly string[], today: Date): ActivityDay[] {
  const active = new Set(activityDays);
  const sinceMonday = (today.getDay() + 6) % 7;
  return WEEKDAY_LETTERS.map((label, index) => {
    const date = new Date(today.getFullYear(), today.getMonth(), today.getDate() - sinceMonday + index);
    const day = localDay(date);
    return { day, label, active: active.has(day), isToday: index === sinceMonday };
  });
}
