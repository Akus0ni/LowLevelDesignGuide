import type { SearchKind } from '../../data/types';

/** A search that led somewhere: saved when a result is opened, not on every keystroke (spec §3.5). */
export interface RecentSearch {
  query: string;
  kind: SearchKind | null;
  module: string | null;
}

export const MAX_RECENT = 8;

const KEY_PREFIX = 'lld.search.recent.';

/** Per owner on this device only, so another user on a shared device never sees them; never sent anywhere. */
const keyOf = (owner: string) => `${KEY_PREFIX}${owner}`;

export function loadRecent(owner: string): RecentSearch[] {
  try {
    const raw = localStorage.getItem(keyOf(owner));
    const list = raw ? (JSON.parse(raw) as RecentSearch[]) : [];
    return Array.isArray(list)
      ? list.filter((item) => typeof item?.query === 'string').slice(0, MAX_RECENT)
      : [];
  } catch {
    return [];
  }
}

/** Newest first; the same query (ignoring case and spaces) moves to the top instead of repeating. */
export function addRecent(owner: string, search: RecentSearch): RecentSearch[] {
  const query = search.query.trim();
  if (!query) return loadRecent(owner);
  const same = (item: RecentSearch) => item.query.trim().toLowerCase() === query.toLowerCase();
  const next = [{ ...search, query }, ...loadRecent(owner).filter((item) => !same(item))].slice(
    0,
    MAX_RECENT,
  );
  try {
    localStorage.setItem(keyOf(owner), JSON.stringify(next));
  } catch {
    // Storage full or blocked: recent searches are a convenience, so search carries on without them.
  }
  return next;
}

export function clearRecent(owner: string): void {
  try {
    localStorage.removeItem(keyOf(owner));
  } catch {
    // Nothing stored, or storage blocked.
  }
}
