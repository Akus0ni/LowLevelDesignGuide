/// <reference lib="dom" />
// `page.evaluate` callbacks run in the browser, hence the DOM types.
import { expect, test } from '@playwright/test';
import { routeWorstCase, WORST_CASE_SCREENS } from './fixtures/worstCase';

// break-ui regression (spec 003 T18): worst-case content must never make a screen scroll sideways or overflow a box.

test.beforeEach(async ({ page }) => {
  await routeWorstCase(page);
});

for (const width of [320, 1360]) {
  test(`worst-case content fits at ${width} px`, async ({ page }) => {
    await page.setViewportSize({ width, height: 800 });
    for (const path of WORST_CASE_SCREENS) {
      await page.goto(path);
      await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
      const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
      expect(overflow, `${path} scrolls sideways`).toBeLessThanOrEqual(0);
    }
  });
}

test('a long module name in the back link stays on one line, with the full name in the tooltip', async ({
  page,
}) => {
  await page.goto('/#/learn/patterns/lesson-2');
  const back = page
    .getByRole('main')
    .getByRole('link', { name: /Gang of Four Design Patterns/ })
    .first();

  await expect(back).toHaveAttribute('title', /Behavioural Patterns in Modern C#/);
  expect((await back.boundingBox())!.height).toBeLessThanOrEqual(44);
});

test('long reading times read as hours', async ({ page }) => {
  await page.goto('/#/learn/patterns');
  await expect(page.getByText('40 lessons · 15 h 39 min')).toBeVisible();
  await expect(page.getByRole('link', { name: /Case Study 12/ }).first()).toContainText('2 h');
});
