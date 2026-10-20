import type { ComponentProps } from 'react';
import { Icon } from '../../ui/Icon';
import { NoteChip } from './NoteMarks';
import { ribbonIcon } from './noteIcons';
import { SectionActions } from './SectionActions';
import { useSectionAnnotations } from './sectionAnnotations';
import { useSectionMarks } from './useNotes';

/**
 * A lesson's `h2` with the learner's marks after the title: the note chip, a ribbon when bookmarked, and the section
 * actions button (spec 007 §3.2). Without the lesson page's context (cheat sheets, a note's preview) it is a plain h2.
 */
export function SectionHeading(props: ComponentProps<'h2'>) {
  const annotations = useSectionAnnotations();
  if (!annotations || !props.id) return <h2 {...props} />;
  return <AnnotatedHeading {...props} id={props.id} lessonId={annotations.lessonId} />;
}

function AnnotatedHeading({ id, lessonId, children, ...props }: ComponentProps<'h2'> & { id: string; lessonId: string }) {
  const annotations = useSectionAnnotations();
  const marks = useSectionMarks(lessonId);
  const count = marks.notes.get(id) ?? 0;
  const title = annotations?.sectionTitles.get(id) ?? '';
  return (
    <h2 id={id} {...props} className="section-heading">
      <span className="section-heading-text">{children}</span>
      <span className="section-tools">
        {count > 0 && <NoteChip count={count} onOpen={() => annotations?.openNotes({ anchor: id })} />}
        {marks.bookmarked.has(id) && (
          <span className="section-ribbon" role="img" aria-label="Bookmarked">
            <Icon d={ribbonIcon} size={16} />
          </span>
        )}
        <SectionActions anchor={id} title={title} />
      </span>
    </h2>
  );
}
