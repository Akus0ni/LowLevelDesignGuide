import { useEffect, useRef } from 'react';
import { useLocation, useSearchParams } from 'react-router';

const REDUCED_MOTION = '(prefers-reduced-motion: reduce)';

/**
 * `?at=anchor` scrolls to that heading (the app uses a hash router, so `#anchor` can't carry it). Opening a page at an
 * anchor jumps there; following an in-page link glides, unless the reader prefers reduced motion. Tapping the same
 * heading again works too: every navigation has a new location key.
 */
export function useAnchorScroll(): void {
  const [params] = useSearchParams();
  const { key } = useLocation();
  const anchor = params.get('at');
  const firstVisit = useRef(true);

  useEffect(() => {
    const target = anchor ? document.getElementById(anchor) : null;
    if (target) {
      const glide = !firstVisit.current && !window.matchMedia(REDUCED_MOTION).matches;
      target.scrollIntoView({ behavior: glide ? 'smooth' : 'auto', block: 'start' });
    }
    firstVisit.current = false;
  }, [anchor, key]);
}
