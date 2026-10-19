import { useEffect, useMemo, useRef, useState } from 'react';
import { useNavigationType, useSearchParams } from 'react-router';
import type { OutlineEntry, ReadingPosition } from '../../../data/types';
import { opened, positionSaved, sectionsRead } from '../../progress/lessonProgress';
import { useLessonProgress, useProgressStore } from '../../progress/useProgress';

/** The "you are reading here" line: a third of the way down the screen. */
const READING_LINE = 0.3;
/** A section counts as read once its end has passed the middle of the screen. */
const READ_LINE = 0.5;
/** Diagrams render after the text; restore once more when they're likely done, unless the reader moved. */
const LATE_RESTORE_MS = 700;
const SAVE_DELAY_MS = 250;

interface Section {
  anchor: string;
  top: number;
}

const last = <T>(items: T[]): T | undefined => items[items.length - 1];

function sectionsOf(outline: OutlineEntry[]): Section[] {
  return outline
    .map((entry) => document.getElementById(entry.anchor))
    .filter((element): element is HTMLElement => element !== null)
    .map((element) => ({ anchor: element.id, top: element.getBoundingClientRect().top + window.scrollY }));
}

function positionNow(sections: Section[]): ReadingPosition {
  const passed = sections.filter((section) => section.top <= window.scrollY + 1);
  const above = last(passed);
  return above
    ? { anchor: above.anchor, offset: Math.round(window.scrollY - above.top) }
    : { anchor: '', offset: window.scrollY };
}

function scrollToPosition(position: ReadingPosition): void {
  const heading = position.anchor ? document.getElementById(position.anchor) : null;
  const base = heading ? heading.getBoundingClientRect().top + window.scrollY : 0;
  window.scrollTo({ top: Math.max(0, base + position.offset), behavior: 'instant' });
}

/**
 * Follows reading on a lesson page (spec 003 T14 / T14b, now through the progress store of spec 005):
 * - opening the lesson starts it (In progress) and makes it the one to continue;
 * - `current`: the section being read (the outline highlights it);
 * - `read`: sections already read by this learner (the outline ticks them); the last one marks the lesson done;
 * - reading position: saved while scrolling and restored when the lesson is opened again, unless the page was opened
 *   at a heading (`?at=`) or the browser is restoring its own scroll (Back).
 */
export function useReadingTracker(lessonId: string, outline: OutlineEntry[]) {
  const store = useProgressStore();
  const readSections = useLessonProgress(lessonId)?.readSections;
  const read = useMemo(() => new Set(readSections ?? []), [readSections]);
  const [current, setCurrent] = useState<string | null>(null);
  const [params] = useSearchParams();
  const navigation = useNavigationType();
  const openedAtAnchor = params.has('at');

  // Opening the lesson starts it.
  useEffect(() => store.change(lessonId, opened), [store, lessonId]);

  // Restore where the reader left off.
  const restoring = useRef(false);
  useEffect(() => {
    const saved = store.getSnapshot().byId.get(lessonId)?.position;
    if (!saved || openedAtAnchor || (navigation === 'POP' && window.scrollY > 0)) return;
    restoring.current = true;
    scrollToPosition(saved);
    let moved = false;
    const stop = () => (moved = true);
    const events = ['wheel', 'touchstart', 'keydown'] as const;
    events.forEach((name) => window.addEventListener(name, stop, { passive: true, once: true }));
    const late = window.setTimeout(() => {
      if (!moved) scrollToPosition(saved);
      restoring.current = false;
    }, LATE_RESTORE_MS);
    return () => {
      window.clearTimeout(late);
      events.forEach((name) => window.removeEventListener(name, stop));
    };
    // Only when a lesson opens; later `?at=` jumps are the anchor hook's job.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [lessonId]);

  // What the scroll handler compares against; updated when the store changes, without restarting the listener.
  const known = useRef(read);
  useEffect(() => {
    known.current = read;
  }, [read]);

  // Track the current section, finished sections and the position while scrolling.
  useEffect(() => {
    const anchors = outline.map((entry) => entry.anchor);
    let frame = 0;
    let saveTimer = 0;
    let scrolled = false;

    const update = () => {
      frame = 0;
      const sections = sectionsOf(outline);
      if (sections.length === 0) return;
      const readingLine = window.scrollY + window.innerHeight * READING_LINE;
      setCurrent(last(sections.filter((section) => section.top <= readingLine))?.anchor ?? null);

      if (scrolled) {
        const readLine = window.scrollY + window.innerHeight * READ_LINE;
        const atBottom = window.innerHeight + window.scrollY >= document.documentElement.scrollHeight - 2;
        const newlyRead = sections
          .filter((section, index) => {
            const end = sections[index + 1]?.top ?? Number.POSITIVE_INFINITY;
            return end <= readLine || (atBottom && section.top <= readLine);
          })
          .map((section) => section.anchor)
          .filter((anchor) => !known.current.has(anchor));
        if (newlyRead.length > 0) {
          known.current = new Set([...known.current, ...newlyRead]);
          store.change(lessonId, (record, id, now) => sectionsRead(record, id, newlyRead, anchors, now));
          store.recordActivity();
        }
      }

      if (!restoring.current) {
        window.clearTimeout(saveTimer);
        saveTimer = window.setTimeout(() => {
          const position = positionNow(sections);
          store.change(lessonId, (record, id, now) => positionSaved(record, id, position, now));
        }, SAVE_DELAY_MS);
      }
    };

    const onScroll = () => {
      scrolled = true;
      if (!frame) frame = window.requestAnimationFrame(update);
    };
    update();
    window.addEventListener('scroll', onScroll, { passive: true });
    return () => {
      window.removeEventListener('scroll', onScroll);
      window.cancelAnimationFrame(frame);
      window.clearTimeout(saveTimer);
    };
  }, [store, lessonId, outline]);

  return { current, read };
}
