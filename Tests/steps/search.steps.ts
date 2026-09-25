import { expect, type Page } from '@playwright/test';
import { When, Then } from './fixtures';
import { clickAndWaitForNavigation } from './common.steps';

// Every search box layout's "Go" button: basic, banner, and full-text
const GO_BUTTON = '#sbkBsav_SearchButton, #sbkFtsav_SearchButton, #sbkBhs_SearchArea_all button.sbk_GoButton';
const SEARCH_BOX = '#SobekHomeSearchBox, #SobekHomeBannerSearchBox';

// The sentence PagedResults_HtmlHelper writes above the results, whitespace-normalized
async function explanationText(page: Page): Promise<string> {
  // A normal results page has .sbkPrsw_ResultsExplanation; the no-results page puts the sentence
  // straight in .sbkPrsw_DescPanel (which on a normal page also wraps the whole toolbar)
  const explanation = page.locator('.sbkPrsw_ResultsExplanation');
  const text = (await explanation.count()) > 0
    ? await explanation.first().innerText()
    : await page.locator('.sbkPrsw_DescPanel').first().innerText();
  return text.replace(/\s+/g, ' ').trim();
}

// The "1 - 20 of 29 matching titles" text in the top results toolbar
function rangeText(page: Page) {
  return page.locator('#sbkPrsw_ButtonsTable td').filter({ hasText: /\d+ - \d+ / }).first();
}

// How many titles PagedResults_HtmlHelper puts on one results page
const RESULTS_PER_PAGE = 20;

// "1 - 20 of 125 matching titles" -> { from: 1, to: 20, total: 125 }, in any interface language:
// only the three numbers are read, so the same helper works on the translated pages
async function parseRange(page: Page) {
  const text = (await rangeText(page).innerText()).replace(/\s+/g, ' ').trim();
  const numbers = (text.match(/\d+/g) ?? []).map(Number);
  expect(numbers.length, `expected three numbers in the result range "${text}"`).toBe(3);
  const [from, to, total] = numbers;
  return { text, from, to, total };
}

// A site-wide record count is the testing site's own content, and changes whenever items are
// added to it, so scenarios write "{total}" where that count belongs and assert the wording and
// the paging arithmetic around it. In an explanation sentence the number still gets checked
// against a second source: the paging bar is written by separate code from the explanation.
const TOTAL_TOKEN = '{total}';

async function withReportedTotal(page: Page, expected: string): Promise<string> {
  if (!expected.includes(TOTAL_TOKEN)) return expected;
  const { total } = await parseRange(page);
  expect(total, 'total matching titles reported by the paging bar').toBeGreaterThan(0);
  return expected.split(TOTAL_TOKEN).join(String(total));
}

// One element per result title, whichever results viewer drew the page (brief, table, thumbnail)
const resultItemSelector = 'section.sbkBrv_SingleResult, tr[onclick^="window.location"], span[id^="sbkThumbnailSpan"]';

// ----- Running searches -----

When('I search for {string}', async ({ page }, term: string) => {
  await page.locator(SEARCH_BOX).first().fill(term);
  await clickAndWaitForNavigation(page, () => page.locator(GO_BUTTON).first().click());
});

When('I search for {string} by pressing Enter', async ({ page }, term: string) => {
  const box = page.locator(SEARCH_BOX).first();
  await box.fill(term);
  await clickAndWaitForNavigation(page, () => box.press('Enter'));
});

// Table columns: operator (blank for the first row, else "and" / "or" / "and not"), term, field
When('I run an advanced search for:', async ({ page }, table) => {
  const rows = table.hashes() as { operator: string; term: string; field: string }[];
  for (const [i, row] of rows.entries()) {
    const n = i + 1;
    if (n > 1 && row.operator) await page.locator(`#andOrNotBox${n - 1}`).selectOption({ label: row.operator });
    await page.locator(`#Textbox${n}`).fill(row.term);
    if (row.field) await page.locator(`#Dropdownlist${n}`).selectOption({ label: row.field });
  }
  await clickAndWaitForNavigation(page, () => page.locator('#searchButton').click());
});

