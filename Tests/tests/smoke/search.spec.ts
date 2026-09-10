import { test, expect } from '@playwright/test';

test('basic search from home page GO button returns a results page', async ({ page, baseURL }) => {
  await page.goto('/');

  // Two supported home-search layouts exist: the standard search box
  // (#SobekHomeSearchBox / #sbkBsav_SearchButton) and the banner variant
  // (#SobekHomeBannerSearchBox, whose Go button has no id -- just class sbk_GoButton
  // inside #sbkBhs_SearchArea_all), used e.g. on opennj.net. A site renders one or the
  // other, never both, so matching either selector finds exactly one real box/button.
  await page.locator('#SobekHomeSearchBox, #SobekHomeBannerSearchBox').fill('a');
  await page.locator('#sbkBsav_SearchButton, #sbkBhs_SearchArea_all button.sbk_GoButton').click();

  await page.waitForURL(/\/results\//);

  const finalHost = new URL(page.url()).hostname;
  const expectedHost = new URL(baseURL!).hostname;
  expect(finalHost).toBe(expectedHost);
  expect(finalHost).not.toContain('sobekdigital.com');

  await expect(page).toHaveTitle(/Search Results/);

  // Different sites render results with different display components -- e.g. the brief
  // view's .sbkBrv_SingleResult divs vs. a paged/thumbnail table view -- so there's no
  // per-result CSS class guaranteed to exist across all of them. Every format still links
  // each result to its own item page as /bibid/vid though, so count links matching that
  // shape instead of a specific class name. Scoped to #sbkPrsw_ResultsOuterTable (written
  // by PagedResults_HtmlHelper.cs, the shared helper behind every results page regardless
  // of viewer/skin) so a header/footer link that happens to match the /bibid/vid shape --
  // e.g. a "featured item" spotlight -- can't produce a false pass. NOT the same as
  // sbkAhs_ResultsPageContainer, which Aggregation_HtmlSubwriter only writes for a
  // DataSet_Browse-type collection viewer -- a different code path than the basic search
  // this test drives.
  const resultsContainer = page.locator('#sbkPrsw_ResultsOuterTable');
  await expect(resultsContainer).toBeVisible();
  // A BibID is exactly 10 characters: 2 letters, then up to 5 more letters/digits, then
  // digits to fill out the rest. A VID is always 5 digits, e.g. "00001".
  const itemLinkCount = await resultsContainer.locator('a[href]').evaluateAll((links) => {
    const isBibid = (s: string) => s.length === 10 && /^[A-Za-z]{2}[A-Za-z0-9]{0,5}\d+$/.test(s);
    const isVid = (s: string) => /^\d{5}$/.test(s);
    return links.filter((a) => {
      try {
        const url = new URL((a as HTMLAnchorElement).href);
        if (url.hostname !== location.hostname) return false;
        const [bibid, vid] = url.pathname.split('/').filter(Boolean);
        return !!bibid && !!vid && isBibid(bibid) && isVid(vid);
      }
      catch {
        return false;
      }
    }).length;
  });
  expect(itemLinkCount).toBeGreaterThan(0);
});
