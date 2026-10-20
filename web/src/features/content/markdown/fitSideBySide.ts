import { useLayoutEffect, useState, type RefObject } from 'react';

export function longestLine(code: string): number {
  return Math.max(0, ...code.split('\n').map((line) => line.length));
}

/** Pane chrome around the code: padding (2 × 1rem), the −/+ gutter (2ch) and the divider. */
const PANE_EXTRA_CH = 2;
const PANE_PADDING_PX = 32;

/**
 * True when two panes, each as wide as the longest line, fit side by side. Measured, not guessed: the width of one
 * character comes from the code font itself, and the check repeats when the box is resized.
 */
export function useFitsSideBySide(figure: RefObject<HTMLElement | null>, longest: number): boolean {
  const [fits, setFits] = useState(false);

  useLayoutEffect(() => {
    const element = figure.current;
    if (!element || typeof ResizeObserver === 'undefined') return;
    const charWidth = measureCharWidth(element);
    const needed = 2 * ((longest + PANE_EXTRA_CH) * charWidth + PANE_PADDING_PX) + 1;
    const check = () => setFits(element.clientWidth >= needed);
    check();
    const observer = new ResizeObserver(check);
    observer.observe(element);
    return () => observer.disconnect();
  }, [figure, longest]);

  return fits;
}

function measureCharWidth(host: HTMLElement): number {
  const probe = document.createElement('pre');
  probe.className = 'compare-probe';
  probe.textContent = '0'.repeat(20);
  host.append(probe);
  const width = probe.getBoundingClientRect().width / 20;
  probe.remove();
  return width || 8;
}
