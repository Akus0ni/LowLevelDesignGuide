import type { LessonProgress, ReadingPosition } from '../../data/types';

/**
 * How a lesson's progress changes (spec 005 §3.2), as pure functions: each takes the current record (or none, which
 * means "not started") and the time, and returns the new record stamped with that time. The state machine:
 *
 *   NotStarted ──open──▶ InProgress ──last section read (auto) / Mark as done (manual)──▶ Done
 *        ▲                    ▲                                                            │
 *        └──── reset ─────────┴──────────────────── Mark as not done ◀─────────────────────┘
 */

export function notStarted(lessonId: string, now: string): LessonProgress {
  return {
    lessonId,
    status: 'notStarted',
    doneBy: null,
    completedAt: null,
    readSections: [],
    position: null,
    lastReadAt: null,
    lastRevisedAt: null,
    updatedAt: now,
    deviceId: '',
  };
}

const orNew = (record: LessonProgress | undefined, lessonId: string, now: string) => record ?? notStarted(lessonId, now);

/** Opening a lesson starts it and makes it the one to continue. */
export function opened(record: LessonProgress | undefined, lessonId: string, now: string): LessonProgress {
  const current = orNew(record, lessonId, now);
  return {
    ...current,
    status: current.status === 'notStarted' ? 'inProgress' : current.status,
    lastReadAt: now,
    updatedAt: now,
  };
}

/**
 * Ticks sections as read. When a *newly* ticked section completes the outline, the lesson is done (auto). After a
 * manual "not done" every section is already ticked, so nothing new can complete it: the learner's choice stands
 * until the outline gains a section.
 */
export function sectionsRead(
  record: LessonProgress | undefined,
  lessonId: string,
  anchors: string[],
  outline: string[],
  now: string,
): LessonProgress {
  const current = opened(record, lessonId, now);
  const added = anchors.filter((anchor) => !current.readSections.includes(anchor));
  if (added.length === 0) return record ?? current;

  const readSections = [...current.readSections, ...added];
  const complete = outline.length > 0 && outline.every((anchor) => readSections.includes(anchor));
  const next = { ...current, readSections };
  return complete && current.status !== 'done'
    ? { ...next, status: 'done', doneBy: 'auto', completedAt: now }
    : next;
}

/** Remembers where the reader is. Not a "read": it doesn't move the lesson to the front of Continue. */
export function positionSaved(
  record: LessonProgress | undefined,
  lessonId: string,
  position: ReadingPosition,
  now: string,
): LessonProgress {
  const current = orNew(record, lessonId, now);
  return { ...current, position, updatedAt: now };
}

export function markedDone(record: LessonProgress | undefined, lessonId: string, now: string): LessonProgress {
  const current = orNew(record, lessonId, now);
  if (current.status === 'done') return current;
  return { ...current, status: 'done', doneBy: 'manual', completedAt: now, lastReadAt: current.lastReadAt ?? now, updatedAt: now };
}

/** Back to in progress; the ticks stay, so the outline still shows what was read. */
export function markedNotDone(record: LessonProgress | undefined, lessonId: string, now: string): LessonProgress {
  const current = orNew(record, lessonId, now);
  if (current.status !== 'done') return current;
  return { ...current, status: 'inProgress', doneBy: null, completedAt: null, updatedAt: now };
}

/** The lesson's cheat sheet was revised (end reached, or every block revealed). */
export function revised(record: LessonProgress | undefined, lessonId: string, now: string): LessonProgress {
  return { ...orNew(record, lessonId, now), lastRevisedAt: now, updatedAt: now };
}

/** Forgets everything about the lesson. A write, not a delete, so another device learns about it (spec 010). */
export function reset(record: LessonProgress | undefined, lessonId: string, now: string): LessonProgress {
  return { ...notStarted(lessonId, now), deviceId: record?.deviceId ?? '' };
}
