import { expect, type Page } from '@playwright/test';
import { Given, When, Then } from './fixtures';
import { region } from '../support/regions';

// Path + query of the current page, ignoring host: generated links carry the site's configured
// base URL, which is the bare IP inside the Terraform test environment.
export function currentPathAndQuery(page: Page): string {
  const url = new URL(page.url());
  return decodeURIComponent(url.pathname + url.search);
}

// Waits for a click that navigates via window.location (most SobekCM buttons do, not form submits)
export async function clickAndWaitForNavigation(page: Page, click: () => Promise<void>) {
  const before = page.url();
  await Promise.all([page.waitForURL((url) => url.toString() !== before), click()]);
  await page.waitForLoadState('domcontentloaded');
}

// ----- Navigation -----

Given('I open {string}', async ({ page, world }, path: string) => {
  world.response = await page.goto(path);
});

Given('I open the {string} collection', async ({ page, world }, code: string) => {
  world.response = await page.goto(`/${code}`);
});

When('I click the {string} link', async ({ page }, name: string) => {
  await clickAndWaitForNavigation(page, () => page.getByRole('link', { name, exact: true }).first().click());
});

When('I click the {string} link in the {string}', async ({ page }, name: string, regionName: string) => {
  const link = page.locator(region(regionName)).getByRole('link', { name, exact: true }).first();
  await clickAndWaitForNavigation(page, () => link.click());
});

When('I click the {string} region', async ({ page }, regionName: string) => {
  await clickAndWaitForNavigation(page, () => page.locator(region(regionName)).first().click());
});

When('I click the {string} region without leaving the page', async ({ page }, regionName: string) => {
  await page.locator(region(regionName)).first().click();
});

// By text, not role: several old SobekCM controls are <a onclick> with no href, which has no link role
When('I click the {string} link without leaving the page', async ({ page }, name: string) => {
  await page.locator('a', { hasText: new RegExp(`^\\s*${name}\\s*$`) }).first().click();
});

When('I reload the page', async ({ page, world }) => {
  world.response = await page.reload();
});

// ----- Status, title and URL -----

Then('the response status should be {int}', async ({ world }, status: number) => {
  expect(world.response, 'no page response was captured').toBeTruthy();
  expect(world.response!.status()).toBe(status);
});

Then('the page title should be {string}', async ({ page }, title: string) => {
  await expect(page).toHaveTitle(title);
});

Then('the page title should contain {string}', async ({ page }, text: string) => {
  expect(await page.title()).toContain(text);
});

Then('I should be on {string}', async ({ page }, expected: string) => {
  expect(currentPathAndQuery(page)).toBe(expected);
});

Then('the URL should contain {string}', async ({ page }, text: string) => {
  expect(currentPathAndQuery(page)).toContain(text);
});

Then('the URL should not contain {string}', async ({ page }, text: string) => {
  expect(currentPathAndQuery(page)).not.toContain(text);
});

// ----- Page content -----

Then('I should see {string}', async ({ page }, text: string) => {
  await expect(page.locator('body')).toContainText(text);
});

Then('I should not see {string}', async ({ page }, text: string) => {
  await expect(page.locator('body')).not.toContainText(text);
});

Then('the {string} link should be visible', async ({ page }, name: string) => {
  await expect(page.getByRole('link', { name, exact: true }).first()).toBeVisible();
});

Then('the {string} should be visible', async ({ page }, regionName: string) => {
  await expect(page.locator(region(regionName)).first()).toBeVisible();
});

Then('the page HTML should not contain {string}', async ({ page }, text: string) => {
  expect(await page.content()).not.toContain(text);
});

Then('the {string} should be present', async ({ page }, regionName: string) => {
  await expect(page.locator(region(regionName))).not.toHaveCount(0);
});

Then('the {string} should not be present', async ({ page }, regionName: string) => {
  await expect(page.locator(region(regionName))).toHaveCount(0);
});

Then('the {string} should contain {string}', async ({ page }, regionName: string, text: string) => {
  await expect(page.locator(region(regionName)).first()).toContainText(text);
});

Then('the {string} should not contain {string}', async ({ page }, regionName: string, text: string) => {
  await expect(page.locator(region(regionName)).first()).not.toContainText(text);
});

Then('the {string} should have focus', async ({ page }, regionName: string) => {
  await expect(page.locator(region(regionName)).first()).toBeFocused();
});

Then('the {string} should contain these links:', async ({ page }, regionName: string, table) => {
  const container = page.locator(region(regionName)).first();
  for (const row of table.hashes() as { text: string; path: string }[]) {
    // Not a visibility check: superfish submenu links stay hidden until hovered
    const link = container.getByRole('link', { name: row.text, exact: true, includeHidden: true }).first();
    await expect(link, `link "${row.text}"`).toHaveCount(1);
    const href = await link.getAttribute('href');
    expect(new URL(href!, page.url()).pathname, `href of "${row.text}"`).toBe(row.path);
  }
});

Then('the {string} should list exactly these links:', async ({ page }, regionName: string, table) => {
  const container = page.locator(region(regionName)).first();
  const expected = (table.raw() as string[][]).map((r) => r[0]);
  // textContent, not innerText: hidden submenu links have empty innerText
  const actual = (await container.locator('a').allTextContents()).map((t) => t.trim()).filter(Boolean);
  expect([...new Set(actual)].sort()).toEqual([...expected].sort());
});

// ----- Page health -----

Then('the page should have no script errors', async ({ page, world }) => {
  // Give document-ready handlers a moment to run
  await page.waitForLoadState('load');
  expect(world.pageErrors).toEqual([]);
});

// Catches header/footer/home-text directives the server forgot to replace, e.g. <%BANNER%> or
// [%WithinInstanceCount%]. Script bodies are skipped on purpose: the results page legitimately
// ships a JavaScript URL template containing <%CODE%> and <%VALUE%>.
Then('the page should not contain unreplaced template tokens', async ({ page }) => {
  const leftovers = await page.evaluate(() => {
    const tokenPattern = /<%[^%]{1,40}%>|\[%[A-Za-z]{2,40}%\]|\{0\}/g;
    const found: string[] = [];
    const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
    for (let node = walker.nextNode(); node; node = walker.nextNode()) {
      const parent = node.parentElement?.tagName;
      if (parent === 'SCRIPT' || parent === 'STYLE') continue;
      found.push(...(node.textContent!.match(tokenPattern) ?? []));
    }
    for (const el of Array.from(document.body.querySelectorAll('[href],[src],[alt],[title],[style]'))) {
      for (const attr of ['href', 'src', 'alt', 'title', 'style']) {
        found.push(...(el.getAttribute(attr)?.match(tokenPattern) ?? []));
      }
    }
    return [...new Set(found)];
  });
  expect(leftovers).toEqual([]);
});

Then('every image in the {string} should load', async ({ page }, regionName: string) => {
  const images = page.locator(`${region(regionName)} img`);
  const count = await images.count();
  expect(count).toBeGreaterThan(0);
  for (let i = 0; i < count; i++) {
    const img = images.nth(i);
    await img.scrollIntoViewIfNeeded();
    await expect
      .poll(() => img.evaluate((el: HTMLImageElement) => el.complete && el.naturalWidth > 0), {
        message: `image ${await img.getAttribute('src')} should load`,
      })
      .toBe(true);
  }
});
