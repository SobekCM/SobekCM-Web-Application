import { expect } from '@playwright/test';
import { When, Then } from './fixtures';
import { clickAndWaitForNavigation } from './common.steps';
import { region } from '../support/regions';

// Text_Search_ItemViewer's own full-text-within-one-item search box (distinct from the
// aggregation/site-wide search boxes - different ids, different underlying viewer)
When('I search this item for {string}', async ({ page }, term: string) => {
  await page.locator('#searchTextBox').fill(term);
  await clickAndWaitForNavigation(page, () => page.locator('button[title="Search this document"]').click());
});

Then('the current table-of-contents page should be {string}', async ({ page }, label: string) => {
  await expect(page.locator(region('table of contents')).locator('span.sbkIsw_SelectedTocTreeViewItem')).toHaveText(label);
});
