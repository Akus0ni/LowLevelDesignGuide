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
  icon?: string | null;
  hook?: string | null;
}

// Spec 004: multi-codepoint emoji (ZWJ sequence, flag, variation selector), one lesson with no icon; hooks at the
// 40-character limit the validator enforces, one of them a single unbreakable identifier.
const ICONS = ['🧑‍💻', '🇮🇳', '🏳️‍🌈', null, '🎛️'];
const HOOKS = ['Fair across tenants, even if Redis dies.', 'IRefreshTokenRepositoryFactoryProviderCo', null, 'Jo'];

const lesson = (moduleSlug: string, n: number, title: string, summary: string, minutes: number): Lesson => ({
  id: `${moduleSlug}-${n}`,
  moduleSlug,
  slug: `lesson-${n}`,
  title,
  summary,
  order: n,
  readingMinutes: minutes,
  tags: ['strategy', 'observer', 'repository', 'unit-of-work', 'dependency-inversion'],
  icon: ICONS[(n - 1) % ICONS.length],
  hook: HOOKS[(n - 1) % HOOKS.length],
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

// Spec 004 map and mnemonic: a long package title, an untitled group, 14 links from the first lesson (a long card),
// and a 7-letter acrostic whose rows include the longest title and the unbreakable one.
const patternSlugs = Array.from({ length: 40 }, (_, i) => `lesson-${i + 1}`);
const map = {
  groups: [
    {
      title: 'Creational patterns that build objects without new, and why most factories are overkill in modern C#',
      lessons: patternSlugs.slice(0, 10),
    },
    { title: '', lessons: patternSlugs.slice(10) },
  ],
  links: [
    { from: 'lesson-1', to: 'lesson-2', label: 'is often confused with, but solves a different problem than' },
    ...patternSlugs.slice(2, 15).map((to) => ({ from: 'lesson-1', to, label: null })),
  ],
};
const mnemonics = [{ word: 'GO-FOR-IT', lessons: patternSlugs.slice(0, 7) }];

const LONG_LINE = `var options = new ${UNBREAKABLE}(tenantId: request.TenantId, window: TimeSpan.FromSeconds(60), limit: 1_000);`;
const spec004Body = [
  '> [!REMEMBER]',
  `> Never let \`${UNBREAKABLE}\` decide alone whether a tenant is over its limit: ask the limiter.`,
  '',
  '```csharp before title="Before: the 2019 version, with a 120-character line and a title much longer than anyone planned for" mark="1,3"',
  LONG_LINE,
  'var limiter = new Limiter(options);',
  'limiter.Check();',
  '```',
  '```csharp after title="After" mark="1"',
  'var limiter = Limiter.For(tenant);',
  '```',
  '',
  ...['NOTE', 'TIP', 'INTERVIEW', 'SMELL', 'PITFALL', 'APP', 'WORK'].flatMap((type) => [
    `> [!${type}]`,
    `> \`${UNBREAKABLE}\` at https://github.com/Akus0ni/LowLevelDesignGuide/blob/main/src/Lld.Infrastructure/Persistence/Repositories.cs`,
    '',
  ]),
  '## Interview questions',
  '',
  ...Array.from({ length: 9 }, (_, i) => [
    `### ${i === 0 ? `Why would ${UNBREAKABLE} ever need to know about tenants, windows, Redis and the on-call rota at the same time?` : `Question ${i + 1}?`}`,
    '',
    i === 0 ? `Because \`${UNBREAKABLE}\` grew. ${'It kept growing. '.repeat(20)}` : 'Short answer.',
    '',
  ]).flat(),
  '## Go deeper',
  '',
  ...Array.from({ length: 12 }, (_, i) => [`### Deep topic number ${i + 1} with a longer title`, '', 'Detail.', '']).flat(),
];

const body = [
  `# ${LONG_TITLE}`,
  '',
  `Inline \`${UNBREAKABLE}\` and a link to https://github.com/Akus0ni/LowLevelDesignGuide/blob/main/src/Lld.Infrastructure/Persistence/Repositories.cs that has no spaces.`,
  '',
  ...outline.flatMap((entry) => [`${entry.level === 2 ? '##' : '###'} ${entry.text}`, '', 'Text.', '']),
  ...spec004Body,
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
    icon: l.icon,
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
        return {
          slug: module.slug,
          title: module.title,
          summary: module.summary,
          lessons: module.lessons,
          ...(module.slug === 'patterns' ? { map, mnemonics } : {}),
        };
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
