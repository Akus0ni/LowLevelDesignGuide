import { parseLineRanges } from '../markdown/fence';

/**
 * The link map the packer writes on a Code ↔ UML diagram (spec 008 §3.4): `links="Order=1-8;Order*--OrderLine=3"`.
 * Keys are class / participant / state names or relationship keys; values are 1-based lines of the paired code.
 */
export type CodeLinks = ReadonlyMap<string, ReadonlySet<number>>;

export function parseCodeLinks(text: string | undefined): CodeLinks {
  const links = new Map<string, ReadonlySet<number>>();
  for (const part of (text ?? '').split(';')) {
    const equal = part.lastIndexOf('=');
    if (equal <= 0) continue;
    const lines = parseLineRanges(part.slice(equal + 1));
    if (lines.size > 0) links.set(part.slice(0, equal).trim(), lines);
  }
  return links;
}

/**
 * The link a tapped code line belongs to: the one covering the fewest lines wins, so a field line picks its arrow
 * rather than the whole class around it.
 */
export function linkAtLine(links: CodeLinks, line: number): string | null {
  let best: string | null = null;
  let size = Infinity;
  for (const [key, lines] of links) {
    if (lines.has(line) && lines.size < size) {
      best = key;
      size = lines.size;
    }
  }
  return best;
}
