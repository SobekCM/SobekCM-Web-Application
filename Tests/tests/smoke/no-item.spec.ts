import { test, expect } from '@playwright/test';

// TEMPORARY: exact copy of item.spec.ts pointed at a BibID that doesn't exist, just to see this
// fail for real on a run. Follow-up (next round): replace this with an actual "item not found"
// test that asserts on whatever the app's real not-found behavior is (status code, message, etc)
// instead of expecting the same title/thumbnail as a real item.
test('requesting a non-existent resource informs user and returns 404', async ({ page, request, baseURL }) => {
  const response = await page.goto('/XX00000016/00001');
  expect(response?.status()).toBe(404);

  // Somewhere in the main page should be the 'Page Not Found' message
  const panel = page.locator('#sbkWchs_InnerPanel');
  await expect(panel).toContainText('Page Not Found');
});
