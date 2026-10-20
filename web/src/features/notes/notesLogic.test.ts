import { describe, expect, it } from 'vitest';
import type { Bookmark, Note } from '../../data/types';
import { buildExport, exportFileName } from './exportNotes';
import { deleteNote, editNote, newNote, restoreNote, setBookmark } from './noteRecords';
import { groupBookmarks, groupNotes, readableAnchor, sectionsOf, tagCounts, type NotesIndex } from './notesQuery';
import { applyStarter, STARTERS } from './starters';

const at = (minutes: number) => new Date(Date.UTC(2026, 9, 20, 9, minutes)).toISOString();

let ids = 0;
const note = (lessonId: string, body: string, anchor = '', minutes = 0, updated = minutes): Note => ({
  ...newNote({ lessonId, anchor }, body, at(minutes), `n-${++ids}`),
  updatedAt: at(updated),
});

const index: NotesIndex = {
  modules: [
    { slug: 'oop-solid', title: 'OOP & SOLID', order: 1 },
    { slug: 'start', title: 'Start here', order: 0 },
  ],
  lessons: new Map([
    ['srp', { id: 'srp', moduleSlug: 'oop-solid', slug: 'srp', title: 'Single Responsibility', order: 5, sections: [
      { anchor: 'the-rule', title: 'The rule' },
      { anchor: 'see-it', title: 'See it' },
    ] }],
    ['ocp', { id: 'ocp', moduleSlug: 'oop-solid', slug: 'ocp', title: 'Open/Closed', order: 6 }],
    ['how-to-use', { id: 'how-to-use', moduleSlug: 'start', slug: 'how-to-use', title: 'How to use', order: 0, sections: [] }],
  ]),
};

describe('note records', () => {
  it('stamps every edit, deletes softly and undoes', () => {
    const written = newNote({ lessonId: 'srp', anchor: 'the-rule' }, 'first', at(0), 'n');
    const edited = editNote(written, 'second', at(1));
    const deleted = deleteNote(edited, at(2));
    const restored = restoreNote(deleted, at(3));

    expect(editNote(written, 'first', at(5))).toBe(written);
    expect(edited).toMatchObject({ body: 'second', updatedAt: at(1), createdAt: at(0) });
    expect(deleted).toMatchObject({ deletedAt: at(2), updatedAt: at(2), body: 'second' });
    expect(restored).toMatchObject({ deletedAt: null, updatedAt: at(3) });
  });

  it('bookmarks and removes, keeping when it was first made', () => {
    const target = { kind: 'section' as const, lessonId: 'srp', anchor: 'the-rule' };
    const on = setBookmark(undefined, target, true, at(0));
    const off = setBookmark(on, target, false, at(4));

    expect(on).toMatchObject({ ...target, createdAt: at(0), deletedAt: null });
    expect(off).toMatchObject({ createdAt: at(0), deletedAt: at(4), updatedAt: at(4) });
    expect(setBookmark(off, target, true, at(5)).deletedAt).toBeNull();
  });
});

describe('starters', () => {
  it('fill in the outline and tag, with the cursor after the first prompt', () => {
    const { body, cursor } = applyStarter(STARTERS[1]);

    expect(body).toBe('#interview\n\n**Situation:** \n**Task:** \n**Action:** \n**Result:** \n');
    expect(body.slice(0, cursor)).toBe('#interview\n\n**Situation:** ');
    expect(STARTERS.map((s) => s.tag)).toEqual(['work', 'interview', 'gotcha']);
  });
});

