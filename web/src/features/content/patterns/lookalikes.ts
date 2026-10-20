import type { Lookalike } from '../../../data/types';

const SEPARATOR = '-vs-';

/** The compare route's key: the slugs in the order written, e.g. `strategy-vs-state` (spec 006 §3.7). */
export function lookalikeKey(lookalike: Lookalike): string {
  return lookalike.lessons.join(SEPARATOR);
}

export function comparePath(moduleSlug: string, lookalike: Lookalike): string {
  return `/learn/${moduleSlug}/compare/${lookalikeKey(lookalike)}`;
}

export function findLookalike(lookalikes: readonly Lookalike[] | null | undefined, key: string): Lookalike | undefined {
  return lookalikes?.find((lookalike) => lookalikeKey(lookalike) === key);
}

/** The groups a lesson belongs to, in the order the author wrote them. */
export function lookalikesOf(lookalikes: readonly Lookalike[] | null | undefined, slug: string): Lookalike[] {
  return (lookalikes ?? []).filter((lookalike) => lookalike.lessons.includes(slug));
}

/** Every lesson a lesson is confused with, once each. */
export function partnersOf(lookalikes: readonly Lookalike[] | null | undefined, slug: string): string[] {
  return [...new Set(lookalikesOf(lookalikes, slug).flatMap((l) => l.lessons).filter((other) => other !== slug))];
}
