import type { BookmarkKind } from '../../data/types';
import { cn } from '../../ui/cn';
import { Icon } from '../../ui/Icon';
import { ribbonIcon } from './noteIcons';
import { undoToast } from './undoToast';
import { useBookmark } from './useNotes';

const LABEL: Record<BookmarkKind, string> = { lesson: 'Bookmark lesson', section: 'Bookmark section', cheatSheet: 'Bookmark cheat sheet' };

/**
 * A lesson's or cheat sheet's bookmark (spec 007 §3.3). The ribbon fills at once: that is the feedback; the toast only
 * offers Undo.
 */
export function BookmarkToggle({ kind, lessonId }: { kind: Exclude<BookmarkKind, 'section'>; lessonId: string }) {
  const [on, set] = useBookmark(kind, lessonId);
  const toggle = () => {
    set(!on);
    undoToast(on ? 'Bookmark removed' : 'Bookmarked', () => set(on));
  };
  return (
    <button
      type="button"
      aria-pressed={on}
      aria-label={LABEL[kind]}
      onClick={toggle}
      className={cn(
        'bookmark-toggle inline-flex size-10 items-center justify-center rounded-full border border-rule bg-surface select-none',
        'transition-transform duration-[120ms] ease-out active:scale-[0.97]',
        'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent',
      )}
    >
      <Icon d={ribbonIcon} size={18} />
    </button>
  );
}
