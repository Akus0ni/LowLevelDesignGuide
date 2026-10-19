import type { Element, ElementContent, Root, RootContent } from 'hast';
import { fenceOf, sideOf } from './fence';

/**
 * Small tree rewrites for spec 004's reader features. Each one only adds wrapper elements or data attributes; the
 * components in MarkdownView turn those into React components.
 */

type Parent = Root | Element;

function isElement(node: RootContent | ElementContent | undefined, tagName?: string): node is Element {
  return node?.type === 'element' && (tagName === undefined || node.tagName === tagName);
}

function isBlank(node: RootContent | ElementContent): boolean {
  return node.type === 'text' && node.value.trim() === '';
}

function forEachParent(parent: Parent, visit: (parent: Parent) => void): void {
  visit(parent);
  for (const child of parent.children) if (isElement(child)) forEachParent(child, visit);
}

/** A `before` fence directly followed by an `after` fence → `<div data-compare>` holding both `<pre>`s. */
export function rehypeCompare() {
  return (tree: Root) =>
    forEachParent(tree, (parent) => {
      // The wrapper made below is visited next; its pair is already grouped.
      if (parent.type === 'element' && parent.properties.dataCompare !== undefined) return;
      const children = parent.children as ElementContent[];
      for (let i = 0; i < children.length; i++) {
        const before = children[i];
        if (!isElement(before, 'pre') || sideOf(fenceOf(before)?.meta ?? EMPTY) !== 'before') continue;
        let j = i + 1;
        while (j < children.length && isBlank(children[j])) j++;
        const after = children[j];
        if (!isElement(after, 'pre') || sideOf(fenceOf(after)?.meta ?? EMPTY) !== 'after') continue;
        children.splice(i, j - i + 1, {
          type: 'element',
          tagName: 'div',
          properties: { dataCompare: '' },
          children: [before, after],
        });
      }
    });
}

const EMPTY = { flags: [], marks: new Set<number>() };

/** The callout types the packer accepts (spec 004 §3.5); mirrors `Callouts.Types` in Lld.Content.Authoring. */
export const CALLOUT_TYPES = ['remember', 'note', 'tip', 'interview', 'smell', 'pitfall', 'app', 'work', 'overuse'] as const;
export type CalloutType = (typeof CALLOUT_TYPES)[number];

const MARKER = /^\[!([A-Za-z-]+)\][ \t]*(?:\r?\n|$)/;

/** `> [!SMELL]` (GitHub's alert syntax) → `<blockquote data-callout="smell">` without the marker. */
export function rehypeCallouts() {
  return (tree: Root) =>
    forEachParent(tree, (parent) => {
      for (const quote of parent.children) {
        if (!isElement(quote, 'blockquote')) continue;
        const paragraph = quote.children.find((child) => isElement(child));
        const first = isElement(paragraph, 'p') ? paragraph.children[0] : undefined;
        if (first?.type !== 'text') continue;
        const match = MARKER.exec(first.value);
        const type = match?.[1].toLowerCase();
        if (!match || !CALLOUT_TYPES.includes(type as CalloutType)) continue;
        first.value = first.value.slice(match[0].length);
        quote.properties.dataCallout = type;
        // A marker on its own line leaves an empty first paragraph.
        if (isElement(paragraph) && paragraph.children.every((child) => child.type === 'text' && child.value.trim() === '')) {
          quote.children.splice(quote.children.indexOf(paragraph), 1);
        }
      }
    });
}

export const GO_DEEPER_ANCHOR = 'go-deeper';

/**
 * Lessons (spec 004 §3.2): everything under `## Go deeper` (up to the next `##`) folds into
 * `<div data-go-deeper="topic\ntopic">`, listing its `###` topics so the closed fold says what is inside.
 */
export function rehypeGoDeeper() {
  return (tree: Root) => {
    const start = tree.children.findIndex((node) => isElement(node, 'h2') && node.properties.id === GO_DEEPER_ANCHOR);
    if (start < 0) return;
    let end = start + 1;
    while (end < tree.children.length && !isElement(tree.children[end], 'h2')) end++;
    const content = tree.children.slice(start + 1, end) as ElementContent[];
    if (content.every((node) => node.type === 'text' && node.value.trim() === '')) return;

    const topics = content.filter((node): node is Element => isElement(node, 'h3')).map(plainText);
    tree.children.splice(start + 1, end - start - 1, {
      type: 'element',
      tagName: 'div',
      properties: { dataGoDeeper: topics.join('\n') },
      children: content,
    });
  };
}

function plainText(node: ElementContent): string {
  if (node.type === 'text') return node.value;
  return node.type === 'element' ? node.children.map(plainText).join('') : '';
}

export const INTERVIEW_ANCHOR = 'interview-questions';

/**
 * Lessons (spec 004 §3.5): under `## Interview questions`, each `###` question and the blocks up to the next question
 * become `<section data-qa="index">` with the answer in `<div data-qa-answer>`; all of them sit in one
 * `<div data-qa-list="count">`. Text before the first question stays as it is. Runs after rehype-slug.
 */
export function rehypeInterview() {
  return (tree: Root) => {
    const start = tree.children.findIndex(
      (node) => isElement(node, 'h2') && node.properties.id === INTERVIEW_ANCHOR,
    );
    if (start < 0) return;
    let end = start + 1;
    while (end < tree.children.length && !isElement(tree.children[end], 'h2')) end++;

    const intro: RootContent[] = [];
    const items: Element[] = [];
    for (const node of tree.children.slice(start + 1, end)) {
      if (isElement(node, 'h3')) {
        node.properties.dataQaQuestion = String(items.length);
        items.push({
          type: 'element',
          tagName: 'section',
          properties: { dataQa: String(items.length) },
          children: [node, { type: 'element', tagName: 'div', properties: { dataQaAnswer: '' }, children: [] }],
        });
      } else if (items.length > 0) {
        (items[items.length - 1].children[1] as Element).children.push(node as ElementContent);
      } else {
        intro.push(node);
      }
    }
    if (items.length === 0) return;

    const list: Element = {
      type: 'element',
      tagName: 'div',
      properties: { dataQaList: String(items.length) },
      children: items,
    };
    tree.children.splice(start + 1, end - start - 1, ...intro, list);
  };
}
