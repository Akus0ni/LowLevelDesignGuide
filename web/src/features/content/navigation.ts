import { useDataSource } from '../../data/DataSourceContext';
import type { DataSource } from '../../data/types';
import { preloadContent } from '../../data/useContent';

export const chevronRight = ['m9 6 6 6-6 6'];
export const chevronLeft = ['m15 6-6 6 6 6'];

/** Content a link leads to; loading starts on press, hover or focus, before the tap even finishes. */
export interface Preload<T> {
  key: string;
  fetch: (source: DataSource) => Promise<T>;
}

export function usePreload<T>(next: Preload<T>) {
  const source = useDataSource();
  const start = () => preloadContent(source, next.key, next.fetch);
  return { onPointerDown: start, onPointerEnter: start, onFocus: start };
}

export function plural(count: number, one: string, many = `${one}s`): string {
  return `${count} ${count === 1 ? one : many}`;
}

const MINUTES_PER_HOUR = 60;

/** Reading time people can take in at a glance: "8 min", "2 h", "15 h 39 min". */
export function formatMinutes(minutes: number): string {
  if (minutes < MINUTES_PER_HOUR) return `${minutes} min`;
  const hours = Math.floor(minutes / MINUTES_PER_HOUR);
  const rest = minutes % MINUTES_PER_HOUR;
  return rest === 0 ? `${hours} h` : `${hours} h ${rest} min`;
}
