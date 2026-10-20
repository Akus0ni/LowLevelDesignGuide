import { useCallback, useEffect, useRef, useState, type RefObject } from 'react';
import { useSearchParams } from 'react-router';
import { useDataSource } from '../../../data/DataSourceContext';
import type { SearchKind } from '../../../data/types';
import { findRanges, firstFrom } from './findRanges';

/** Re-mark this long after the page last changed (code highlighting and diagrams arrive after the text). */
const REMARK_DELAY_MS = 120;
const REDUCED_MOTION = '(prefers-reduced-motion: reduce)';

export const HIGHLIGHT = 'search';
export const HIGHLIGHT_CURRENT = 'search-current';

export interface SearchMarks {
  /** The query the page was opened with (`?q=`), or '' when there are no marks. */
  query: string;
  count: number;
  /** Index of the current mark; -1 when there are none. */
  current: number;
  next(): void;
  previous(): void;
  /** Removes the marks and drops `q` from the URL. */
  clear(): void;
}

/** The CSS Custom Highlight API draws marks without touching the DOM React owns; older engines just get no marks. */
const highlights = (): HighlightRegistry | null =>
  typeof CSS !== 'undefined' && 'highlights' in CSS && typeof Highlight !== 'undefined'
    ? CSS.highlights
    : null;

/**
 * Marks the words a search matched, across the whole page (spec §3.6): the words come from the index (stems and
 * synonyms included), the current mark starts in the section the result pointed at, and stepping to a mark inside a
 * closed fold opens it (Base UI panels open on `beforematch`, the event the browser's own find-in-page sends).
 */
export function useSearchMarks(
  root: RefObject<HTMLElement | null>,
  lessonId: string,
  kind: SearchKind,
): SearchMarks {
  const [params, setParams] = useSearchParams();
  const query = params.get('q') ?? '';
  const anchor = params.get('at');
  const source = useDataSource();
  const [words, setWords] = useState<string[]>([]);
  const [ranges, setRanges] = useState<Range[]>([]);
  const [current, setCurrent] = useState(-1);
  const key = `${query}|${anchor ?? ''}`;
  /** Set once the learner steps: from then on re-marks keep their place instead of starting over. */
  const moved = useRef<string | null>(null);

  useEffect(() => {
    if (!query) {
      setWords([]);
      return;
    }
    let live = true;
    source
      .search({ query, lessonId, kind })
      .then((results) => live && setWords(results.words))
      .catch(() => live && setWords([]));
    return () => {
      live = false;
    };
  }, [source, query, lessonId, kind]);

  useEffect(() => {
    const element = root.current;
    if (!element || words.length === 0) {
      setRanges([]);
      return;
    }
    let timer: ReturnType<typeof setTimeout> | undefined;
    const mark = () => setRanges(findRanges(element, words));
    mark();
    const observer = new MutationObserver(() => {
      clearTimeout(timer);
      timer = setTimeout(mark, REMARK_DELAY_MS);
    });
    observer.observe(element, { childList: true, subtree: true, characterData: true });
    return () => {
      observer.disconnect();
      clearTimeout(timer);
    };
  }, [root, words]);

  // Until the learner steps, every re-mark starts again in the section the result pointed at: the page fills in over
  // a few frames (highlighted code, diagrams), so the first marks found may not reach that section yet.
  useEffect(() => {
    if (ranges.length === 0) {
      setCurrent(-1);
      return;
    }
    if (moved.current === key) {
      setCurrent((index) => Math.min(Math.max(index, 0), ranges.length - 1));
      return;
    }
    // Inside the marked text: the same heading id can also appear elsewhere on the page (outline, sheets).
    const heading = anchor
      ? ([...(root.current?.querySelectorAll('[id]') ?? [])].find((e) => e.id === anchor) ?? null)
      : null;
    const start = firstFrom(ranges, heading);
    setCurrent(start);
    if (insideFold(ranges[start])) show(ranges[start]);
  }, [ranges, key, anchor, root]);

  useEffect(() => {
    const registry = highlights();
    if (!registry || ranges.length === 0) return;
    registry.set(HIGHLIGHT, new Highlight(...ranges));
    if (ranges[current]) registry.set(HIGHLIGHT_CURRENT, new Highlight(ranges[current]));
    return () => {
      registry.delete(HIGHLIGHT);
      registry.delete(HIGHLIGHT_CURRENT);
    };
  }, [ranges, current]);

  const go = useCallback(
    (step: 1 | -1) => {
      if (ranges.length === 0) return;
      const index = (current + step + ranges.length) % ranges.length;
      moved.current = key;
      setCurrent(index);
      show(ranges[index]);
    },
    [ranges, current, key],
  );

  const clear = useCallback(() => {
    const next = new URLSearchParams(params);
    next.delete('q');
    moved.current = null;
    setParams(next, { replace: true });
  }, [params, setParams]);

  return {
    query: ranges.length > 0 ? query : '',
    count: ranges.length,
    current,
    next: () => go(1),
    previous: () => go(-1),
    clear,
  };
}

function foldOf(range: Range | undefined): HTMLElement | null {
  return range?.startContainer.parentElement?.closest<HTMLElement>('[hidden="until-found"]') ?? null;
}

const insideFold = (range: Range | undefined) => foldOf(range) !== null;

/** Opens the fold that holds the mark, then brings the mark to the middle of the screen. */
function show(range: Range | undefined): void {
  if (!range) return;
  foldOf(range)?.dispatchEvent(new Event('beforematch', { bubbles: true }));
  requestAnimationFrame(() => {
    const smooth = !window.matchMedia(REDUCED_MOTION).matches;
    range.startContainer.parentElement?.scrollIntoView({
      block: 'center',
      behavior: smooth ? 'smooth' : 'auto',
    });
  });
}
