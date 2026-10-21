import type { SearchHit, SearchKind } from '../../data/types';

export const KIND_ICON: Record<SearchKind, string[]> = {
  lesson: ['M4 5a2 2 0 0 1 2-2h13v16H6a2 2 0 0 0-2 2V5Z', 'M4 19a2 2 0 0 1 2-2h13'],
  cheatSheet: ['M13 2 4 14h7l-1 8 9-12h-7l1-8Z'],
  note: ['M4 20h4L19 9l-4-4L4 16v4Z', 'm13 7 4 4'],
};

export const KIND_LABEL: Record<SearchKind, string> = {
  lesson: 'Lesson',
  cheatSheet: 'Cheat sheet',
  note: 'Note',
};

/** "Lesson › Section", the sheet reference of a drawing set; a note on a removed lesson says so. */
export function hitReference(hit: SearchHit): string {
  const lesson = hit.lessonTitle || 'Removed lesson';
  return hit.heading ? `${lesson} › ${hit.heading}` : lesson;
}
