/// <reference lib="dom" />
// `page.evaluate` callbacks run in the browser, hence the DOM types.
import { expect, test, type Page } from '@playwright/test';
import { allModules, routeContent } from './content';

// Spec 004 acceptance criteria A1, A6–A9 against the repository's real OOP & SOLID content.

test.beforeEach(async ({ page }) => {
  await routeContent(page);
});

const MODULE = '/#/learn/oop-solid';
const ENCAPSULATION = `${MODULE}/encapsulation`;

async function expectNoSidewaysScroll(page: Page) {
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
  expect(overflow, 'the page must never scroll sideways').toBeLessThanOrEqual(0);
}

test('A1: the module lists its lessons in order, under its map', async ({ page }) => {
  const module = allModules().find((m) => m.slug === 'oop-solid')!;
  await page.goto(MODULE);

  await expect(page.getByRole('heading', { level: 1 })).toHaveText(module.title);
  await expect(page.getByRole('region', { name: 'Map of this module' })).toBeVisible();
  await expect(page.getByRole('list').filter({ has: page.getByRole('link', { name: /Encapsulation/ }) }).last()
    .getByRole('link')).toHaveCount(module.lessonCount);
});

test('A9: tapping a lesson on the map traces its links; the card walks and opens', async ({ page }) => {
  await page.goto(MODULE);
  const map = page.getByRole('region', { name: 'Map of this module' });

  await map.getByRole('button', { name: 'Abstraction', exact: true }).click();
  const card = map.getByRole('region', { name: 'Abstraction' });
  await expect(card).toBeVisible();
  await expect(map.getByRole('button', { name: 'Polymorphism', exact: true })).toHaveAttribute('data-state', 'related');

  await card.getByRole('button', { name: /Polymorphism/ }).click();
  await expect(map.getByRole('button', { name: 'Polymorphism', exact: true })).toHaveAttribute('aria-pressed', 'true');

  await page.keyboard.press('Escape');
  await expect(map.getByRole('region')).toHaveCount(0);

  await map.getByRole('button', { name: 'Inheritance', exact: true }).click();
  await map.getByRole('link', { name: 'Open lesson →' }).click();
  await expect(page).toHaveURL(/#\/learn\/oop-solid\/inheritance$/);
});

test('A6: Before / After switches in place with marked lines; nothing scrolls sideways', async ({ page }) => {
  await page.goto(`${MODULE}/inheritance`);
  const compare = page.getByRole('figure', { name: /Before and after/ }).first();
  await compare.scrollIntoViewIfNeeded();

  if ((await compare.getAttribute('data-layout')) === 'tabs') {
    const box = await compare.boundingBox();
    await compare.getByRole('tab', { name: 'after' }).click();
    await expect(compare.getByRole('tab', { name: 'after' })).toHaveAttribute('aria-selected', 'true');
    expect((await compare.boundingBox())!.height, 'switching keeps the box height').toBe(box!.height);
    await compare.getByRole('tab', { name: 'after' }).press('ArrowLeft');
    await expect(compare.getByRole('tab', { name: 'before' })).toBeFocused();
  }
  await expect(page.locator('.compare [data-mark]').first()).toBeAttached();
  await expectNoSidewaysScroll(page);
});

test('A6: at 320 px no lesson page scrolls sideways', async ({ page }) => {
  await page.setViewportSize({ width: 320, height: 700 });
  for (const slug of ['encapsulation', 'abstraction', 'inheritance', 'polymorphism']) {
    await page.goto(`${MODULE}/${slug}`);
    await expect(page.locator('.qa-list')).toBeVisible();
    await expectNoSidewaysScroll(page);
  }
});

test('A7 + memory aids: every lesson opens with a stamp and a picture; callouts are labelled notes', async ({ page }) => {
  for (const slug of ['encapsulation', 'abstraction', 'inheritance', 'polymorphism']) {
    await page.goto(`${MODULE}/${slug}`);
    const stamp = page.getByRole('complementary', { name: 'remember', exact: true }).first();
    await expect(stamp).toContainText('«remember»');
    await expect(page.locator('.diagram').first()).toBeVisible();
    await expect(page.locator('header .lesson-icon-tile')).toHaveCount(1);
  }

  await page.goto(ENCAPSULATION);
  await page.getByRole('button', { name: /Open the deep dive/ }).click();
  for (const label of ['in this app', 'from my work']) {
    await expect(page.getByRole('complementary', { name: label, exact: true }).first()).toContainText(`«${label}»`);
  }
  await expect(page.getByText('[!REMEMBER]')).toHaveCount(0);
});

test('Go deeper starts folded, lists its topics and opens on tap', async ({ page }) => {
  await page.goto(ENCAPSULATION);
  const trigger = page.getByRole('button', { name: /Open the deep dive/ });

  await expect(trigger).toHaveAttribute('aria-expanded', 'false');
  await expect(trigger).toContainText('C# traps');
  await trigger.click();
  await expect(page.getByRole('heading', { level: 3, name: 'C# traps' })).toBeVisible();
});

test('the module mnemonic spells A-PIE and each letter opens its lesson', async ({ page }) => {
  await page.goto(MODULE);
  const mnemonic = page.getByRole('figure', { name: /^A-PIE:/ });

  await expect(mnemonic.locator('.mnemonic-letter')).toHaveText(['A', 'P', 'I', 'E']);
  await mnemonic.getByRole('link', { name: /Polymorphism/ }).click();
  await expect(page).toHaveURL(/#\/learn\/oop-solid\/polymorphism$/);
});

test('A8: interview answers start folded, open on tap, and Show all opens the rest', async ({ page }) => {
  await page.goto(ENCAPSULATION);
  const list = page.locator('.qa-list');
  const first = list.getByRole('button', { name: /What is encapsulation/ });

  await expect(first).toHaveAttribute('aria-expanded', 'false');
  await first.click();
  await expect(first).toHaveAttribute('aria-expanded', 'true');
  await expect(list.getByText(/protects its/)).toBeVisible();

  await list.getByRole('button', { name: 'Show all' }).click();
  await expect(list.getByRole('button', { expanded: false })).toHaveCount(0);
  await expect(page.getByRole('navigation', { name: /On this page/i }).getByText(/What is encapsulation/)).toHaveCount(0);
});
