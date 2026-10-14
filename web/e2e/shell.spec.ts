import { expect, test } from '@playwright/test';

// The real API is covered by the Aspire integration test; here we only check the UI shell.
test.beforeEach(async ({ page }) => {
  await page.route('**/api/info', (route) =>
    route.fulfill({ json: { name: 'LLD Guide API', version: '0.1.0', environment: 'Smoke' } }),
  );
});

test('opens on Learn and navigates to Profile', async ({ page, isMobile }) => {
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Learn' })).toBeVisible();

  const nav = page.getByRole('navigation', { name: 'Main' }).filter({ visible: true });
  await expect(nav).toHaveCount(1);

  const navBox = await nav.boundingBox();
  const viewport = page.viewportSize()!;
  if (isMobile) {
    // Bottom tab bar on phones.
    expect(navBox!.y + navBox!.height).toBeCloseTo(viewport.height, 0);
  } else {
    // Left sidebar on desktop.
    expect(navBox!.x).toBeLessThan(viewport.width / 3);
  }

  await nav.getByRole('link', { name: 'Profile' }).click();
  await expect(page).toHaveURL(/#\/profile$/);
  await expect(page.getByTestId('info-name')).toHaveText('LLD Guide API');
  await expect(page.getByTestId('transport')).toHaveText('HTTP API');
});
