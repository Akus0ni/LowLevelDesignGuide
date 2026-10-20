/// <reference lib="dom" />
// `page.evaluate` callbacks run in the browser, hence the DOM types.
import { expect, test, type Page } from '@playwright/test';
import { lessonSlugsOf, routeContent } from './content';

// Spec 005: progress end to end in browser mode, signed out (the guest's progress lives in localStorage).

const [first, second] = lessonSlugsOf('oop-solid');

test.beforeEach(async ({ page }) => {
  await routeContent(page);
});

/**
 * Clicks without scrolling. The Done control sits at the end of the lesson, and scrolling there ticks the sections on
 * screen after a moment (R101), which can swap the button mid-click.
 */
async function press(page: Page, name: string) {
  await page.getByRole('button', { name }).evaluate((button: HTMLElement) => button.click());
}

async function lessonRow(page: Page, slug: string) {
  return page.getByRole('region', { name: 'Lessons' }).locator(`a[href="#/learn/oop-solid/${slug}"]`);
}

test('A1: opening starts a lesson; marking done and back updates every view, and survives a reload', async ({ page }) => {
  await page.goto(`/#/learn/oop-solid/${first}`);
  await expect(page.getByRole('button', { name: 'Mark as done' })).toBeVisible();

  await page.goto('/#/learn/oop-solid');
  await expect((await lessonRow(page, first)).getByRole('img', { name: 'In progress' })).toBeVisible();

  await page.goto(`/#/learn/oop-solid/${first}`);
  await press(page, 'Mark as done');
  await expect(page.getByRole('button', { name: 'Mark as not done' })).toBeFocused();

  await page.reload();
  await page.goto('/#/learn/oop-solid');
  await expect((await lessonRow(page, first)).getByRole('img', { name: 'Done' })).toBeVisible();
  await expect(page.getByLabel(/^1 of \d+ lessons done$/).first()).toBeVisible();

  await page.goto(`/#/learn/oop-solid/${first}`);
  await press(page, 'Mark as not done');
  await page.goto('/#/learn/oop-solid');
  await expect((await lessonRow(page, first)).getByRole('img', { name: 'In progress' })).toBeVisible();
});

test('R101: a fling to the end reads nothing; staying ticks only what is on screen', async ({ page }) => {
  await page.goto(`/#/learn/oop-solid/${first}`);
  await page.getByRole('heading', { level: 1 }).waitFor();
  await page.evaluate(() => window.scrollTo(0, document.documentElement.scrollHeight));
  await page.waitForTimeout(1000);
  await expect(page.getByText(/^0 of \d+ sections read$/)).toBeVisible();

  await expect(page.getByText(/^[1-9]\d* of \d+ sections read$/)).toBeVisible({ timeout: 5000 });
  await expect(page.getByRole('button', { name: 'Mark as done' })).toBeVisible();
});

test('A3: Continue opens the lesson in progress where it was left', async ({ page }) => {
  await page.goto(`/#/learn/oop-solid/${second}`);
  await page.getByRole('heading', { level: 1 }).waitFor();
  await page.evaluate(() => window.scrollTo(0, 900));
  // Position saves after a short pause, then the store writes after a quiet second.
  await page.waitForTimeout(1600);

  await page.goto('/#/learn');
  const card = page.getByRole('link', { name: /^Continue:/ });
  await expect(card).toHaveAttribute('href', `#/learn/oop-solid/${second}`);
  await card.click();

  await expect.poll(() => page.evaluate(() => window.scrollY)).toBeGreaterThan(600);
});

test('A7: Revise shows what was revised, and can hide it', async ({ page }) => {
  await page.goto(`/#/revise/oop-solid/${first}`);
  await page.getByRole('switch', { name: 'Recall' }).click();
  await page.getByRole('button', { name: 'Reveal all' }).click();
  await page.waitForTimeout(1200);

  await page.goto('/#/revise?module=oop-solid');
  const sheets = page.getByRole('region', { name: 'OOP & SOLID' });
  await expect(sheets.getByText('Revised today')).toHaveCount(1);

  await page.getByRole('radio', { name: 'Not revised' }).click();
  await expect(sheets.getByText('Revised today')).toHaveCount(0);
  await expect(sheets.getByText('Not revised yet').first()).toBeVisible();
});

test('A9: resetting a module clears it, after picking it', async ({ page }) => {
  await page.goto(`/#/learn/oop-solid/${first}`);
  await press(page, 'Mark as done');

  await page.goto('/#/profile');
  const progress = page.getByRole('region', { name: 'Progress' });
  await expect(progress.getByText('lessons done').locator('..')).toContainText('1 of');
  await progress.getByRole('button', { name: 'Reset progress' }).click();
  await expect(page.getByRole('button', { name: 'Choose what to reset' })).toBeDisabled();
  await page.getByRole('radio', { name: 'OOP & SOLID' }).click();
  await page.getByRole('button', { name: 'Reset OOP & SOLID' }).click();

  await expect(page.getByText('OOP & SOLID progress reset')).toBeVisible();
  await expect(progress.getByText('lessons done').locator('..')).toContainText('0 of');
});

test('A14: a lesson’s status looks the same on Learn, its module and Revise', async ({ page }) => {
  await page.goto(`/#/learn/oop-solid/${first}`);
  await press(page, 'Mark as done');

  await page.goto('/#/learn');
  const box = page.locator('article', { has: page.getByRole('heading', { name: 'OOP & SOLID' }) });
  await expect(box.locator(`a[href="#/learn/oop-solid/${first}"]`).getByRole('img', { name: 'Done' })).toBeVisible();

  await page.goto('/#/learn/oop-solid');
  await expect((await lessonRow(page, first)).getByRole('img', { name: 'Done' })).toBeVisible();

  await page.goto('/#/revise?module=oop-solid');
  await expect(
    page.locator(`a[href="#/revise/oop-solid/${first}"]`).getByRole('img', { name: 'Done' }),
  ).toBeVisible();
});
