import type { Page } from '@playwright/test';

/**
 * break-ui worst case (spec 003 T18): content an author could realistically write, served through the same boundary
 * as real content (/api/content/*). Titles and summaries have no length limit in the front-matter schema, so long but
 * believable values; counts at 0, 1 and large; unbreakable identifiers; non-Latin text; a 40-entry outline.
 */
const LONG_TITLE =
  'Case Study 12: Designing a Multi-Tenant Rate Limiter with Sliding-Window Counters and a Redis Fallback';
const UNBREAKABLE = 'IRefreshTokenRepositoryFactoryProviderConfigurationOptions';

interface Lesson {
  id: string;
  moduleSlug: string;
  slug: string;
  title: string;
  summary: string;
  order: number;
  readingMinutes: number;
  tags: string[];
}

const lesson = (moduleSlug: string, n: number, title: string, summary: string, minutes: number): Lesson => ({
  id: `${moduleSlug}-${n}`,
  moduleSlug,
  slug: `lesson-${n}`,
  title,
  summary,
  order: n,
  readingMinutes: minutes,
  tags: ['strategy', 'observer', 'repository', 'unit-of-work', 'dependency-inversion'],
});

const modules = [
  {
    slug: 'start',
    title: 'Start here',
    summary: 'One.',
    lessons: [lesson('start', 1, 'Jo', 'A.', 1)],
  },
  {
    slug: 'patterns',
    title: 'Gang of Four Design Patterns: Creational, Structural and Behavioural Patterns in Modern C#',
    summary:
      'Every one of the 23 classic patterns with a compiling C# 14 sample, the forces that make it worth using, the ' +
      'simpler alternative you should usually reach for first, and the interview questions each pattern tends to trigger.',
    lessons: Array.from({ length: 40 }, (_, i) =>
      lesson(
        'patterns',
        i + 1,
        i === 0
          ? LONG_TITLE
          : i === 1
            ? UNBREAKABLE
            : i === 2
              ? 'Façade · Fábrica · Строитель · ビルダー'
              : `Pattern ${i + 1}`,
        i === 0
          ? 'A rate limiter that stays fair across tenants, survives a Redis outage and explains itself to the on-call engineer at 3 a.m. without a single dashboard.'
          : `Pattern number ${i + 1}.`,
        i === 0 ? 120 : i + 1,
      ),
    ),
  },
  { slug: 'uml', title: 'UML', summary: 'Diagrams.', lessons: [] },
  ...Array.from({ length: 7 }, (_, i) => ({
    slug: `extra-${i}`,
    title: `Extra module ${i + 1}`,
    summary: 'Filler to make the curriculum long.',
    lessons: [lesson(`extra-${i}`, 1, `Only lesson of extra ${i + 1}`, 'Short.', 3)],
  })),
];

const outline = Array.from({ length: 40 }, (_, i) => ({
  level: i % 3 === 2 ? 3 : 2,
  text:
    i === 0 ? `${UNBREAKABLE} and why every adapter needs exactly one reason to change` : `Section ${i + 1}`,
  anchor: `section-${i + 1}`,
}));

const body = [
  `# ${LONG_TITLE}`,
  '',
  `Inline \`${UNBREAKABLE}\` and a link to https://github.com/Akus0ni/LowLevelDesignGuide/blob/main/src/Lld.Infrastructure/Persistence/Repositories.cs that has no spaces.`,
  '',
  ...outline.flatMap((entry) => [`${entry.level === 2 ? '##' : '###'} ${entry.text}`, '', 'Text.', '']),
].join('\n');

function lessonDetail(l: Lesson, moduleTitle: string, siblings: Lesson[]) {
  const index = siblings.findIndex((s) => s.id === l.id);
  const link = (s?: Lesson) =>
    s ? { id: s.id, moduleSlug: s.moduleSlug, slug: s.slug, title: s.title } : null;
  return {
    ...l,
    moduleTitle,
    bodyMarkdown: body,
    outline,
    cheatSheetTitle: `Cheat sheet: ${l.title}`,
    previous: link(siblings[index - 1]),
    next: link(siblings[index + 1]),
    terms: [{ term: UNBREAKABLE, role: 'Builds the options. '.repeat(12).trim(), anchor: 'section-1' }],
  };
}

const sheets = modules.flatMap((m) =>
  m.lessons.map((l) => ({
    lessonId: l.id,
    moduleSlug: m.slug,
    moduleTitle: m.title,
    lessonSlug: l.slug,
    lessonTitle: l.title,
    title: `Cheat sheet: ${l.title}`,
  })),
);

/** Serves the worst case instead of the real content. */
export async function routeWorstCase(page: Page): Promise<void> {
  await page.route('**/api/auth/refresh', (route) =>
    route.fulfill({ status: 401, json: { code: 'auth.session_invalid' } }),
  );
  await page.route('**/api/content/**', (route) => {
    const url = new URL(route.request().url());
    const path = url.pathname.replace(/^\/api\/content\//, '');
    const [kind, key] = path.split('/');
    const module = modules.find((m) => m.slug === key);
    const json = (() => {
      if (path === 'modules')
        return modules.map(({ lessons, ...m }, order) => ({ ...m, order, lessonCount: lessons.length }));
      if (kind === 'modules' && module)
        return { slug: module.slug, title: module.title, summary: module.summary, lessons: module.lessons };
      if (path === 'cheatsheets') {
        const filter = url.searchParams.get('module');
        return filter ? sheets.filter((s) => s.moduleSlug === filter) : sheets;
      }
      for (const m of modules) {
        const found = m.lessons.find((l) => l.id === key);
        if (found && kind === 'lessons') return lessonDetail(found, m.title, m.lessons);
        if (found && kind === 'cheatsheets') {
          return { ...sheets.find((s) => s.lessonId === key)!, bodyMarkdown: body };
        }
      }
      return null;
    })();
    return json === null
      ? route.fulfill({ status: 404, json: { code: 'content.not_found' } })
      : route.fulfill({ json });
  });
}

/** Screens to check, with the worst-case data behind them. */
export const WORST_CASE_SCREENS = [
  '/#/learn',
  '/#/learn/patterns',
  '/#/learn/patterns/lesson-1',
  '/#/learn/patterns/lesson-2',
  '/#/revise',
  '/#/revise/patterns/lesson-1',
];
