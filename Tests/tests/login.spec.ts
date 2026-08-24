import { test, expect } from '@playwright/test';

const username = process.env.SOBEKCM_TEST_USERNAME;
const password = process.env.SOBEKCM_TEST_PASSWORD;

test('user can log in with valid credentials', async ({ page }) => {
  test.skip(!username || !password, 'SOBEKCM_TEST_USERNAME / SOBEKCM_TEST_PASSWORD not set -- copy .env.example to .env and fill them in');

  await page.goto('/my/logon');

  await page.locator('#form_logon_term').click();
  await page.locator('#logon_username').fill(username!);
  await page.locator('#logon_password').fill(password!);
  await page.getByRole('button', { name: 'LOGIN' }).click();

  await expect(page.locator('a[href*="my/logout"]').first()).toBeVisible();
});
