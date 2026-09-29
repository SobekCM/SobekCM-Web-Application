import { expect } from '@playwright/test';
import { Given, When, Then } from './fixtures';
import { clickAndWaitForNavigation } from './common.steps';
import { region } from '../support/regions';
import { getItemBrief } from '../support/engineApi';

// ----- Fixture verification, via the engine's own item data (not the rendered page) -----
// These fail fast, before a browser page even loads, if the test data no longer matches what the
// scenario assumes - telling "the data changed" apart from "the code broke".

Given('the item {string} is marked Dark', async ({ request }, bibidVid: string) => {
  const brief = await getItemBrief(request, bibidVid);
  expect(brief.behaviors.dark ?? false, `expected ${bibidVid} to be Dark per the engine's item data`).toBe(true);
});

Given('the item {string} is marked Private', async ({ request }, bibidVid: string) => {
  const brief = await getItemBrief(request, bibidVid);
  expect(brief.behaviors.ipRestriction ?? 0, `expected ${bibidVid} to be Private (a negative ipRestriction) per the engine's item data`).toBeLessThan(0);
});

Given('the item {string} is full-text searchable', async ({ request }, bibidVid: string) => {
  const brief = await getItemBrief(request, bibidVid);
  expect(brief.behaviors.textSearchable ?? false, `expected ${bibidVid} to be full-text searchable per the engine's item data`).toBe(true);
});

Given('the item {string} has a {string} file', async ({ request }, bibidVid: string, extension: string) => {
  const brief = await getItemBrief(request, bibidVid);
  expect(brief.web?.fileExtensions ?? [], `expected ${bibidVid} to have a .${extension} file per the engine's item data`).toContain(extension);
});

Given('the item {string} has geographic data', async ({ request }, bibidVid: string) => {
  const brief = await getItemBrief(request, bibidVid);
  const count = (brief.geospatial?.points?.length ?? 0) + (brief.geospatial?.polygons?.length ?? 0);
  expect(count, `expected ${bibidVid} to have points or areas per the engine's item data`).toBeGreaterThan(0);
});

// The item menu marks the current view either as a plain, link-less tab (a view with no sub-menu) or as
// the link heading the sub-menu it sits in (StandardItemMenuProvider)
Then('the selected item tab should be {string}', async ({ page }, label: string) => {
  const selected = page.locator(region('item menu')).locator('li.selected-sf-menu-item, li.selected-sf-menu-item-link > a').first();
  await expect(selected).toHaveText(label);
});

// Text_Search_ItemViewer's own full-text-within-one-item search box (distinct from the
// aggregation/site-wide search boxes - different ids, different underlying viewer)
When('I search this item for {string}', async ({ page }, term: string) => {
  await page.locator('#searchTextBox').fill(term);
  await clickAndWaitForNavigation(page, () => page.locator('button[title="Search this document"]').click());
});

Then('the current table-of-contents page should be {string}', async ({ page }, label: string) => {
  await expect(page.locator(region('table of contents')).locator('span.sbkIsw_SelectedTocTreeViewItem')).toHaveText(label);
});

// MultiVolumes_ItemViewer's tree: native <details>/<summary>, one per module/branch. Clicking a
// currently-collapsed <summary> expands it (plain browser behavior, no page reload) to reveal its
// own volume links.
When('I expand the {string} branch in the {string}', async ({ page }, label: string, regionName: string) => {
  await page.locator(region(regionName)).locator('summary', { hasText: label }).click();
});

Then('the current volume in the tree should not be a link', async ({ page }) => {
  const tagName = await page.locator(region('current volume in tree')).evaluate((el) => el.tagName);
  expect(tagName).not.toBe('A');
});
