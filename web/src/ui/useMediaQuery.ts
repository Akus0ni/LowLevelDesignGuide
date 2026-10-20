import { useSyncExternalStore } from 'react';

/** Whether a media query matches now, kept up to date (capability and size queries, never user-agent sniffing). */
export function useMediaQuery(query: string): boolean {
  return useSyncExternalStore(
    (onChange) => {
      const list = window.matchMedia(query);
      list.addEventListener('change', onChange);
      return () => list.removeEventListener('change', onChange);
    },
    () => window.matchMedia(query).matches,
    () => false,
  );
}

/** Wide enough for the notes panel beside the text (the reader's own desktop breakpoint, `lg`). */
export const DESKTOP = '(min-width: 64rem)';
