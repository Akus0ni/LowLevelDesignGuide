import type { ModuleMap } from '../../data/types';

/** A lesson the selected lesson links to (`out`) or that links to it (`in`), with the link's wording. */
export interface Relation {
  slug: string;
  direction: 'out' | 'in';
  label: string;
}

const DEFAULT_LABEL = 'leads to';

/** Outgoing links first, then incoming, each in the order the author wrote them. */
export function relationsOf(map: ModuleMap, slug: string): Relation[] {
  return [
    ...map.links
      .filter((link) => link.from === slug && link.to !== slug)
      .map((link) => ({ slug: link.to, direction: 'out' as const, label: link.label ?? DEFAULT_LABEL })),
    ...map.links
      .filter((link) => link.to === slug && link.from !== slug)
      .map((link) => ({ slug: link.from, direction: 'in' as const, label: link.label ?? DEFAULT_LABEL })),
  ];
}
