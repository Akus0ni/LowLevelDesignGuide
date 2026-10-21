import type { Page } from '@playwright/test';
import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { CONTENT_SNAPSHOT } from './global-setup';

/**
 * Serves /api/content/* from the snapshot (same paths as Lld.Content.Cli's ApiSnapshotWriter) and answers the auth
 * refresh as "signed out", so pages render exactly as against the dev API.
 */
export async function routeContent(page: Page): Promise<void> {
  await page.route('**/api/auth/refresh', (route) =>
    route.fulfill({ status: 401, json: { title: 'Signed out', code: 'auth.session_invalid' } }),
  );
  await page.route('**/api/info', (route) =>
    route.fulfill({ json: { name: 'LLD Guide API', version: '0.1.0', environment: 'E2E' } }),
  );
  await page.route('**/api/content/**', (route) => {
    const file = join(CONTENT_SNAPSHOT, fileFor(route.request().url()));
    return existsSync(file)
      ? route.fulfill({ body: readFileSync(file), contentType: 'application/json' })
      : route.fulfill({ status: 404, json: { title: 'Not found.', code: 'content.not_found' } });
  });
}

/**
 * Serves /api/search from the snapshot: only the requests in fixtures/search-requests.json have an answer (the key is
 * Lld.Content.Cli's SearchSnapshotWriter.KeyOf). Anything else is a 404, so a test using an unlisted search fails clearly.
 */
const KEY_SEPARATOR = String.fromCharCode(1);

export async function routeSearch(page: Page): Promise<void> {
  await page.route('**/api/search?**', (route) => {
    const params = new URL(route.request().url()).searchParams;
    const key = Buffer.from(
      [
        params.get('q') ?? '',
        params.get('kind') ?? '',
        params.get('module') ?? '',
        params.get('lessonId') ?? '',
      ].join(KEY_SEPARATOR),
      'utf8',
    ).toString('base64url');
    const file = join(CONTENT_SNAPSHOT, 'search', `${key}.json`);
    return existsSync(file)
      ? route.fulfill({ body: readFileSync(file), contentType: 'application/json' })
      : route.fulfill({
          status: 404,
          json: { title: `No snapshot for this search: ${params}`, code: 'e2e.search_missing' },
        });
  });
}

function fileFor(url: string): string {
  const { pathname, searchParams } = new URL(url);
  const path = decodeURIComponent(pathname.replace(/^\/api\/content\//, ''));
  const module = searchParams.get('module');
  return path === 'cheatsheets' && module ? `cheatsheets-by-module/${module}.json` : `${path}.json`;
}

/** Every module in the snapshot, in reading order. */
export function allModules(): { slug: string; title: string; lessonCount: number }[] {
  return JSON.parse(readFileSync(join(CONTENT_SNAPSHOT, 'modules.json'), 'utf8'));
}

/** The lesson slugs of one module, in reading order. */
export function lessonSlugsOf(moduleSlug: string): string[] {
  const module = JSON.parse(readFileSync(join(CONTENT_SNAPSHOT, 'modules', `${moduleSlug}.json`), 'utf8'));
  return module.lessons.map((lesson: { slug: string }) => lesson.slug);
}

/** Every cheat sheet in the snapshot, for checks that must cover all of them. */
export function allCheatSheets(): { moduleSlug: string; lessonSlug: string; title: string }[] {
  return JSON.parse(readFileSync(join(CONTENT_SNAPSHOT, 'cheatsheets.json'), 'utf8'));
}
