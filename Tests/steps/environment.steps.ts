import { Before } from './fixtures';

// Scenarios in the state-altering stages -- @content, @structure and @site-settings (see playwright.config.ts) --
// change the site they run against: they submit items, delete collections, clear settings. They only ever run in the
// disposable e2e environment, which sets E2E_DISPOSABLE=true; anywhere else (a local run against the testing site,
// say) they skip before their first step, so nothing real is changed by accident. No step has to remember this --
// the stage tag alone is enough.
Before('@content or @structure or @site-settings', async ({ $testInfo }) => {
  $testInfo.skip(process.env.E2E_DISPOSABLE !== 'true',
    'Changes the site it runs against, so it only runs in the disposable e2e environment (E2E_DISPOSABLE=true)');
});
