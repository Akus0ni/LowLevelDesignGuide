import type { SearchHit, SearchSnippet } from '../../data/types';

/** One module's hits, in result order. `moduleSlug` '' holds notes on lessons that left the content. */
export interface ResultGroup {
  moduleSlug: string;
  hits: SearchHit[];
}

export interface GroupedResults {
  /** The first hit, shown on its own (spec Q2); null when nothing matched. */
  best: SearchHit | null;
  /** The other hits by module: groups ordered by their best hit, removed lessons last, rows in rank order. */
  groups: ResultGroup[];
}

/** Spec §3.4 / Q2: a Best match on top, then module groups ordered by their best hit. Hits arrive ranked. */
export function groupResults(hits: readonly SearchHit[]): GroupedResults {
  const [best = null, ...rest] = hits;
  const groups = new Map<string, SearchHit[]>();
  for (const hit of rest) {
    const group = groups.get(hit.moduleSlug);
    if (group) group.push(hit);
    else groups.set(hit.moduleSlug, [hit]);
  }
  const ordered = [...groups].map(([moduleSlug, list]) => ({ moduleSlug, hits: list }));
  return {
    best,
    groups: [...ordered.filter((g) => g.moduleSlug !== ''), ...ordered.filter((g) => g.moduleSlug === '')],
  };
}

/**
 * Where a hit opens (spec §3.6): the lesson at its section, the cheat sheet at its section, or the note in its lesson,
 * each carrying the query so the reader can mark the words. A note on a removed lesson opens the Notes tab.
 */
export function hitLink(hit: SearchHit, query: string): string {
  if (hit.moduleSlug === '') return '/notes';
  const params = new URLSearchParams();
  if (hit.anchor) params.set('at', hit.anchor);
  if (hit.kind === 'note' && hit.noteId) params.set('note', hit.noteId);
  params.set('q', query.trim());
  const base = hit.kind === 'cheatSheet' ? '/revise' : '/learn';
  return `${base}/${hit.moduleSlug}/${hit.lessonSlug}?${params}`;
}

export interface SnippetPart {
  text: string;
  marked: boolean;
}

/** The snippet as plain and marked runs; ranges that overlap or run past the text are clipped, never thrown on. */
export function snippetParts(snippet: SearchSnippet): SnippetPart[] {
  const parts: SnippetPart[] = [];
  let at = 0;
  for (const [start, length] of [...snippet.marks].sort((a, b) => a[0] - b[0])) {
    const from = Math.max(start, at);
    const to = Math.min(start + length, snippet.text.length);
    if (to <= from) continue;
    if (from > at) parts.push({ text: snippet.text.slice(at, from), marked: false });
    parts.push({ text: snippet.text.slice(from, to), marked: true });
    at = to;
  }
  if (at < snippet.text.length) parts.push({ text: snippet.text.slice(at), marked: false });
  return parts;
}
