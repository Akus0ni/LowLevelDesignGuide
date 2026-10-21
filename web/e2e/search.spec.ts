/// <reference lib="dom" />
// `page.evaluate` callbacks run in the browser, hence the DOM types.
import { expect, test, type Page } from '@playwright/test';
import { routeContent, routeSearch } from './content';

// Spec 009: search end to end in browser mode, signed out, over the real content (responses snapshotted from the
// real engine for the requests in fixtures/search-requests.json).

test.beforeEach(async ({ page }) => {
  await routeContent(page);
  await routeSearch(page);
});

const searchBox = (page: Page) =>
  page.getByRole('searchbox', { name: 'Search lessons, cheat sheets and your notes' });
const matchBar = (page: Page) => page.getByRole('group', { name: 'Search matches' });

/** How many ranges the CSS highlight registry holds for the search marks. */
const markCount = (page: Page) =>
  // The DOM typings don't list the registry's Map methods yet.
  page.evaluate(() => (CSS.highlights as unknown as Map<string, Set<Range>>).get('search')?.size ?? 0);

test('search, open the Best match, step through the marks and clear them (A5, A6)', async ({ page }) => {
  await page.goto('/#/search');
  await expect(page.getByRole('button', { name: 'Observer' })).toBeVisible();

  await searchBox(page).fill('decor');
  const best = page.getByRole('link', { name: /Best match/ });
  await expect(best).toContainText('Decorator');
  await expect(page.getByText(/^\d+ results/)).toBeVisible();
  await best.click();

  await expect(page).toHaveURL(/#\/learn\/gof-patterns\/decorator\?q=decor$/);
  const bar = matchBar(page);
  // Opened at the top: the marks are counted, none is current until the learner steps.
  await expect(bar).toContainText(/\d+ matches/);
  await expect.poll(() => markCount(page)).toBeGreaterThan(1);
  await bar.getByRole('button', { name: 'Next match' }).click();
  await expect(bar).toContainText(/1 of \d+/);

  await bar.getByRole('button', { name: 'Clear search marks' }).click();
  await expect(bar).toBeHidden();
  await expect(page).toHaveURL(/#\/learn\/gof-patterns\/decorator$/);
  await expect.poll(() => markCount(page)).toBe(0);
});

test('Back from a result returns to the same list, and the search is remembered (A5, A8)', async ({
  page,
}) => {
  await page.goto('/#/search');
  await searchBox(page).fill('decor');
  await page.getByRole('link', { name: /Best match/ }).click();
  await expect(matchBar(page)).toBeVisible();

  await page.goBack();
  await expect(searchBox(page)).toHaveValue('decor');
  await expect(page.getByRole('link', { name: /Best match/ })).toContainText('Decorator');

  await page.getByRole('button', { name: 'Clear', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Recent' })).toBeVisible();
  await expect(page.getByRole('button', { name: /decor/ })).toBeVisible();
});

test('filters by kind through the URL (A5)', async ({ page }) => {
  await page.goto('/#/search?q=decor');
  await page.getByRole('button', { name: /^Cheat sheets/ }).click();

  await expect(page).toHaveURL(/kind=cheatSheet/);
  const hits = page.locator('[data-search-hit]');
  await expect(hits.first()).toBeVisible();
  // The previous list stays until the new answer arrives (no flash), so wait for it.
  await expect
    .poll(() => hits.evaluateAll((links) => [...new Set(links.map((l) => l.getAttribute('data-kind')))]))
    .toEqual(['cheatSheet']);
});

test('acronyms find their long form and say so (A2, A5)', async ({ page }) => {
  await page.goto('/#/search?q=SRP');

  await expect(page.getByText(/also searching single responsibility/)).toBeVisible();
  await expect(page.getByRole('link', { name: /Best match/ })).toContainText('Single Responsibility');
});

test('a suggestion searches with one tap (A5)', async ({ page }) => {
  await page.goto('/#/search');
  await page.getByRole('button', { name: 'Observer' }).click();

  await expect(searchBox(page)).toHaveValue('Observer');
  await expect(page.getByRole('link', { name: /Best match/ })).toContainText('Observer');
});

test('on a cheat sheet in recall mode only the matched section is revealed (A6, Q8)', async ({ page }) => {
  await page.goto('/#/revise');
  await page.evaluate(() => localStorage.setItem('lld.reader.recall', 'true'));
  await page.goto('/#/revise/gof-patterns/decorator?at=interview-one-liners&q=wraps');

  await expect(matchBar(page)).toBeVisible();
  await expect(page.getByText('Same interface, wraps one, adds one thing.', { exact: false })).toBeVisible();
  await expect(page.getByRole('button', { name: /think first, then reveal/ })).toHaveCount(4);
});

test('marks read in both themes and nothing scrolls sideways at 320 px (A13)', async ({ page }) => {
  await page.setViewportSize({ width: 320, height: 640 });
  for (const scheme of ['light', 'dark'] as const) {
    await page.emulateMedia({ colorScheme: scheme });
    await page.goto('/#/search?q=decor');
    const mark = page.locator('mark[data-search]').first();
    await expect(mark).toBeVisible();
    const contrast = await mark.evaluate((element: HTMLElement) => {
      const tone = getComputedStyle(document.documentElement).getPropertyValue('--tone-match').trim();
      const probe = document.createElement('span');
      probe.style.color = tone;
      document.body.append(probe);
      const fill = getComputedStyle(probe).color;
      probe.remove();
      const channels = (value: string) => (value.match(/[\d.]+/g) ?? []).slice(0, 3).map(Number);
      const luminance = (value: string) => {
        const [r, g, b] = channels(value).map((c) => {
          const s = c / 255;
          return s <= 0.03928 ? s / 12.92 : ((s + 0.055) / 1.055) ** 2.4;
        });
        return 0.2126 * r + 0.7152 * g + 0.0722 * b;
      };
      const [a, b] = [luminance(getComputedStyle(element).color), luminance(fill)].sort((x, y) => y - x);
      return (a + 0.05) / (b + 0.05);
    });
    expect(contrast).toBeGreaterThanOrEqual(4.5);
    expect(
      await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth),
    ).toBe(0);
  }
});

test.describe('quick search (A9)', () => {
  test('Ctrl+K, type, Enter opens the lesson with its marks; not offered on phones', async ({
    page,
    isMobile,
  }) => {
    await page.goto('/#/learn');
    await expect(page.getByRole('heading', { level: 1, name: 'Learn' })).toBeVisible();
    await page.keyboard.press('Control+K');
    if (isMobile) {
      await expect(page.getByRole('dialog')).toHaveCount(0);
      return;
    }

    const box = page.getByRole('combobox', { name: 'Search' });
    await expect(box).toBeFocused();
    await box.fill('observer');
    await expect(page.getByRole('option').first()).toContainText('Observer');
    await page.keyboard.press('Enter');

    await expect(page).toHaveURL(/#\/learn\/gof-patterns\/observer\?q=observer$/);
    await expect(matchBar(page)).toBeVisible();
  });
});
