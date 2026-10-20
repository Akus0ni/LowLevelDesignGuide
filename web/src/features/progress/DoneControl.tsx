import { useCallback, useEffect, useRef } from 'react';
import { cn } from '../../ui/cn';
import { markedDone, markedNotDone } from './lessonProgress';
import { useProgress, useProgressStore } from './useProgress';

/**
 * The end of a lesson (spec 005 §3.8): "Mark as done", or the inked "✓ Done" with a quiet way back. No confirmation:
 * the toggle is its own undo. The check draws in once, the first time the lesson is done, whether the learner tapped
 * or the last section was read; it is right where the reader is, so no toast repeats it.
 */
export function DoneControl({ lessonId, sectionCount }: { lessonId: string; sectionCount: number }) {
  const store = useProgressStore();
  const { byId, freshlyDone } = useProgress();
  const record = byId.get(lessonId);
  const done = record?.status === 'done';
  const read = Math.min(record?.readSections.length ?? 0, sectionCount);

  // The pressed button disappears; keep keyboard focus on the control that replaced it.
  const markButton = useRef<HTMLButtonElement>(null);
  const undoButton = useRef<HTMLButtonElement>(null);
  const toggledHere = useRef(false);
  useEffect(() => {
    if (!toggledHere.current) return;
    toggledHere.current = false;
    (done ? undoButton : markButton).current?.focus();
  }, [done]);

  // A native listener: the check's animation bubbles its end up to the badge; then it never draws again.
  const settleWhenDrawn = useCallback(
    (badge: HTMLElement | null) => {
      if (!badge) return;
      const settle = () => store.settleCelebration(lessonId);
      badge.addEventListener('animationend', settle);
      return () => badge.removeEventListener('animationend', settle);
    },
    [store, lessonId],
  );

  const toggle = () => {
    toggledHere.current = true;
    store.change(lessonId, done ? markedNotDone : markedDone);
  };

  return (
    <div className="flex min-h-12 flex-wrap items-center gap-x-4 gap-y-2">
      {done ? (
        <>
          <span
            ref={settleWhenDrawn}
            data-fresh={freshlyDone.has(lessonId) || undefined}
            className="done-badge inline-flex h-12 items-center gap-2 rounded-full border-[1.5px] border-fg px-5 text-[15px] font-semibold"
          >
            <svg viewBox="0 0 24 24" width="18" height="18" aria-hidden="true" className="done-check">
              <path d="m5 12 5 5L20 7" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" />
            </svg>
            Done
          </span>
          <button
            ref={undoButton}
            type="button"
            onClick={toggle}
            className={cn(
              'inline-flex h-11 items-center text-[15px] text-muted underline-offset-4 select-none',
              'hover:text-fg hover:underline focus-visible:outline-2 focus-visible:outline-accent',
            )}
          >
            Mark as not done
          </button>
        </>
      ) : (
        <>
          <button
            ref={markButton}
            type="button"
            onClick={toggle}
            className={cn(
              'inline-flex h-12 w-full items-center justify-center gap-2 rounded-full bg-accent px-6 text-[15px] font-semibold text-on-fill select-none sm:w-auto',
              'transition-[transform,opacity] duration-[120ms] ease-out active:scale-[0.97] hover:opacity-90',
              'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent',
            )}
          >
            Mark as done
          </button>
          {sectionCount > 0 && (
            <span className="font-mono text-[12px] tracking-[0.04em] text-muted tabular-nums">
              {read} of {sectionCount} sections read
            </span>
          )}
        </>
      )}
      <span role="status" className="sr-only">
        {done ? 'Lesson done' : ''}
      </span>
    </div>
  );
}
