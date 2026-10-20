import type { Note } from '../../data/types';
import { groupForExport, readableAnchor, type NotesIndex } from './notesQuery';

const RULE = '\n\n---\n\n';

/** `lld-notes-2026-10-20.md`, from the learner's local date. */
export function exportFileName(today: Date): string {
  const pad = (value: number) => String(value).padStart(2, '0');
  return `lld-notes-${today.getFullYear()}-${pad(today.getMonth() + 1)}-${pad(today.getDate())}.md`;
}

/**
 * All live notes as one Markdown file (spec 007 §3.7): `# My LLD notes` → `## module` → `### lesson` →
 * `#### section | Whole lesson` → the notes as written, oldest first, between rules. Removed lessons come last.
 */
export function buildExport(notes: readonly Note[], index: NotesIndex): string {
  const parts = ['# My LLD notes'];
  groupForExport(notes, index).forEach(({ module, lessons }) => {
    parts.push(`## ${module?.title ?? 'Removed lessons'}`);
    lessons.forEach(({ lessonId, lesson, sections }) => {
      parts.push(`### ${lesson?.title ?? lessonId}`);
      sections.forEach(({ anchor, title, notes: sectionNotes }) => {
        parts.push(`#### ${title ?? readableAnchor(anchor)}`);
        parts.push(sectionNotes.map((note) => note.body.trim()).join(RULE));
      });
    });
  });
  return `${parts.join('\n\n')}\n`;
}
