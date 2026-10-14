import { expect, test } from '@playwright/test';

const user = {
  id: 'u1',
  email: 'learner@example.com',
  displayName: 'Ada Learner',
  roles: ['Learner'],
  mustChangePassword: true,
};
const session = (mustChangePassword: boolean) => ({
  accessToken: 'token',
  accessTokenExpiresAt: '2030-01-01T00:00:00Z',
  refreshToken: null,
  refreshTokenExpiresAt: '2030-01-01T00:00:00Z',
  user: { ...user, mustChangePassword },
});

test.beforeEach(async ({ page }) => {
  await page.route('**/api/info', (route) =>
    route.fulfill({ json: { name: 'LLD Guide API', version: '0.1.0', environment: 'Smoke' } }),
  );
  // No refresh cookie yet: the app starts signed out.
  await page.route('**/api/auth/refresh', (route) =>
    route.fulfill({ status: 401, json: { title: 'Signed out', code: 'auth.session_invalid' } }),
  );
});

test('temporary password sign-in forces a new password before the app opens', async ({ page }) => {
  await page.route('**/api/auth/login', async (route) => {
    expect(route.request().headers()['x-lld-client']).toBe('web');
    await route.fulfill({ json: session(true) });
  });
  await page.route('**/api/auth/change-password', (route) => route.fulfill({ json: session(false) }));

  await page.goto('/#/profile');
  await page.getByLabel('Email').fill(user.email);
  await page.getByLabel('Password').fill('TempPass-1234-xyz');
  await page.getByRole('button', { name: 'Sign in' }).click();

  await expect(page.getByRole('heading', { name: 'Set a new password' })).toBeVisible();
  await expect(page.getByRole('navigation', { name: 'Main' })).toHaveCount(0);

  await page.getByLabel('Current password').fill('TempPass-1234-xyz');
  await page.getByLabel('New password', { exact: true }).fill('a-brand-new-password');
  await page.getByLabel('Confirm new password').fill('a-brand-new-password');
  await page.getByRole('button', { name: 'Save and continue' }).click();

  await expect(page.getByText('Ada Learner')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Sign out of all devices' })).toBeVisible();
});

test('request access shows a confirmation', async ({ page }) => {
  await page.route('**/api/access-requests', (route) =>
    route.fulfill({ status: 202, json: { message: 'Thanks!' } }),
  );

  await page.goto('/#/profile');
  await page.getByRole('tab', { name: 'Request access' }).click();
  await page.getByLabel('Email').fill('newcomer@example.com');
  await page.getByLabel('Name').fill('New Comer');
  await page.getByRole('button', { name: 'Request access' }).click();

  await expect(page.getByRole('status')).toContainText('newcomer@example.com');
});
