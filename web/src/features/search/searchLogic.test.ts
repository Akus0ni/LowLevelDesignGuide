import { afterEach, describe, expect, it } from 'vitest';
import type { SearchHit } from '../../data/types';
import { addRecent, clearRecent, loadRecent, MAX_RECENT } from './recentSearches';
import { readSearchState, writeSearchState } from './searchParams';
import { groupResults, hitLink, snippetParts } from './searchResults';
import { isSearchable } from './useSearch';

const hit = (over: Partial<SearchHit>): SearchHit => ({
  kind: 'lesson',
  moduleSlug: 'gof-patterns',
  lessonId: 'decorator',
  lessonSlug: 'decorator',
  lessonTitle: 'Decorator',
  anchor: '',
  heading: '',
  noteId: null,
  snippet: { text: '', marks: [] },
  titleMatch: false,
  ...over,
});

afterEach(() => localStorage.clear());

describe('groupResults (spec Q2)', () => {
  it('puts the first hit on its own, then groups by module in the order their best hit came, removed lessons last', () => {
    const hits = [
      hit({ lessonId: 'a' }),
      hit({ moduleSlug: '', lessonId: 'gone', kind: 'note' }),
      hit({ moduleSlug: 'uml', lessonId: 'b' }),
      hit({ lessonId: 'c' }),
      hit({ moduleSlug: 'uml', lessonId: 'd' }),
    ];

    const { best, groups } = groupResults(hits);

    expect(best?.lessonId).toBe('a');
    expect(groups.map((g) => [g.moduleSlug, g.hits.map((h) => h.lessonId)])).toEqual([
      ['uml', ['b', 'd']],
      ['gof-patterns', ['c']],
      ['', ['gone']],
    ]);
  });

  it('has no best match and no groups for no hits', () => {
    expect(groupResults([])).toEqual({ best: null, groups: [] });
  });
});

describe('hitLink (spec §3.6)', () => {
  it('opens a lesson at its section with the query, a cheat sheet in Revise, a note in its lesson', () => {
    expect(hitLink(hit({ anchor: 'the-idea' }), ' decor ')).toBe('/learn/gof-patterns/decorator?at=the-idea&q=decor');
    expect(hitLink(hit({ kind: 'cheatSheet', anchor: 'shape' }), 'wrap')).toBe('/revise/gof-patterns/decorator?at=shape&q=wrap');
    expect(hitLink(hit({ kind: 'note', noteId: 'n1' }), 'retry')).toBe('/learn/gof-patterns/decorator?note=n1&q=retry');
  });

  it('opens the Notes tab for a note on a removed lesson', () => {
    expect(hitLink(hit({ kind: 'note', moduleSlug: '', noteId: 'n1' }), 'x')).toBe('/notes');
  });
});

describe('snippetParts', () => {
  it('splits text into plain and marked runs', () => {
    expect(snippetParts({ text: 'a class inherits from Base', marks: [[8, 8], [22, 4]] })).toEqual([
      { text: 'a class ', marked: false },
      { text: 'inherits', marked: true },
      { text: ' from ', marked: false },
      { text: 'Base', marked: true },
    ]);
  });

  it('clips marks that overlap, come out of order or run past the end', () => {
    expect(snippetParts({ text: 'abcdef', marks: [[4, 9], [0, 3], [1, 3]] })).toEqual([
      { text: 'abc', marked: true },
      { text: 'd', marked: true },
      { text: 'ef', marked: true },
    ]);
  });
});

describe('search state in the URL', () => {
  it('reads and writes q, kind and module, ignoring an unknown kind', () => {
    expect(readSearchState(new URLSearchParams('q=srp&kind=note&module=oop-solid'))).toEqual({ query: 'srp', kind: 'note', module: 'oop-solid' });
    expect(readSearchState(new URLSearchParams('kind=video'))).toEqual({ query: '', kind: null, module: null });
    expect(writeSearchState({ query: 'srp', kind: null, module: 'uml' }).toString()).toBe('q=srp&module=uml');
  });
});

describe('isSearchable (spec §3.2)', () => {
  it('needs two letters or digits; punctuation does not count', () => {
    expect(isSearchable('a')).toBe(false);
    expect(isSearchable('"*(')).toBe(false);
    expect(isSearchable('di')).toBe(true);
    expect(isSearchable('#a1')).toBe(true);
  });
});

describe('recent searches (spec §3.5)', () => {
  it('keeps the last eight, newest first, without repeats', () => {
    for (let i = 0; i < 10; i++) addRecent('guest', { query: `q${i}`, kind: null, module: null });
    addRecent('guest', { query: ' Q5 ', kind: 'note', module: null });

    const recent = loadRecent('guest');

    expect(recent).toHaveLength(MAX_RECENT);
    expect(recent[0]).toEqual({ query: 'Q5', kind: 'note', module: null });
    expect(recent.filter((r) => r.query.toLowerCase() === 'q5')).toHaveLength(1);
  });

  it('keeps each owner apart and clears one owner only', () => {
    addRecent('guest', { query: 'mine', kind: null, module: null });
    addRecent('user-2', { query: 'theirs', kind: null, module: null });

    clearRecent('guest');

    expect(loadRecent('guest')).toEqual([]);
    expect(loadRecent('user-2').map((r) => r.query)).toEqual(['theirs']);
  });

  it('survives broken storage', () => {
    localStorage.setItem('lld.search.recent.guest', '{not json');

    expect(loadRecent('guest')).toEqual([]);
  });
});
