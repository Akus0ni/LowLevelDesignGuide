import { useEffect } from 'react';
import { Icon } from '../../../ui/Icon';
import type { SearchMarks } from './useSearchMarks';

const upIcon = ['m6 15 6-6 6 6'];
const downIcon = ['m6 9 6 6 6-6'];
const closeIcon = ['M6 6l12 12', 'M18 6 6 18'];

/** An open sheet, dialog or popover handles Escape itself; the bar must not clear marks underneath it. */
const OVERLAY_OPEN = '[role="dialog"][data-open]';

/**
 * "decorator · 3 of 7 · ↑ ↓ · ✕" (spec §3.6): steps through the search marks and clears them. Escape and Android Back
 * (see hardwareBack) clear; on a desktop keyboard Enter / Shift+Enter step when nothing else has focus.
 */
export function MatchBar({ marks }: { marks: SearchMarks }) {
  const { query, count, current, next, previous, clear } = marks;

  useEffect(() => {
    if (!query) return;
    const onKey = (event: KeyboardEvent) => {
      if (event.defaultPrevented || document.querySelector(OVERLAY_OPEN)) return;
      if (event.key === 'Escape') {
        event.preventDefault();
        clear();
      } else if (
        event.key === 'Enter' &&
        (document.activeElement === document.body || document.activeElement === null)
      ) {
        event.preventDefault();
        if (event.shiftKey) previous();
        else next();
      }
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [query, clear, next, previous]);

  if (!query) return null;
  return (
    <div className="match-bar" data-match-bar="" role="group" aria-label="Search matches">
      <span className="match-bar-query">“{query.trim()}”</span>
      <span className="match-bar-count" aria-live="polite">
        {current < 0 ? (
          <>
            <span className="tabular-nums">{count}</span> {count === 1 ? 'match' : 'matches'}
          </>
        ) : (
          <>
            <span className="tabular-nums">{current + 1}</span> of{' '}
            <span className="tabular-nums">{count}</span>
          </>
        )}
      </span>
      <button type="button" className="match-bar-button" onClick={previous} aria-label="Previous match">
        <Icon d={upIcon} size={18} />
      </button>
      <button type="button" className="match-bar-button" onClick={next} aria-label="Next match">
        <Icon d={downIcon} size={18} />
      </button>
      <button type="button" className="match-bar-button" onClick={clear} aria-label="Clear search marks">
        <Icon d={closeIcon} size={18} />
      </button>
    </div>
  );
}
