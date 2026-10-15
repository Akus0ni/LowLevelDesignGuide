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
