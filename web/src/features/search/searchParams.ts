import type { SearchKind } from '../../data/types';

/** What the Search tab shows, kept in the URL (spec §3.4) so Back from a result returns to the same list. */
export interface SearchState {
  query: string;
  kind: SearchKind | null;
  module: string | null;
}

const KINDS: readonly SearchKind[] = ['lesson', 'cheatSheet', 'note'];

export function readSearchState(params: URLSearchParams): SearchState {
  const kind = params.get('kind');
  return {
    query: params.get('q') ?? '',
    kind: KINDS.includes(kind as SearchKind) ? (kind as SearchKind) : null,
    module: params.get('module') || null,
  };
}

export function writeSearchState(state: SearchState): URLSearchParams {
  const params = new URLSearchParams();
  if (state.query) params.set('q', state.query);
  if (state.kind) params.set('kind', state.kind);
  if (state.module) params.set('module', state.module);
  return params;
}
