import { test, expect } from '@playwright/test';

test('home page loads without redirecting to sobekdigital.com', async ({ page, baseURL }) => {
  const response = await page.goto('/');

  expect(response?.status()).toBe(200);

  const finalHost = new URL(page.url()).hostname;
  const expectedHost = new URL(baseURL!).hostname;

  expect(finalHost).toBe(expectedHost);
  expect(finalHost).not.toContain('sobekdigital.com');

  await expect(page).toHaveTitle(/.+/);
});
