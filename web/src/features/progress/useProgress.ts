import { createContext, use, useContext, useSyncExternalStore } from 'react';
import type { LessonProgress } from '../../data/types';
import type { ProgressState, ProgressStore } from './progressStore';

export const ProgressContext = createContext<ProgressStore | null>(null);

export function useProgressStore(): ProgressStore {
  const store = useContext(ProgressContext);
  if (!store) throw new Error('useProgressStore must be used inside <ProgressProvider>');
  return store;
}

/**
 * The owner's progress. Suspends until it has loaded (a local read, so about as long as the content itself), which
 * keeps counts and marks from flashing from "nothing" to the real values.
 */
export function useProgress(): ProgressState {
  const store = useProgressStore();
  use(store.ready);
  return useSyncExternalStore(store.subscribe, store.getSnapshot);
}

export function useLessonProgress(lessonId: string): LessonProgress | undefined {
  return useProgress().byId.get(lessonId);
}