When('I choose the {string} search precision', async ({ page }, label: string) => {
  await page.getByLabel(label).check();
});

When('I run the advanced search', async ({ page }) => {
  await clickAndWaitForNavigation(page, () => page.locator('#searchButton').click());
});

When('I type {string} into advanced search row {int}', async ({ page }, term: string, n: number) => {
  await page.locator(`#Textbox${n}`).fill(term);
});

// ----- Results summary -----

Then('the search should report {int} matching record(s)', async ({ page }, count: number) => {
  expect(await explanationText(page)).toMatch(new RegExp(`resulted in ${count} matching records?\\.`));
});

Then('the search should report no matching records', async ({ page }) => {
  expect(await explanationText(page)).toContain('resulted in no matching records.');
});

Then('the search should report some matching records', async ({ page }) => {
  expect(await explanationText(page)).toMatch(/resulted in (\d+|one) matching records?\./);
});

Then('the search explanation should read {string}', async ({ page }, expected: string) => {
  expect(await explanationText(page)).toBe(await withReportedTotal(page, expected));
});

Then('the search explanation should contain {string}', async ({ page }, expected: string) => {
  expect(await explanationText(page)).toContain(expected);
});

Then('the result range should read {string}', async ({ page }, expected: string) => {
  await expect(rangeText(page)).toHaveText(await withReportedTotal(page, expected));
});

// Paging arithmetic, checked against the total the page itself reports: page N starts at title
// (N-1)*20+1 and runs to title N*20, or to the last title if that page is short
Then('the result range should cover results page {int}', async ({ page }, pageNumber: number) => {
  const { text, from, to, total } = await parseRange(page);
  expect(from, `first title shown, from "${text}"`).toBe((pageNumber - 1) * RESULTS_PER_PAGE + 1);
  expect(to, `last title shown, from "${text}"`).toBe(Math.min(pageNumber * RESULTS_PER_PAGE, total));
});

Then('the result range should cover the last results page', async ({ page }) => {
  const { text, from, to, total } = await parseRange(page);
  expect(to, `last title shown, from "${text}"`).toBe(total);
  expect((from - 1) % RESULTS_PER_PAGE, `first title shown, from "${text}"`).toBe(0);
  expect(to - from + 1, `titles left on the last page, from "${text}"`).toBeLessThanOrEqual(RESULTS_PER_PAGE);
});

Then('the bottom paging bar should show the result range', async ({ page }) => {
  const top = (await rangeText(page).innerText()).trim();
  await expect(page.locator('div.sbkPrsw_ResultsNavBar').last()).toContainText(top);
});

Then('the page should list {int} result(s)', async ({ page }, count: number) => {
  await expect(page.locator('#sbkPrsw_ResultsOuterTable').locator(resultItemSelector)).toHaveCount(count);
});

Then('the page should list one result for every title in the range', async ({ page }) => {
  const { from, to } = await parseRange(page);
  await expect(page.locator('#sbkPrsw_ResultsOuterTable').locator(resultItemSelector)).toHaveCount(to - from + 1);
});

Then('every result should link to an item page', async ({ page }) => {
  const hrefs = await page.locator('#sbkPrsw_ResultsOuterTable a[href]').evaluateAll((links) =>
    links.map((a) => new URL((a as HTMLAnchorElement).href).pathname));
  const itemLinks = hrefs.filter((p) => /^\/[A-Za-z]{2}[A-Za-z0-9]{0,5}\d+\/\d{5}\/?$/.test(p));
  expect(itemLinks.length).toBeGreaterThan(0);
});

Then('the results should be shown in the {string} view', async ({ page }, view: string) => {
  const selectors: Record<string, string> = {
    brief: 'section.sbkBrv_Results',
    table: '.SobekTableSortText',
    thumbnail: 'span[id^="sbkThumbnailSpan"]',
  };
  const selector = selectors[view.toLowerCase()];
  expect(selector, `unknown results view "${view}"`).toBeTruthy();
  await expect(page.locator(selector).first()).toBeVisible();
  await expect(page.locator('img.sbkPrsw_ViewIconButtonCurrent')).toHaveAttribute('alt', new RegExp(view, 'i'));
});

