import { test, expect } from '@playwright/test';
import { hasCredentials, requiresLogon, username, password } from './auth';

// Always starts logged out, even on a private site where the other tests reuse auth.setup.ts's session
test.use({ storageState: { cookies: [], origins: [] } });

test('user can log in with valid credentials', async ({ page }) => {
  test.skip(!hasCredentials, 'SOBEKCM_TEST_USERNAME / SOBEKCM_TEST_PASSWORD not set -- copy .env.example to .env and fill them in for local runs, or set TestUsername/TestPassword in this site\'s SiteConfigs JSON');

  if (requiresLogon) {
    // A private (SSO) site's standard logon screen may go straight to the identity provider, so use the
    // unlinked local logon page, which never does
    await page.goto('/my/logon/local');
    await page.locator('#logon_username').fill(username!);
    await page.locator('#logon_password').fill(password!);
    await page.locator('#logon_submit').click();
  } else {
    await page.goto('/my/logon');

    await page.locator('#form_logon_term').click();
    await page.locator('#logon_username').fill(username!);
    await page.locator('#logon_password').fill(password!);
    await page.getByRole('button', { name: 'LOGIN' }).click();
  }

  // Some skins render more than one a[href*="my/logout"] (e.g. a hidden mobile-nav
  // duplicate alongside the visible one) -- .first() isn't guaranteed to land on the
  // visible one, so filter to :visible before taking the first match.
  await expect(page.locator('a[href*="my/logout"]:visible').first()).toBeVisible();
});
