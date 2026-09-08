import { test, expect } from '@playwright/test';

// TEMPORARY: exact copy of item.spec.ts pointed at a BibID that doesn't exist, just to see this
// fail for real on a run. Follow-up (next round): replace this with an actual "item not found"
// test that asserts on whatever the app's real not-found behavior is (status code, message, etc)
// instead of expecting the same title/thumbnail as a real item.
test('item citation page renders title and a working thumbnail', async ({ page, request, baseURL }) => {
  const response = await page.goto('/XX00000016/00001');
  expect(response?.status()).toBe(200);

  // Selecting by class + itemprop (rather than matching raw HTML) naturally ignores the inline
  // style="margin-left:230px;" on the <dd> -- that's presentation, not part of the title.
  const title = page.locator('dd.sbk_CivTITLE_Element span[itemprop="name"]');
  await expect(title).toHaveText('(18) Palestine ancienne et moderne.');

  const thumbnail = page.locator('#Sbk_CivThumbnailDiv img#Sbk_CivThumbnailImg');
  await expect(thumbnail).toBeVisible();

  const thumbnailSrc = await thumbnail.getAttribute('src');
  expect(thumbnailSrc).toBeTruthy();

  // The thumbnail should be served from this same ephemeral environment (its own Image Server
  // URL setting), not hardcoded back to the production testing.sobeklibrary.com domain.
  const thumbnailHost = new URL(thumbnailSrc!, baseURL).hostname;
  const expectedHost = new URL(baseURL!).hostname;
  expect(thumbnailHost).toBe(expectedHost);

  // Actually fetch the thumbnail rather than just checking the <img> tag exists -- this is the
  // one thing that catches RestoreLocalFileCache silently writing zero-byte/missing local files
  // while still reporting success (see Hybrid_FileSystem.DownloadFile).
  const imageResponse = await request.get(thumbnailSrc!);
  expect(imageResponse.status()).toBe(200);
  expect(imageResponse.headers()['content-type']).toContain('image');
  expect((await imageResponse.body()).length).toBeGreaterThan(0);
});
