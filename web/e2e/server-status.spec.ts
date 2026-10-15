import { expect, test } from '@playwright/test';

test.beforeEach(async ({ page }) => {
  await page.route('**/api/info', (route) =>
    route.fulfill({ json: { name: 'LLD Guide API', version: '0.1.0', environment: 'Smoke' } }),
  );
  await page.route('**/api/auth/refresh', (route) =>
    route.fulfill({ status: 401, json: { title: 'Signed out', code: 'auth.session_invalid' } }),
  );
});

test('an access request made while the server is down is saved, then sent when it is back', async ({
  page,
}) => {
  // Server down: the tunnel can't reach the API.
  await page.route('**/api/access-requests', (route) => route.abort('internetdisconnected'));

  await page.goto('/#/profile');
  await page.getByRole('tab', { name: 'Request access' }).click();
  await page.getByLabel('Email').fill('newcomer@example.com');
  await page.getByLabel('Name').fill('New Comer');
  await page.getByRole('button', { name: 'Request access' }).click();

  await expect(page.getByText('Request saved, will be sent when the server is online.')).toBeVisible();
  const banner = page.locator('[role="status"][data-visible]');
  await expect(banner).toHaveAttribute('data-visible', 'true');
  await expect(banner).toContainText('Server offline');

  // Server back: the next successful call flips the status, and the saved request goes out by itself.
  let delivered = false;
  await page.unroute('**/api/access-requests');
  await page.route('**/api/access-requests', (route) => {
    delivered = true;
    return route.fulfill({ status: 202, json: { message: 'Thanks!' } });
  });
  await page.getByRole('link', { name: 'Learn' }).first().click();
  await page.getByRole('link', { name: 'Profile' }).first().click();

  await expect(page.getByText('Access request sent')).toBeVisible();
  await expect(banner).toHaveAttribute('data-visible', 'false');
  expect(delivered).toBe(true);
});
