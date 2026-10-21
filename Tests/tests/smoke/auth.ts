// Upgrade-CustomerSite.ps1 sends this in place of a real value when a site's config has
// no TestUsername/TestPassword -- NOT an empty string. An empty string doesn't reliably
// survive the extra process hop into this test's worker process on Windows (it can look
// "absent" by the time it gets here rather than "present but empty"), which lets dotenv
// silently fill the gap from a developer's local .env and run this test against the
// wrong account entirely. A non-empty sentinel can't fall into that ambiguity.
export const NO_CREDENTIALS = '__NO_TEST_CREDENTIALS__';
export const username = process.env.SOBEKCM_TEST_USERNAME;
export const password = process.env.SOBEKCM_TEST_PASSWORD;
export const hasCredentials = !!username && !!password && username !== NO_CREDENTIALS && password !== NO_CREDENTIALS;

// "true" for a private site (RequireLogon="true" in sobekcm_authentication.config): every test then runs
// logged on, using the session auth.setup.ts saves here
export const requiresLogon = (process.env.SOBEKCM_REQUIRES_LOGON ?? '').toLowerCase() === 'true';
export const AUTH_FILE = 'playwright/.auth/user.json';
