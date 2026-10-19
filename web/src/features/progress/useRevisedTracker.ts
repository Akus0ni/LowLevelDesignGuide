import { useCallback, useEffect, useRef, useState } from 'react';
import { revised } from './lessonProgress';
import { useProgressStore } from './useProgress';

/**
 * A short cheat sheet's end is on screen the moment it opens; staying there this long means it was read, not just
 * opened by mistake.
 */
export const END_DWELL_MS = 2000;

/**
 * Records "revised" for a cheat sheet (spec 005 §3.5), once per visit: when its end has been on screen for a moment
 * with recall off, or when recall mode has every block revealed. Returns the ref for an element at the very end.
 */
export function useRevisedTracker(lessonId: string, { recall, allRevealed }: { recall: boolean; allRevealed: boolean }) {
  const store = useProgressStore();
  const recorded = useRef<string | null>(null);
  const [end, setEnd] = useState<HTMLElement | null>(null);

  const record = useCallback(() => {
    if (recorded.current === lessonId) return;
    recorded.current = lessonId;
    store.change(lessonId, revised);
    store.recordActivity();
  }, [store, lessonId]);

  useEffect(() => {
    if (recall && allRevealed) record();
  }, [recall, allRevealed, record]);

  useEffect(() => {
    if (recall || !end || typeof IntersectionObserver === 'undefined') return;
    let timer: ReturnType<typeof setTimeout> | undefined;
    const observer = new IntersectionObserver((entries) => {
      clearTimeout(timer);
      if (entries.some((entry) => entry.isIntersecting)) timer = setTimeout(record, END_DWELL_MS);
    });
    observer.observe(end);
    return () => {
      clearTimeout(timer);
      observer.disconnect();
    };
  }, [recall, end, record]);

  return setEnd;
}
