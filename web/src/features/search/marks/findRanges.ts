/** Code blocks, diagrams and controls aren't searched (spec §3.1), so they aren't marked either. */
const SKIPPED = 'pre, svg, script, style, textarea, input, [data-search-skip]';

const escape = (word: string) => word.replace(/[.*+?^${}()|[\]\\]/g, '\\$&').replace(/\s+/g, '\\s+');

/**
 * A pattern for the matched words as whole words, ignoring case: `inherits` marks "Inherits" but not "inheritsFrom".
 * Longer words first, so "observers" wins over "observer". Null when there is nothing to mark.
 */
export function wordPattern(words: readonly string[]): RegExp | null {
  const unique = [...new Set(words.map((w) => w.trim().toLowerCase()).filter(Boolean))].sort(
    (a, b) => b.length - a.length,
  );
  if (unique.length === 0) return null;
  return new RegExp(`(?<![\\p{L}\\p{N}])(?:${unique.map(escape).join('|')})(?![\\p{L}\\p{N}])`, 'giu');
}

/**
 * Every place the words appear in `root`'s text, in document order (spec §3.6). Folded text that is kept in the page
 * (`hidden="until-found"`) is included, so a match inside a closed fold can open it.
 */
export function findRanges(root: Node, words: readonly string[]): Range[] {
  const pattern = wordPattern(words);
  if (!pattern) return [];
  const document = root.ownerDocument ?? (root as Document);
  const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT, {
    acceptNode: (node) =>
      node.parentElement?.closest(SKIPPED) ? NodeFilter.FILTER_REJECT : NodeFilter.FILTER_ACCEPT,
  });
  const ranges: Range[] = [];
  for (let node = walker.nextNode(); node; node = walker.nextNode()) {
    const text = node.nodeValue ?? '';
    for (const match of text.matchAll(pattern)) {
      const range = document.createRange();
      range.setStart(node, match.index);
      range.setEnd(node, match.index + match[0].length);
      ranges.push(range);
    }
  }
  return ranges;
}

/** The first range at or after `element` (a section heading), or the first range at all. */
export function firstFrom(ranges: readonly Range[], element: Element | null): number {
  if (ranges.length === 0) return -1;
  if (!element) return 0;
  const index = ranges.findIndex(
    (range) => element.compareDocumentPosition(range.startContainer) & Node.DOCUMENT_POSITION_FOLLOWING,
  );
  return index < 0 ? 0 : index;
}
