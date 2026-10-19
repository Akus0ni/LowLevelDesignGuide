import { describe, expect, it } from 'vitest';
import {
  markedDone,
  markedNotDone,
  notStarted,
  opened,
  positionSaved,
  reset,
  revised,
  sectionsRead,
} from './lessonProgress';

const T1 = '2026-10-19T09:00:00.000Z';
const T2 = '2026-10-19T09:05:00.000Z';
const OUTLINE = ['rule', 'picture', 'smells'];

describe('lesson progress transitions', () => {
  it('opening a lesson starts it', () => {
    expect(opened(undefined, 'srp', T1)).toMatchObject({ status: 'inProgress', lastReadAt: T1, updatedAt: T1 });
  });

  it('opening a done lesson keeps it done', () => {
    const done = markedDone(undefined, 'srp', T1);

    expect(opened(done, 'srp', T2)).toMatchObject({ status: 'done', lastReadAt: T2 });
  });

  it('reading the last section marks the lesson done by itself', () => {
    const partly = sectionsRead(undefined, 'srp', ['rule', 'picture'], OUTLINE, T1);

    const all = sectionsRead(partly, 'srp', ['smells'], OUTLINE, T2);

    expect(partly.status).toBe('inProgress');
    expect(all).toMatchObject({ status: 'done', doneBy: 'auto', completedAt: T2, readSections: OUTLINE });
  });

  it('returns the same record when nothing new was read', () => {
    const record = sectionsRead(undefined, 'srp', ['rule'], OUTLINE, T1);

    expect(sectionsRead(record, 'srp', ['rule'], OUTLINE, T2)).toBe(record);
  });

  it('after "not done", reading again does not mark it done until a new section appears', () => {
    const doneByReading = sectionsRead(undefined, 'srp', OUTLINE, OUTLINE, T1);
    const unmarked = markedNotDone(doneByReading, 'srp', T2);

    expect(sectionsRead(unmarked, 'srp', OUTLINE, OUTLINE, T2).status).toBe('inProgress');
    expect(sectionsRead(unmarked, 'srp', ['new-section'], [...OUTLINE, 'new-section'], T2).status).toBe('done');
  });

  it('marks done by hand and back, keeping the ticks', () => {
    const ticked = sectionsRead(undefined, 'srp', ['rule'], OUTLINE, T1);

    const done = markedDone(ticked, 'srp', T2);
    const back = markedNotDone(done, 'srp', T2);

    expect(done).toMatchObject({ status: 'done', doneBy: 'manual', completedAt: T2 });
    expect(back).toMatchObject({ status: 'inProgress', doneBy: null, completedAt: null, readSections: ['rule'] });
  });

  it('saving a position does not count as reading', () => {
    const record = opened(undefined, 'srp', T1);

    expect(positionSaved(record, 'srp', { anchor: 'rule', offset: 40 }, T2)).toMatchObject({
      position: { anchor: 'rule', offset: 40 },
      lastReadAt: T1,
      updatedAt: T2,
    });
  });

  it('revising keeps the status', () => {
    expect(revised(undefined, 'srp', T1)).toMatchObject({ status: 'notStarted', lastRevisedAt: T1 });
  });

  it('reset forgets everything but is still a record, newer than before', () => {
    const done = markedDone(sectionsRead(undefined, 'srp', OUTLINE, OUTLINE, T1), 'srp', T1);

    expect(reset(done, 'srp', T2)).toEqual({ ...notStarted('srp', T2), deviceId: '' });
  });
});
