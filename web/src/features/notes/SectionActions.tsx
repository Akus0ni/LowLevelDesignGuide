import { Popover } from '@base-ui/react/popover';
import { useState } from 'react';
import { Icon } from '../../ui/Icon';
import { moreIcon, pencilIcon, ribbonIcon } from './noteIcons';
import { useSectionAnnotations } from './sectionAnnotations';
import { undoToast } from './undoToast';
import { useBookmark } from './useNotes';

/**
 * The quiet button at the end of a section heading (spec 007 §3.2): "Add note here" and "Bookmark section". It is
 * there on phones too (44 px target); on desktop it stays faint until hovered or focused.
 */
export function SectionActions({ anchor, title }: { anchor: string; title: string }) {
  const annotations = useSectionAnnotations();
  const [open, setOpen] = useState(false);
  const [bookmarked, setBookmarked] = useBookmark('section', annotations?.lessonId ?? '', anchor);
  if (!annotations) return null;

  const addNote = () => {
    setOpen(false);
    annotations.openNotes({ anchor, create: true });
  };
  const toggleBookmark = () => {
    setOpen(false);
    setBookmarked(!bookmarked);
    undoToast(bookmarked ? 'Bookmark removed' : 'Section bookmarked', () => setBookmarked(bookmarked));
  };

  return (
    <Popover.Root open={open} onOpenChange={setOpen}>
      <Popover.Trigger className="section-actions-trigger" aria-label={`Note or bookmark: ${title}`}>
        <Icon d={moreIcon} size={20} />
      </Popover.Trigger>
      <Popover.Portal>
        <Popover.Positioner side="bottom" align="end" sideOffset={4} className="z-40">
          <Popover.Popup className="section-actions">
            <Popover.Title className="sr-only">{title}</Popover.Title>
            <button type="button" className="section-action" onClick={addNote}>
              <Icon d={pencilIcon} size={18} />
              Add note here
            </button>
            <button type="button" className="section-action" onClick={toggleBookmark} aria-pressed={bookmarked}>
              <Icon d={ribbonIcon} size={18} />
              {bookmarked ? 'Remove bookmark' : 'Bookmark section'}
            </button>
          </Popover.Popup>
        </Popover.Positioner>
      </Popover.Portal>
    </Popover.Root>
  );
}
