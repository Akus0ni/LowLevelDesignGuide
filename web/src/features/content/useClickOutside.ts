import { useEffect, useRef, type RefObject } from 'react';

/** Calls `onOutside` when the user presses anywhere outside `element` (only while `active`). */
export function useClickOutside(element: RefObject<HTMLElement | null>, active: boolean, onOutside: () => void) {
  const latest = useRef(onOutside);
  latest.current = onOutside;

  useEffect(() => {
    if (!active) return;
    const onPointerDown = (event: PointerEvent) => {
      if (element.current && !element.current.contains(event.target as Node)) latest.current();
    };
    document.addEventListener('pointerdown', onPointerDown);
    return () => document.removeEventListener('pointerdown', onPointerDown);
  }, [element, active]);
}
