import { Icon } from '../../ui/Icon';
import { pencilIcon, ribbonIcon } from './noteIcons';

const notesLabel = (count: number) => (count === 1 ? '1 note' : `${count} notes`);

/** The outline's small marks for a section: how many notes, and a ribbon when bookmarked. Labelled for screen readers. */
export function NoteMarks({ notes, bookmarked }: { notes: number; bookmarked: boolean }) {
  if (notes === 0 && !bookmarked) return null;
  return (
    <span className="note-marks">
      {notes > 0 && (
        <span className="note-mark" role="img" aria-label={notesLabel(notes)}>
          <Icon d={pencilIcon} size={12} />
          <span aria-hidden="true">{notes}</span>
        </span>
      )}
      {bookmarked && (
        <span className="bookmark-mark" role="img" aria-label="Bookmarked">
          <Icon d={ribbonIcon} size={12} />
        </span>
      )}
    </span>
  );
}

/** The note chip after a section heading: tapping it opens the notes sheet at that section. */
export function NoteChip({ count, onOpen }: { count: number; onOpen: () => void }) {
  return (
    <button type="button" className="note-chip" onClick={onOpen} aria-label={`${notesLabel(count)} on this section`}>
      <Icon d={pencilIcon} size={14} />
      <span aria-hidden="true">{count}</span>
    </button>
  );
}
