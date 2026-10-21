import { test as setup, expect } from '@playwright/test';
import { AUTH_FILE, requiresLogon, hasCredentials, username, password } from './auth';

// Only does anything on a private site (RequireLogon="true" in sobekcm_authentication.config), where every
// other smoke test would otherwise just land on the logon screen -- or, with AutoSignInProvider set, on the
// identity provider. Logs on once through the unlinked my/logon/local page, which is never sent on to SSO,
// and saves the session for the other tests to reuse.
setup('log on to a private site', async ({ page }) => {
  setup.skip(!requiresLogon, 'SOBEKCM_REQUIRES_LOGON not set -- this site is public');
  setup.skip(!hasCredentials, 'SOBEKCM_REQUIRES_LOGON is set, but SOBEKCM_TEST_USERNAME / SOBEKCM_TEST_PASSWORD are not');

  await page.goto('/my/logon/local');
  await page.locator('#logon_username').fill(username!);
  await page.locator('#logon_password').fill(password!);
  await page.locator('#logon_submit').click();

  await expect(page.locator('a[href*="my/logout"]:visible').first()).toBeVisible();
  await page.context().storageState({ path: AUTH_FILE });
});
