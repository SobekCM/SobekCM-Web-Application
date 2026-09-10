import { test, expect } from '@playwright/test';

// Upgrade-CustomerSite.ps1 sends this in place of a real value when a site's config has
// no TestUsername/TestPassword -- NOT an empty string. An empty string doesn't reliably
// survive the extra process hop into this test's worker process on Windows (it can look
// "absent" by the time it gets here rather than "present but empty"), which lets dotenv
// silently fill the gap from a developer's local .env and run this test against the
// wrong account entirely. A non-empty sentinel can't fall into that ambiguity.
const NO_CREDENTIALS = '__NO_TEST_CREDENTIALS__';
const username = process.env.SOBEKCM_TEST_USERNAME;
const password = process.env.SOBEKCM_TEST_PASSWORD;
const hasCredentials = !!username && !!password && username !== NO_CREDENTIALS && password !== NO_CREDENTIALS;

test('user can log in with valid credentials', async ({ page }) => {
  test.skip(!hasCredentials, 'SOBEKCM_TEST_USERNAME / SOBEKCM_TEST_PASSWORD not set -- copy .env.example to .env and fill them in for local runs, or set TestUsername/TestPassword in this site\'s SiteConfigs JSON');

  await page.goto('/my/logon');

  await page.locator('#form_logon_term').click();
  await page.locator('#logon_username').fill(username!);
  await page.locator('#logon_password').fill(password!);
  await page.getByRole('button', { name: 'LOGIN' }).click();

  // Some skins render more than one a[href*="my/logout"] (e.g. a hidden mobile-nav
  // duplicate alongside the visible one) -- .first() isn't guaranteed to land on the
  // visible one, so filter to :visible before taking the first match.
  await expect(page.locator('a[href*="my/logout"]:visible').first()).toBeVisible();
});
