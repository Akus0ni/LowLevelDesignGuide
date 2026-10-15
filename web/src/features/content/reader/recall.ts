import type { Element, ElementContent, Root } from 'hast';
import { createContext } from 'react';

/**
 * Recall mode on cheat sheets (spec 003 §3.8): each `##` section folds under its heading; the reader thinks of the
 * answer, then reveals it. No scoring.
 */
export interface RecallState {
  on: boolean;
  isRevealed: (id: string) => boolean;
  reveal: (id: string) => void;
}

export const RecallContext = createContext<RecallState>({
  on: false,
  isRevealed: () => true,
  reveal: () => {},
});

/**
 * Wraps each h2 and everything up to the next h2 in `<section data-recall="id">` with the body in
 * `<div data-recall-body>`, so a section can fold. Content before the first h2 stays as it is.
 */
export function rehypeRecallSections() {
  return (tree: Root) => {
    const children: Root['children'] = [];
    let section: Element | null = null;
    for (const node of tree.children) {
      if (node.type === 'element' && node.tagName === 'h2') {
        const id = String(node.properties.id ?? '');
        const body: Element = {
          type: 'element',
          tagName: 'div',
          properties: { dataRecallBody: '' },
          children: [],
        };
        section = {
          type: 'element',
          tagName: 'section',
          properties: { dataRecall: id },
          children: [node, body],
        };
        children.push(section);
      } else if (section) {
        (section.children[1] as Element).children.push(node as ElementContent);
      } else {
        children.push(node);
      }
    }
    tree.children = children;
  };
}
