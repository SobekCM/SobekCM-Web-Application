import { test, expect } from '@playwright/test';

test('requesting a non-existent item shows a Page Not Found message', async ({ page }) => {
  await page.goto('/XX00000016/00001');

  // Selecting by tag/text (rather than matching raw HTML) naturally ignores the inline style on
  // the <h1> -- that's presentation, not part of the message.
  await expect(page.locator('h1', { hasText: 'Page Not Found' })).toBeVisible();
  await expect(page.getByText('The resource you requested does not exist.')).toBeVisible();
});
