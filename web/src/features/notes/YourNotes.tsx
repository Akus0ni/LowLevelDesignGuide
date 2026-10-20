import { Collapsible } from '@base-ui/react/collapsible';
import { Suspense } from 'react';
import { Link } from 'react-router';
import type { Note } from '../../data/types';
import { useLesson } from '../../data/useContent';
import { NoteMarkdown } from '../content/markdown/MarkdownView';
import { Icon } from '../../ui/Icon';
import { readableAnchor, sectionsOf } from './notesQuery';
import { useLessonNotes } from './useNotes';

const chevron = ['m6 9 6 6 6-6'];

/**
 * Under a cheat sheet in Revise (spec 007 §3.6): the lesson's notes, folded, in reading order, so the learner's own
 * words come back before an interview. Not part of recall: Reveal all doesn't open it, and it doesn't count as revised.
 */
export function YourNotes({ lessonId, lessonPath }: { lessonId: string; lessonPath: string }) {
  const notes = useLessonNotes(lessonId);
  if (notes.length === 0) return null;
  return (
    <Collapsible.Root className="your-notes">
      <Collapsible.Trigger className="your-notes-trigger">
        <span className="note-stereotype">«my notes»</span>
        <span className="flex-1 text-left font-semibold">Your notes ({notes.length})</span>
        <span className="your-notes-chevron">
          <Icon d={chevron} size={18} />
        </span>
      </Collapsible.Trigger>
      <Collapsible.Panel className="recall-panel">
        <Suspense fallback={null}>
          <NotesInReadingOrder lessonId={lessonId} lessonPath={lessonPath} notes={notes} />
        </Suspense>
      </Collapsible.Panel>
    </Collapsible.Root>
  );
}

/** The lesson's outline gives the order and the section titles (a local read, cached for the reader too). */
function NotesInReadingOrder({ lessonId, lessonPath, notes }: { lessonId: string; lessonPath: string; notes: Note[] }) {
  const lesson = useLesson(lessonId);
  const sections = lesson.outline
    .filter((entry) => entry.level === 2)
    .map((entry) => ({ anchor: entry.anchor, title: entry.text }));
  const info = { id: lessonId, moduleSlug: lesson.moduleSlug, slug: lesson.slug, title: lesson.title, order: 0, sections };
  return (
    <ul className="mt-3 flex flex-col gap-3">
      {sectionsOf(notes, info).flatMap((group) =>
        group.notes.map((note) => (
          <li key={note.id} className="your-note">
            <p className="note-group-title">{group.title ?? (group.anchor ? readableAnchor(group.anchor) : 'Whole lesson')}</p>
            <NoteMarkdown markdown={note.body} />
            <Link
              to={`${lessonPath}?${new URLSearchParams({ ...(note.anchor && { at: note.anchor }), note: note.id })}`}
              className="your-note-edit"
            >
              Edit in the lesson
            </Link>
          </li>
        )),
      )}
    </ul>
  );
}
