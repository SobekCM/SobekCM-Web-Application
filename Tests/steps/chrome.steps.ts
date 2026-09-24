import { expect } from '@playwright/test';
import { Given, When, Then } from './fixtures';
import { clickAndWaitForNavigation } from './common.steps';

// ----- Header / footer -----

Then('the breadcrumbs should read {string}', async ({ page }, expected: string) => {
  const text = (await page.locator('#instheaderbottomleft').innerText()).replace(/\s+/g, ' ').trim();
  expect(text).toBe(expected);
});

Then('there should be no breadcrumbs', async ({ page }) => {
  const text = (await page.locator('#instheaderbottomleft').innerText()).trim();
  expect(text).toBe('');
});

Then('the footer should offer these languages: {string}', async ({ page }, list: string) => {
  const items = (await page.locator('#instfooternav li').allInnerTexts()).map((t) => t.trim());
  expect(items).toEqual(list.split(',').map((s) => s.trim()));
});

Then('the current language {string} should not be a link', async ({ page }, name: string) => {
  const item = page.locator('#instfooternav li', { hasText: new RegExp(`^${name}$`) });
  await expect(item).toHaveCount(1);
  await expect(item.locator('a')).toHaveCount(0);
});

When('I switch the language to {string}', async ({ page }, name: string) => {
  const link = page.locator('#instfooternav').getByRole('link', { name, exact: true });
  await clickAndWaitForNavigation(page, () => link.click());
});

Then('the page language should be {string}', async ({ page }, code: string) => {
  await expect(page.locator('html')).toHaveAttribute('lang', code);
});

Then('the footer should show the expected software version', async ({ page, $test }) => {
  const expected = process.env.SOBEKCM_EXPECTED_VERSION;
  $test.skip(!expected, 'Set SOBEKCM_EXPECTED_VERSION to the release candidate version to run this check');
  await expect(page.locator('footer')).toContainText(expected!);
});

Then('the footer should show the current year', async ({ page }) => {
  await expect(page.locator('footer')).toContainText(String(new Date().getFullYear()));
});

Then('the account link should lead to the logon page', async ({ page }) => {
  const link = page.locator('#instheaderbottomright a[href*="/my/logon"]');
  await expect(link).toHaveCount(1);
});

Then('the skin stylesheet should load', async ({ page, request }) => {
  const hrefs = await page.locator('link[rel=stylesheet][href*="/design/skins/"]').evaluateAll((links) =>
    links.map((l) => (l as HTMLLinkElement).href));
  expect(hrefs.length, 'no skin stylesheet link in the page head').toBeGreaterThan(0);
  for (const href of hrefs) {
    expect((await request.get(href)).status(), href).toBe(200);
  }
});

Then('no skin or design file should fail to load', async ({ page }) => {
  const failures: string[] = [];
  page.on('response', (r) => {
    if (r.status() >= 400 && /\/design\//i.test(r.url())) failures.push(`${r.status()} ${r.url()}`);
  });
  await page.reload();
  await page.waitForLoadState('load');
  expect(failures).toEqual([]);
});

Then('the skip link should point at the main content', async ({ page }) => {
  await expect(page.locator('#skip-to-main-content a[href="#main-content"]')).toHaveCount(1);
  await expect(page.locator('#main-content')).toHaveCount(1);
});

When('I click the {string} contact form button', async ({ page }, title: string) => {
  const button = page.locator(`#sbkChsw_ButtonDiv button[title="${title.toUpperCase()}"]`);
  await clickAndWaitForNavigation(page, () => button.click());
});

// ----- Main menu -----

function topLevelMenuItems(page: import('@playwright/test').Page) {
  return page.locator('#sbkAgm_Menu > li');
}

Then('the main menu should include {string}', async ({ page }, text: string) => {
  await expect(topLevelMenuItems(page).filter({ has: page.locator(':scope > a', { hasText: text }) })).toHaveCount(1);
});

Then('the main menu should not include {string}', async ({ page }, text: string) => {
  await expect(topLevelMenuItems(page).filter({ has: page.locator(':scope > a', { hasText: text }) })).toHaveCount(0);
});

Then('the {string} main menu item should be selected', async ({ page }, text: string) => {
  const selected = page.locator('#sbkAgm_Menu > li.selected-sf-menu-item-link');
  await expect(selected).toHaveCount(1);
  await expect(selected).toContainText(text);
});

When('I hover over the {string} main menu item', async ({ page }, text: string) => {
  await topLevelMenuItems(page).locator(':scope > a', { hasText: text }).first().hover();
});

Then('the anonymous visitor should not see any admin menu items', async ({ page }) => {
  await expect(page.locator('#sbkUsm_MenuBar, #sbkUsm_Admin, #sbkAgm_MyCollections, #sbkAgm_HomePersonalized')).toHaveCount(0);
});

// ----- Maps -----

async function googleMapsEnabled(page: import('@playwright/test').Page) {
  const html = await (await page.request.get('/aerials/geography')).text();
  return !html.includes('Google Maps are not enabled');
}

Given('Google Maps is enabled on this site', async ({ page, $test }) => {
  $test.skip(!(await googleMapsEnabled(page)), 'No Google Maps API key is configured on this site');
});

Given('Google Maps is not enabled on this site', async ({ page, $test }) => {
  $test.skip(await googleMapsEnabled(page), 'A Google Maps API key is configured on this site');
});
