/// <reference lib="dom" />
// `page.evaluate` callbacks run in the browser, hence the DOM types.
import { expect, test, type Page } from '@playwright/test';
import { allCheatSheets, routeContent } from './content';

// Spec 003 acceptance criteria A5-A8, A13-A15, A17 against the repository's real content.

test.beforeEach(async ({ page }) => {
  await routeContent(page);
});

const AUTH = '/#/learn/case-studies/auth';

async function noSidewaysScroll(page: Page) {
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
  expect(overflow, 'the page must never scroll sideways').toBeLessThanOrEqual(0);
}

test('A13: Learn draws the curriculum as linked class boxes, intro once', async ({ page }) => {
  await page.goto('/#/learn');

  const boxes = page.getByRole('list', { name: 'Modules in reading order' }).getByRole('article');
  await expect(boxes).toHaveCount(2);
  await expect(boxes.first()).toContainText('«module»');
  await expect(boxes.first()).toHaveClass(/animate-rise/);

  await boxes
    .first()
    .getByRole('link', { name: /Start here/ })
    .click();
  await page.getByRole('link', { name: 'Learn' }).first().click();
  await expect(
    page.getByRole('list', { name: 'Modules in reading order' }).getByRole('article').first(),
  ).not.toHaveClass(/animate-rise/);
});

test('A13: reduced motion turns the intro into a fade', async ({ page }) => {
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await page.goto('/#/learn');

  const name = await page
    .getByRole('article')
    .first()
    .evaluate((box) => getComputedStyle(box).animationName);
  expect(name).toBe('appear');
});

for (const colorScheme of ['light', 'dark'] as const) {
  test(`A5: the Auth lesson renders outline, C#, diagrams and tables (${colorScheme})`, async ({
    page,
    isMobile,
  }) => {
    await page.emulateMedia({ colorScheme });
    await page.goto(AUTH);

    await expect(page.getByRole('heading', { level: 1 })).toHaveText('Case Study 1: Authentication & Access');
    await expect(page.locator('.diagram-canvas svg')).toHaveCount(4, { timeout: 15_000 });
    await expect(page.locator('.diagram-failed')).toHaveCount(0);
    await expect(page.locator('.code-block .code-line').first()).toBeVisible();
    await expect(page.getByRole('region', { name: 'Table' }).first()).toBeVisible();

    if (isMobile) {
      await page.getByRole('button', { name: 'On this page' }).click();
      await expect(page.getByRole('dialog').getByRole('link')).toHaveCount(13);
    } else {
      await expect(page.getByRole('navigation', { name: 'On this page' }).getByRole('link')).toHaveCount(13);
    }
  });
}

test('A5: app links stay in the app; GitHub links open outside', async ({ page }) => {
  await page.goto('/#/learn/start/how-to-use');
  await page.getByRole('link', { name: "this app's own sign-in" }).click();
  await expect(page).toHaveURL(/#\/learn\/case-studies\/auth$/);

  const popup = page.waitForEvent('popup');
  await page
    .getByRole('link', { name: /^AccessRequest\.cs/ })
    .first()
    .click();
  expect((await popup).url()).toContain(
    'github.com/Akus0ni/LowLevelDesignGuide/blob/main/src/Lld.Domain/Access/AccessRequest.cs',
  );
});

test('A6: no screen scrolls sideways at 320 px', async ({ page }) => {
  await page.setViewportSize({ width: 320, height: 700 });
  for (const path of [
    '/#/learn',
    '/#/learn/case-studies',
    AUTH,
    '/#/revise',
    '/#/revise/case-studies/auth',
  ]) {
    await page.goto(path);
    await page.waitForTimeout(path === AUTH ? 2500 : 500);
    await noSidewaysScroll(page);
  }
});

test('A7: lesson to cheat sheet in one tap and back; Revise lists and filters', async ({ page }) => {
  await page.goto(AUTH);
  await page.getByRole('link', { name: 'Cheat sheet', exact: true }).click();
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Cheat sheet: Authentication & Access');

  await page.getByRole('link', { name: /Case Study 1: Authentication & Access/ }).click();
  await expect(page).toHaveURL(/#\/learn\/case-studies\/auth$/);

  await page.goto('/#/revise');
  await expect(page.getByRole('link', { name: /Cheat sheet:/ })).toHaveCount(allCheatSheets().length);
  await page.getByRole('button', { name: 'Case studies' }).click();
  await expect(page).toHaveURL(/module=case-studies/);
  await expect(page.getByRole('link', { name: /Cheat sheet:/ })).toHaveCount(1);
});

test('A8: every cheat sheet fits its screen budget at 390 × 844', async ({ page, isMobile }) => {
  test.skip(!isMobile, 'measured on a phone');
  await page.setViewportSize({ width: 390, height: 844 });
  for (const sheet of allCheatSheets()) {
    await page.goto(`/#/revise/${sheet.moduleSlug}/${sheet.lessonSlug}`);
    await expect(page.getByRole('heading', { level: 1 })).toHaveText(sheet.title);
    await page.waitForTimeout(1500); // diagrams
    const screens = await page.evaluate(() => document.documentElement.scrollHeight / window.innerHeight);
    const budget = sheet.moduleSlug === 'case-studies' ? 5 : 3; // ≤ 4 scrolls for case studies, ≤ 2 otherwise
    expect(screens, `${sheet.title}: ${screens.toFixed(2)} screens`).toBeLessThanOrEqual(budget);
  }
});

test('A14: tap a class to trace it; step through a sequence diagram', async ({ page }) => {
  await page.goto(AUTH);
  const classes = page.getByRole('figure', { name: 'class diagram' });
  await classes.getByRole('button', { name: 'Class SessionManager' }).click();
  await expect(classes.locator('svg[data-focus]')).toHaveCount(1);
  await expect(classes.locator('figcaption')).toContainText('SessionManager · 5 links');
  await page.keyboard.press('Escape');
  await expect(classes.locator('svg[data-focus]')).toHaveCount(0);

  const sequence = page.getByRole('figure', { name: 'sequence diagram' }).first();
  await sequence.getByRole('button', { name: 'step through' }).click();
  await page.keyboard.press('ArrowRight');
  await page.keyboard.press('ArrowRight');
  await expect(sequence.locator('figcaption')).toContainText('3/15');
  await expect(sequence.locator('[aria-live]')).toContainText('Step 3 of 15');
  await sequence.getByRole('button', { name: 'all' }).click();
  await expect(sequence.locator('[data-step]')).toHaveCount(0);
});

test('A15: AccessRequest in code explains itself and links to the entities table', async ({ page }) => {
  await page.goto(AUTH);
  await page.getByRole('button', { name: 'AccessRequest: what is it?' }).first().click();
  const card = page.getByRole('dialog');
  await expect(card).toContainText("A person's request and its lifecycle");
  await card.getByRole('link', { name: 'Read more →' }).click();
  await expect(page).toHaveURL(/at=3-core-entities/);
});

test('A17: recall folds the cheat sheet, reveals on tap, and is remembered', async ({ page }) => {
  await page.goto('/#/revise/case-studies/auth');
  await page.getByRole('switch', { name: 'Recall' }).click();
  const folded = page.getByRole('button', { name: /think first, then reveal/ });
  await expect(folded).toHaveCount(5);

  await folded.first().click();
  await expect(folded).toHaveCount(4);

  await page.reload();
  await expect(page.getByRole('switch', { name: 'Recall' })).toHaveAttribute('aria-checked', 'true');
  await page.getByRole('button', { name: 'Reveal all' }).click();
  await expect(folded).toHaveCount(0);
});
