import { useEffect, useRef, useState } from 'react';
import { useDataSource } from '../../data/DataSourceContext';
import type { SearchKind, SearchResults } from '../../data/types';

/** Typing pauses this long before a search runs; a local search takes a few ms, so this only saves wasted work. */
export const SEARCH_DEBOUNCE_MS = 120;

/** Searching starts at 2 letters or digits (spec §3.2); below that the box shows recent searches and suggestions. */
export const MIN_QUERY_LENGTH = 2;

export const isSearchable = (query: string) =>
  query.replace(/[^\p{L}\p{N}]/gu, '').length >= MIN_QUERY_LENGTH;

export interface SearchOptions {
  kind?: SearchKind | null;
  module?: string | null;
  limit?: number;
}

export interface SearchState {
  /** The latest answer, kept while the next one loads so the list never flashes empty. */
  results: SearchResults | null;
  /** The query `results` belong to. */
  answered: string | null;
  error: boolean;
}

/**
 * Searches as the learner types (spec §3.4): a short debounce, and the latest query always wins: an older answer that
 * arrives late is dropped. A query too short to search is sent at once, to get the suggestions.
 */
export function useSearch(
  query: string,
  { kind = null, module = null, limit }: SearchOptions = {},
): SearchState {
  const source = useDataSource();
  const [state, setState] = useState<SearchState>({ results: null, answered: null, error: false });
  const latest = useRef(0);

  useEffect(() => {
    const ticket = ++latest.current;
    const run = () =>
      source
        .search({ query, kind: kind ?? undefined, module: module ?? undefined, limit })
        .then((results) => {
          if (ticket === latest.current) setState({ results, answered: query, error: false });
        })
        .catch(() => {
          if (ticket === latest.current) setState((before) => ({ ...before, answered: query, error: true }));
        });
    if (!isSearchable(query)) {
      void run();
      return;
    }
    const timer = setTimeout(() => void run(), SEARCH_DEBOUNCE_MS);
    return () => clearTimeout(timer);
  }, [source, query, kind, module, limit]);

  return state;
}
