import { test, expect } from '@playwright/test';

test('basic search from home page GO button returns a results page', async ({ page, baseURL }) => {
  await page.goto('/');

  await page.locator('#SobekHomeSearchBox').fill('a');
  await page.locator('#sbkBsav_SearchButton').click();

  await page.waitForURL(/\/results\//);

  const finalHost = new URL(page.url()).hostname;
  const expectedHost = new URL(baseURL!).hostname;
  expect(finalHost).toBe(expectedHost);
  expect(finalHost).not.toContain('sobekdigital.com');

  await expect(page).toHaveTitle(/Search Results/);

  const results = page.locator('.sbkBrv_SingleResult');
  await expect(results.first()).toBeVisible();
  expect(await results.count()).toBeGreaterThan(0);
});
