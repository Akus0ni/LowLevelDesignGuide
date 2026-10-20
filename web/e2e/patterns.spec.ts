/// <reference lib="dom" />
// `page.evaluate` callbacks run in the browser, hence the DOM types.
import { expect, test, type Page } from '@playwright/test';
import { allModules, lessonSlugsOf, routeContent } from './content';

// Spec 006 acceptance criteria A1, A5–A9 against the repository's real pattern content.

test.beforeEach(async ({ page }) => {
  await routeContent(page);
});

const GOF = '/#/learn/gof-patterns';
const ENTERPRISE = '/#/learn/enterprise-patterns';

async function expectNoSidewaysScroll(page: Page) {
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
  expect(overflow, 'the page must never scroll sideways').toBeLessThanOrEqual(0);
}

test('A1: GoF has 23 lessons and Enterprise 5, in reading order, before UML and case studies', async ({ page }) => {
  const modules = allModules();
  expect(modules.map((m) => m.slug)).toEqual(['start', 'oop-solid', 'gof-patterns', 'enterprise-patterns', 'case-studies']);
  expect(lessonSlugsOf('gof-patterns')).toHaveLength(23);
  expect(lessonSlugsOf('gof-patterns').slice(0, 3)).toEqual(['singleton', 'factory-method', 'abstract-factory']);
  expect(lessonSlugsOf('enterprise-patterns')).toEqual([
    'repository',
    'unit-of-work',
    'specification',
    'null-object',
    'di-vs-service-locator',
  ]);

  await page.goto(GOF);
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('GoF Design Patterns');
  await expect(page.locator('.ptable-tile')).toHaveCount(23);
});

test('A5: the periodic table marks links and look-alikes, opens a card, and moves with the keyboard', async ({ page }) => {
  await page.goto(GOF);
  const table = page.getByRole('region', { name: 'Periodic table of patterns' });
  await expect(table.getByText('«creational»')).toBeVisible();
  await expect(table.getByText('«structural»')).toBeVisible();
  await expect(table.getByText('«behavioral»')).toBeVisible();

  await table.getByRole('button', { name: 'Strategy (St)' }).click();
  await expect(table.getByRole('button', { name: 'State (Sa)' })).toHaveAttribute('data-state', 'lookalike');
  await expect(table.getByRole('button', { name: 'Factory Method (Fm)' })).toHaveAttribute('data-state', 'related');
  const card = table.getByRole('region', { name: 'Strategy' });
  await expect(card.getByRole('link', { name: /Compare Strategy · State/ })).toBeVisible();

  await page.keyboard.press('Escape');
  await expect(table.getByRole('region', { name: 'Strategy' })).toHaveCount(0);

  await table.getByRole('button', { name: 'Singleton (Sg)' }).focus();
  await page.keyboard.press('ArrowRight');
  await expect(table.getByRole('button', { name: 'Factory Method (Fm)' })).toBeFocused();
  await page.keyboard.press('End');
  await expect(table.getByRole('button', { name: 'Interpreter (In)' })).toBeFocused();

  await table.getByRole('button', { name: 'Interpreter (In)' }).click();
  await table.getByRole('region', { name: 'Interpreter' }).getByRole('link', { name: 'Open lesson →' }).click();
  await expect(page).toHaveURL(/#\/learn\/gof-patterns\/interpreter$/);
});

test('A6: the picker asks one question at a time, steps back, and ends at a lesson', async ({ page }) => {
  await page.goto(GOF);
  await page.getByRole('button', { name: 'Which pattern?' }).click();
  const sheet = page.getByRole('dialog', { name: 'Which pattern?' });

  await sheet.getByRole('button', { name: 'How objects behave and talk' }).click();
  await sheet.getByRole('button', { name: 'Change how a job is done' }).click();
  await expect(sheet.getByText('Who decides how it is done?')).toBeVisible();

  await page.keyboard.press('Escape');
  await expect(sheet.getByText('What do you need?')).toBeVisible();
  await sheet.getByRole('button', { name: 'Back' }).click();
  await expect(sheet.getByText('What is your problem about?')).toBeVisible();

  await sheet.getByRole('button', { name: 'Fitting objects together' }).click();
  await sheet.getByRole('button', { name: /Add to an object, or guard it/ }).click();
  await sheet.getByRole('button', { name: 'Adding behaviour, in layers' }).click();
  await expect(sheet.getByText('Try Decorator')).toBeVisible();
  await sheet.getByRole('link', { name: /Decorator/ }).click();
  await expect(page).toHaveURL(/#\/learn\/gof-patterns\/decorator$/);
});

test('A7: a compare opens from a lesson and keeps its height when switching', async ({ page, isMobile }) => {
  await page.goto(`${GOF}/strategy`);
  const strip = page.getByRole('navigation', { name: 'Often confused with' });
  await strip.getByRole('link', { name: /Strategy ⇄ State/ }).click();
  await expect(page).toHaveURL(/compare\/strategy-vs-state$/);

  await expect(page.getByRole('complementary', { name: 'The difference' })).toContainText('Ask yourself');
  await expect(page.getByText(/Each \*\*way of doing one job\*\*|Each way of doing one job/)).toBeVisible();
  // Both panes stay mounted; wait until both structure diagrams are drawn before measuring.
  await expect(page.locator('.lookalike-pane .diagram:not(.diagram-loading)')).toHaveCount(2);
  if (isMobile) {
    const panes = page.locator('.lookalike-panes');
    const height = (await panes.boundingBox())!.height;
    await page.getByRole('tab', { name: /State/ }).click();
    await expect(page.getByRole('tab', { name: /State/ })).toHaveAttribute('aria-selected', 'true');
    expect((await panes.boundingBox())!.height, 'switching keeps the height').toBe(height);
  } else {
    await expect(page.getByRole('region', { name: 'State' })).toBeVisible();
  }
  await expectNoSidewaysScroll(page);
});

test('A8: phrase mnemonics draw one tile per word; SOLID is unchanged', async ({ page }) => {
  await page.goto(GOF);
  const behavioral = page.getByRole('figure', { name: /Cats Can Ignore/ });
  await expect(behavioral.getByRole('link')).toHaveCount(11);
  await expect(behavioral.locator('figcaption b')).toHaveText(['C', 'C', 'I', 'I', 'M', 'M', 'O', 'S', 'S', 'T', 'V']);

  await page.goto('/#/learn/oop-solid');
  await expect(page.getByRole('figure', { name: /^SOLID/ }).getByRole('link')).toHaveCount(5);
});

test('A9: every pattern lesson has a structure diagram and an Overuse stamp', async ({ page }) => {
  for (const [module, base] of [
    ['gof-patterns', GOF],
    ['enterprise-patterns', ENTERPRISE],
  ] as const) {
    for (const slug of lessonSlugsOf(module)) {
      await page.goto(`${base}/${slug}`);
      await expect(page.getByRole('complementary', { name: 'when it hurts' })).toBeVisible();
      await expect(page.locator('#structure')).toBeAttached();
    }
  }
});

test('A5: at 320 px the pattern pages never scroll sideways', async ({ page }) => {
  await page.setViewportSize({ width: 320, height: 700 });
  for (const path of [GOF, ENTERPRISE, `${GOF}/compare/decorator-vs-proxy-vs-adapter`, `${GOF}/chain-of-responsibility`]) {
    await page.goto(path);
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
    await expectNoSidewaysScroll(page);
  }
});
