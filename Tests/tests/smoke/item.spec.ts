import { test, expect } from '@playwright/test';

// Upgrade-CustomerSite.ps1 sends this in place of a real value when a site's config has no
// TestItem -- NOT an empty string (see the matching comment in login.spec.ts for why an empty
// string doesn't reliably survive into this test's worker process on Windows).
const NO_TEST_ITEM = '__NO_TEST_ITEM__';
const testItem = process.env.SOBEKCM_TEST_ITEM;
const hasTestItem = !!testItem && testItem !== NO_TEST_ITEM;

test('item citation page renders title and a working thumbnail', async ({ page, request, baseURL }) => {
  test.skip(!hasTestItem, 'SOBEKCM_TEST_ITEM not set -- set TestItem (a bibid/vid this site actually has, e.g. "DR00000016/00001") in this site\'s SiteConfigs JSON');

  const response = await page.goto(`/${testItem}/citation`);
  expect(response?.status()).toBe(200);

  // Every site's TestItem points at a different real item with a different real title, so we
  // can only confirm a title actually rendered -- not what it says.
  // Selecting by class + itemprop (rather than matching raw HTML) naturally ignores the inline
  // style="margin-left:230px;" on the <dd> -- that's presentation, not part of the title.
  const title = page.locator('dd.sbk_CivTITLE_Element span[itemprop="name"]');
  await expect(title).toBeVisible();
  const titleText = await title.textContent();
  expect(titleText?.trim().length ?? 0).toBeGreaterThan(0);

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
