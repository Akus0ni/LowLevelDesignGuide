import type { Element, ElementContent } from 'hast';

/**
 * A code fence as the packer left it (spec 004 §3.3–3.4): the language, then flags and `key="value"` attributes, e.g.
 * ```` ```csharp before title="Before" mark="1,5" ````. Snippets are already inlined, so `snippet` never reaches here.
 */
export interface FenceMeta {
  flags: string[];
  title?: string;
  /** 1-based lines to mark: removed (before), added (after) or "look here" (any other fence). */
  marks: ReadonlySet<number>;
}

export interface Fence {
  code: string;
  language?: string;
  meta: FenceMeta;
}

export type FenceSide = 'before' | 'after';

const ATTRIBUTE = /([A-Za-z][\w-]*)(?:="([^"]*)"|=(\S+))?/g;

export function parseFenceMeta(meta: string | null | undefined): FenceMeta {
  const flags: string[] = [];
  const attributes = new Map<string, string>();
  for (const [, name, quoted, bare] of (meta ?? '').matchAll(ATTRIBUTE)) {
    const value = quoted ?? bare;
    if (value === undefined) flags.push(name);
    else attributes.set(name, value);
  }
  return { flags, title: attributes.get('title'), marks: parseLineRanges(attributes.get('mark') ?? '') };
}

/** `3-5,9` → 3, 4, 5, 9. Anything unreadable is skipped (the packer already validated it). */
export function parseLineRanges(text: string): ReadonlySet<number> {
  const lines = new Set<number>();
  for (const part of text.split(',')) {
    if (part.trim() === '') continue;
    const [first, last = first] = part.trim().split('-').map(Number);
    if (!Number.isInteger(first) || !Number.isInteger(last) || first < 1) continue;
    for (let line = first; line <= last; line++) lines.add(line);
  }
  return lines;
}

export function sideOf(meta: FenceMeta): FenceSide | null {
  if (meta.flags.includes('before')) return 'before';
  if (meta.flags.includes('after')) return 'after';
  return null;
}

/** How a marked line reads: removed (Before), added (After), or "look here" in a normal block. */
export type MarkKind = 'del' | 'add' | 'mark';

export function markKindOf(side: FenceSide | null): MarkKind {
  return side === 'before' ? 'del' : side === 'after' ? 'add' : 'mark';
}

/** The fence a `<pre>` holds (remark-rehype keeps the info string after the language as `code.data.meta`). */
export function fenceOf(pre: Element | undefined): Fence | null {
  const code = pre?.children.find(
    (child): child is Element => child.type === 'element' && child.tagName === 'code',
  );
  if (!code) return null;
  const classes = (code.properties.className as string[] | undefined) ?? [];
  const language = classes.find((name) => name.startsWith('language-'))?.slice('language-'.length);
  const meta = (code.data as { meta?: string | null } | undefined)?.meta;
  return {
    code: code.children.map(textOf).join('').replace(/\n$/, ''),
    language,
    meta: parseFenceMeta(meta),
  };
}

export function textOf(node: ElementContent): string {
  if (node.type === 'text') return node.value;
  return node.type === 'element' ? node.children.map(textOf).join('') : '';
}
