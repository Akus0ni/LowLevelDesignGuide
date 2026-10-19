import { describe, expect, it } from 'vitest';
import type { LessonProgress, LessonSummary, ModuleDetail } from '../../data/types';
import { activityWeek, continueTarget, countDone, localDay, sectionsRead, type ProgressById } from './progressMath';

const lesson = (id: string, sectionCount = 4): LessonSummary => ({
  id,
  moduleSlug: 'm',
  slug: id,
  title: id,
  summary: '',
  order: 0,
  readingMinutes: 5,
  tags: [],
  sectionCount,
});

const record = (lessonId: string, status: LessonProgress['status'], lastReadAt: string | null = null): LessonProgress => ({
  lessonId,
  status,
  doneBy: status === 'done' ? 'manual' : null,
  completedAt: status === 'done' ? '2026-10-19T09:00:00Z' : null,
  readSections: ['a', 'b', 'c', 'd', 'e'],
  position: null,
  lastReadAt,
  lastRevisedAt: null,
  updatedAt: '2026-10-19T09:00:00Z',
  deviceId: 'd',
});

const byId = (...records: LessonProgress[]): ProgressById => new Map(records.map((r) => [r.lessonId, r]));
const module = (slug: string, ...lessons: LessonSummary[]): ModuleDetail => ({ slug, title: slug, summary: '', lessons });

describe('progress math', () => {
  it('counts done lessons of the current content only', () => {
    const progress = byId(record('srp', 'done'), record('removed', 'done'), record('ocp', 'inProgress'));

    expect(countDone(progress, [lesson('srp'), lesson('ocp'), lesson('lsp')])).toEqual({ done: 1, total: 3 });
    expect(countDone(byId(), [])).toEqual({ done: 0, total: 0 });
  });

  it('never reports more sections read than the lesson has', () => {
    expect(sectionsRead(byId(record('srp', 'inProgress')), lesson('srp', 4))).toBe(4);
    expect(sectionsRead(byId(), lesson('srp', 4))).toBe(0);
  });

  it('continues the in-progress lesson read most recently', () => {
    const progress = byId(
      record('srp', 'inProgress', '2026-10-18T09:00:00Z'),
      record('lsp', 'inProgress', '2026-10-19T08:00:00Z'),
    );

    expect(continueTarget(progress, [module('oop', lesson('srp'), lesson('ocp'), lesson('lsp'))])).toEqual({
      kind: 'continue',
      lesson: lesson('lsp'),
    });
  });

  it('starts the next lesson in curriculum order when nothing is in progress', () => {
    const modules = [module('start', lesson('intro')), module('oop', lesson('srp'), lesson('ocp'))];

    expect(continueTarget(byId(record('intro', 'done')), modules)).toEqual({ kind: 'start', lesson: lesson('srp') });
  });

  it('has nothing to continue once everything is done', () => {
    expect(continueTarget(byId(record('srp', 'done')), [module('oop', lesson('srp'))])).toBeNull();
  });

  it('lays out this week Monday to Sunday with the active days', () => {
    const wednesday = new Date(2026, 9, 21, 23, 30);

    const week = activityWeek(['2026-10-19', '2026-10-21', '2026-10-12'], wednesday);

    expect(week.map((d) => d.day)).toEqual([
      '2026-10-19',
      '2026-10-20',
      '2026-10-21',
      '2026-10-22',
      '2026-10-23',
      '2026-10-24',
      '2026-10-25',
    ]);
    expect(week.filter((d) => d.active).map((d) => d.label)).toEqual(['M', 'W']);
    expect(week.find((d) => d.isToday)?.day).toBe('2026-10-21');
  });

  it('starts a new week on Monday, across a month change', () => {
    const sunday = new Date(2026, 10, 1, 0, 5);

    expect(activityWeek([], sunday)[0].day).toBe('2026-10-26');
    expect(localDay(sunday)).toBe('2026-11-01');
  });
});
