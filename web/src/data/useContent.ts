import { use } from 'react';
import { useDataSource } from './DataSourceContext';
import type {
  CheatSheetDetail,
  CheatSheetSummary,
  DataSource,
  LessonDetail,
  ModuleDetail,
  ModuleSummary,
} from './types';

/**
 * Content can't change while the app runs (it ships with the app; in dev mode a reload picks up edits), so each
 * request is made once per data source and kept. Components read it with React's `use()`: they suspend while it
 * loads, and a failure goes to the nearest error boundary, whose "Try again" calls `forgetContent`.
 */
const caches = new WeakMap<DataSource, Map<string, Promise<unknown>>>();

function cacheOf(source: DataSource): Map<string, Promise<unknown>> {
  let cache = caches.get(source);
  if (!cache) {
    cache = new Map();
    caches.set(source, cache);
  }
  return cache;
}

function load<T>(source: DataSource, key: string, fetch: (source: DataSource) => Promise<T>): Promise<T> {
  const cache = cacheOf(source);
  let promise = cache.get(key) as Promise<T> | undefined;
  if (!promise) {
    promise = fetch(source);
    cache.set(key, promise);
  }
  return promise;
}

/** Reads one piece of content, loading it the first time. Must be inside `<Suspense>` and an error boundary. */
export function useContent<T>(key: string, fetch: (source: DataSource) => Promise<T>): T {
  return use(load(useDataSource(), key, fetch));
}

/** Starts loading without waiting, e.g. the next lesson while this one is read. Failures are left for `useContent`. */
export function preloadContent<T>(
  source: DataSource,
  key: string,
  fetch: (source: DataSource) => Promise<T>,
): void {
  load(source, key, fetch).catch(() => {});
}

/** Forgets one entry (or all), so the next render asks again: what "Try again" does after a failure. */
export function forgetContent(source: DataSource, key?: string): void {
  if (key === undefined) caches.delete(source);
  else cacheOf(source).delete(key);
}

/** Cache keys and loaders for each kind of content, so screens never build keys by hand. */
export const content = {
  modules: { key: 'modules', fetch: (s: DataSource) => s.getModules() },
  module: (slug: string) => ({ key: `module:${slug}`, fetch: (s: DataSource) => s.getModule(slug) }),
  lesson: (id: string) => ({ key: `lesson:${id}`, fetch: (s: DataSource) => s.getLesson(id) }),
  cheatSheets: (moduleSlug?: string) => ({
    key: `cheatsheets:${moduleSlug ?? '*'}`,
    fetch: (s: DataSource) => s.getCheatSheets(moduleSlug),
  }),
  cheatSheet: (lessonId: string) => ({
    key: `cheatsheet:${lessonId}`,
    fetch: (s: DataSource) => s.getCheatSheet(lessonId),
  }),
};

export const useModules = (): ModuleSummary[] => useContent(content.modules.key, content.modules.fetch);

export function useModule(slug: string): ModuleDetail {
  const { key, fetch } = content.module(slug);
  return useContent(key, fetch);
}

export function useLesson(id: string): LessonDetail {
  const { key, fetch } = content.lesson(id);
  return useContent(key, fetch);
}

export function useCheatSheets(moduleSlug?: string): CheatSheetSummary[] {
  const { key, fetch } = content.cheatSheets(moduleSlug);
  return useContent(key, fetch);
}

export function useCheatSheet(lessonId: string): CheatSheetDetail {
  const { key, fetch } = content.cheatSheet(lessonId);
  return useContent(key, fetch);
}