describe('notes query', () => {
  it('orders a lesson as whole lesson, then sections in reading order, oldest first; lost sections join the whole lesson', () => {
    const notes = [
      note('srp', 'see', 'see-it', 1),
      note('srp', 'rule 2', 'the-rule', 3),
      note('srp', 'rule 1', 'the-rule', 2),
      note('srp', 'gone', 'renamed-section', 4),
      note('srp', 'whole', '', 5),
    ];

    const groups = sectionsOf(notes, index.lessons.get('srp')!);

    expect(groups.map((g) => [g.anchor, g.notes.map((n) => n.body)])).toEqual([
      ['', ['gone', 'whole']],
      ['the-rule', ['rule 1', 'rule 2']],
      ['see-it', ['see']],
    ]);
    expect(groups[0].missing.has(notes[3].id)).toBe(true);
    expect(groups[0].missing.size).toBe(1);
  });

  it('keeps notes visible by anchor while the outline is loading', () => {
    const groups = sectionsOf([note('ocp', 'a', 'extend-it'), note('ocp', 'b')], index.lessons.get('ocp')!);

    expect(groups.map((g) => g.anchor)).toEqual(['', 'extend-it']);
    expect(readableAnchor('extend-it')).toBe('Extend it');
  });

  it('groups modules in course order, the latest-edited lesson first, removed lessons last, deleted notes out', () => {
    const notes = [
      note('srp', 'old', '', 0, 1),
      note('ocp', 'new', '', 0, 9),
      note('how-to-use', 'start'),
      note('left-the-course', 'kept'),
      deleteNote(note('srp', 'deleted'), at(2)),
    ];

    const groups = groupNotes(notes, index);

    expect(groups.map((g) => [g.module?.slug ?? null, g.lessons.map((l) => l.lessonId)])).toEqual([
      ['start', ['how-to-use']],
      ['oop-solid', ['ocp', 'srp']],
      [null, ['left-the-course']],
    ]);
  });

  it('filters by every chosen tag and by text, ignoring case', () => {
    const notes = [note('srp', '#weak and #work'), note('srp', 'Only #weak'), note('ocp', 'Factory #WORK')];

    const bodies = (filter: { tags: string[]; text: string }) =>
      groupNotes(notes, index, filter).flatMap((g) => g.lessons.flatMap((l) => l.sections.flatMap((s) => s.notes.map((n) => n.body))));

    expect(bodies({ tags: ['weak', 'work'], text: '' })).toEqual(['#weak and #work']);
    expect(bodies({ tags: ['work'], text: 'factory' })).toEqual(['Factory #WORK']);
    expect(tagCounts(notes)).toEqual([
      { tag: 'weak', count: 2 },
      { tag: 'work', count: 2 },
    ]);
  });

  it('groups bookmarks: lesson, then sections in reading order, then the cheat sheet', () => {
    const mark = (kind: Bookmark['kind'], lessonId: string, anchor = ''): Bookmark =>
      setBookmark(undefined, { kind, lessonId, anchor }, true, at(0));
    const removed = setBookmark(mark('lesson', 'ocp'), { kind: 'lesson', lessonId: 'ocp', anchor: '' }, false, at(1));

    const groups = groupBookmarks(
      [mark('cheatSheet', 'srp'), mark('section', 'srp', 'see-it'), mark('section', 'srp', 'the-rule'), mark('lesson', 'srp'), removed],
      index,
    );

    expect(groups).toHaveLength(1);
    expect(groups[0].lessons[0].bookmarks.map((b) => `${b.kind}:${b.anchor}`)).toEqual([
      'lesson:',
      'section:the-rule',
      'section:see-it',
      'cheatSheet:',
    ]);
  });
});

describe('export', () => {
  it('writes modules and lessons in course order, sections by title, notes oldest first, removed lessons last', () => {
    const notes = [
      note('ocp', 'Extend, don’t edit', '', 0, 9),
      note('srp', 'Second on the rule', 'the-rule', 2),
      note('srp', 'First on the rule', 'the-rule', 1),
      note('gone', 'Still mine'),
      deleteNote(note('srp', 'Deleted'), at(3)),
    ];

    expect(buildExport(notes, index)).toBe(
      [
        '# My LLD notes',
        '## OOP & SOLID',
        '### Single Responsibility',
        '#### The rule',
        'First on the rule\n\n---\n\nSecond on the rule',
        '### Open/Closed',
        '#### Whole lesson',
        'Extend, don’t edit',
        '## Removed lessons',
        '### gone',
        '#### Whole lesson',
        'Still mine',
      ].join('\n\n') + '\n',
    );
    expect(exportFileName(new Date(2026, 9, 5))).toBe('lld-notes-2026-10-05.md');
  });
});
