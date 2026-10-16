import { createContext } from 'react';
import type { LessonTerm } from '../../../data/types';

/** The lesson's code-callout terms; empty outside a lesson (cheat sheets, failed-diagram sources). */
export const TermsContext = createContext<LessonTerm[]>([]);

export interface TextPart {
  text: string;
  term?: LessonTerm;
}

/** Splits text into plain parts and whole-word term matches (`AccessRequestStates` doesn't match `AccessRequest`). */
export function splitByTerms(text: string, terms: LessonTerm[]): TextPart[] {
  if (terms.length === 0 || text.length === 0) return [{ text }];
  const byName = new Map(terms.map((term) => [term.term, term]));
  const names = [...byName.keys()].sort((a, b) => b.length - a.length).map(escape);
  const pattern = new RegExp(`(?<![\\w.])(${names.join('|')})(?!\\w)`, 'g');

  const parts: TextPart[] = [];
  let last = 0;
  for (const match of text.matchAll(pattern)) {
    if (match.index > last) parts.push({ text: text.slice(last, match.index) });
    parts.push({ text: match[0], term: byName.get(match[0]) });
    last = match.index + match[0].length;
  }
  if (last < text.length) parts.push({ text: text.slice(last) });
  return parts;
}

function escape(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