Then('the result titles should be in alphabetical order', async ({ page }) => {
  const titles = (await page.locator('.briefResultsTitle a').allInnerTexts()).map((t) => t.trim());
  expect(titles.length).toBeGreaterThan(1);
  // Case-insensitive ordinal order, which is what the search index sorts by: "(Egypt)" sorts
  // before "Across" because "(" comes before letters
  const key = (t: string) => t.toLowerCase();
  const sorted = [...titles].sort((a, b) => (key(a) < key(b) ? -1 : key(a) > key(b) ? 1 : 0));
  expect(titles).toEqual(sorted);
});

// ----- Sorting and paging -----

Then('the sort options should be {string}', async ({ page }, list: string) => {
  const options = (await page.locator('#sorter_input option').allInnerTexts()).map((t) => t.trim());
  expect(options).toEqual(list.split(',').map((s) => s.trim()));
});

When('I sort the results by {string}', async ({ page }, label: string) => {
  await clickAndWaitForNavigation(page, () => page.locator('#sorter_input').selectOption({ label }).then(() => {}));
});

When('I go to the {string} results page', async ({ page }, which: string) => {
  const title = `${which[0].toUpperCase()}${which.slice(1).toLowerCase()} Page`;
  const button = page.locator(`#sbkPrsw_ButtonsTable button[title="${title}"]`);
  await clickAndWaitForNavigation(page, () => button.click());
});

Then('there should be a {string} page button', async ({ page }, which: string) => {
  await expect(page.locator(`#sbkPrsw_ButtonsTable button[title="${cap(which)} Page"]`)).toHaveCount(1);
});

Then('there should be no {string} page button', async ({ page }, which: string) => {
  await expect(page.locator(`#sbkPrsw_ButtonsTable button[title="${cap(which)} Page"]`)).toHaveCount(0);
});

Then('there should be no paging buttons', async ({ page }) => {
  await expect(page.locator('button.sbkPrsw_RoundButton')).toHaveCount(0);
});

function cap(word: string) {
  return word[0].toUpperCase() + word.slice(1).toLowerCase();
}

// ----- Facets -----

function facetBox(page: Page, group: string) {
  return page.locator('.sbkPrsw_FacetBoxTitle', { hasText: new RegExp(`^${escapeRegExp(group)}$`) })
    .locator('xpath=following-sibling::div[contains(@class,"sbkPrsw_FacetBox")][1]');
}

function escapeRegExp(s: string) {
  return s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

Then('the facet {string} should offer {string} with {int} result(s)', async ({ page }, group: string, value: string, count: number) => {
  const box = facetBox(page, group);
  await expect(box).toContainText(`${value} ( ${count} )`);
});

When('I narrow the results by {string} {string}', async ({ page }, group: string, value: string) => {
  const link = facetBox(page, group).getByRole('link', { name: value, exact: true });
  await clickAndWaitForNavigation(page, () => link.click());
});

Then('the facet {string} should list {int} values', async ({ page }, group: string, count: number) => {
  await expect(facetBox(page, group).locator('a[onclick*="add_facet"]')).toHaveCount(count);
});

Then('the facet {string} should list more than {int} values', async ({ page }, group: string, count: number) => {
  expect(await facetBox(page, group).locator('a[onclick*="add_facet"]').count()).toBeGreaterThan(count);
});

When('I click {string} in the {string} facet', async ({ page }, text: string, group: string) => {
  // Show More / Show Less / resort post the itemNavForm back
  // Labels carry arrows: "Show More >>" and "<< Show Less"
  const link = facetBox(page, group).locator('.sbkPrsw_ShowHideFacets a', { hasText: text });
  await Promise.all([page.waitForResponse((r) => r.request().method() === 'POST'), link.click()]);
  await page.waitForLoadState('domcontentloaded');
});

// ----- Search terms -----

When('I remove the search term {string}', async ({ page }, term: string) => {
  const termDiv = page.locator('.sbkPrsw_SearchTerm', { hasText: `'${term}'` });
  await clickAndWaitForNavigation(page, () => termDiv.locator('a[title="Click to remove this search term"]').click());
});
