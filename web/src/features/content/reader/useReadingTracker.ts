import { useEffect, useRef, useState } from 'react';
import { useNavigationType, useSearchParams } from 'react-router';
import type { OutlineEntry } from '../../../data/types';
import {
  loadPosition,
  loadReadSections,
  saveReadSections,
  savePosition,
  type ReadingPosition,
} from './readerStore';

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
 * Follows reading on a lesson page (spec 003 T14 / T14b):
 * - `current`: the section being read (the outline highlights it);
 * - `read`: sections already read, remembered on this device (the outline ticks them);
 * - reading position: saved while scrolling and restored when the lesson is opened again, unless the page was opened
 *   at a heading (`?at=`) or the browser is restoring its own scroll (Back).
 */
export function useReadingTracker(lessonId: string, outline: OutlineEntry[]) {
  const [current, setCurrent] = useState<string | null>(null);
  const [read, setRead] = useState<Set<string>>(() => loadReadSections(lessonId));
  const [params] = useSearchParams();
  const navigation = useNavigationType();
  const openedAtAnchor = params.has('at');

  // Restore where the reader left off.
  const restoring = useRef(false);
  useEffect(() => {
    const saved = loadPosition(lessonId);
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

  // Track the current section, finished sections and the position while scrolling.
  useEffect(() => {
    setRead(loadReadSections(lessonId));
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
        setRead((before) => {
          const after = new Set(before);
          sections.forEach((section, index) => {
            const end = sections[index + 1]?.top ?? Number.POSITIVE_INFINITY;
            if (end <= readLine || (atBottom && section.top <= readLine)) after.add(section.anchor);
          });
          if (after.size === before.size) return before;
          saveReadSections(lessonId, after);
          return after;
        });
      }

      if (!restoring.current) {
        window.clearTimeout(saveTimer);
        saveTimer = window.setTimeout(() => savePosition(lessonId, positionNow(sections)), SAVE_DELAY_MS);
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
  }, [lessonId, outline]);

  return { current, read };
}
