import { defineConfig } from '@playwright/test';
import { defineBddProject } from 'playwright-bdd';
import 'dotenv/config';

// Projects:
//   smoke - the plain Playwright specs under ./tests (also run on their own by playwright.smoke.config.ts)
//   the BDD stages - the Gherkin acceptance suite under ./features, compiled into .features-gen by `bddgen`,
//   split by tag into stages ordered by how far their changes reach:
//     public          anonymous and read-only -- everything without a stage tag
//     logon-readonly  @logon-readonly: each role logs on and only looks
//     content         @content: submitting items, editing metadata, attaching files
//     structure       @structure: creating or deleting collections, changing collection settings
//     site-settings   @site-settings: system-wide settings (e.g. clearing the Google Maps key)
// The e2e pipeline runs the stages one after another, one `playwright test --project=<stage>` step each, so a
// later stage only starts once the earlier one has finished changing (or not changing) the site, and every stage
// reports even when an earlier one failed. Scenarios in the state-altering stages (content, structure,
// site-settings) skip themselves unless E2E_DISPOSABLE=true -- see steps/environment.steps.ts -- so running the
// suite against a real site never changes it.
// `npm test` runs bddgen first; a bare `npx playwright test` would run stale (or no) generated BDD tests.

// Also in steps/environment.steps.ts, whose guard hook skips these outside the disposable environment
const STATE_ALTERING_TAGS = '@content or @structure or @site-settings';
const STAGE_TAGS = `@logon-readonly or ${STATE_ALTERING_TAGS}`;

// With PLAYWRIGHT_BLOB_NAME set (the e2e pipeline sets it to the stage's name), each run writes a blob report
// instead of its own HTML and JUnit output; the pipeline merges every stage's blob into one HTML report and one
// JUnit file afterwards (npm run merge-reports, see playwright.merge.config.ts). Otherwise each run of the stages
// would overwrite the last. Each stage gets its own blob folder, because the blob reporter empties its folder at
// the start of every run.
const blobName = process.env.PLAYWRIGHT_BLOB_NAME;
const reporter = blobName
  ? [['list'], ['blob', { outputDir: `blob-report/${blobName}`, fileName: `${blobName}.zip` }]] as const
  : [['list'], ['junit', { outputFile: 'test-results/junit.xml' }], ['html', { open: 'never' }]] as const;

function stage(name: string, tags: string) {
  return {
    ...defineBddProject({
      name,
      features: 'features/**/*.feature',
      steps: 'steps/**/*.ts',
      tags,
    }),
    // Each stage keeps its own artifacts (traces, screenshots), so a later stage's run doesn't clear them
    outputDir: `test-results/${name}`,
  };
}

export default defineConfig({
  fullyParallel: true,
  // Capped so a full run can't look like a crawler to the app's per-IP rate limiter, if it's enabled
  workers: process.env.CI ? 4 : undefined,
  reporter: [...reporter],
  use: {
    baseURL: process.env.BASE_URL || 'https://demo.sobeklibrary.com',
    trace: 'retain-on-failure',
  },
  projects: [
    {
      name: 'smoke',
      testDir: './tests',
      outputDir: 'test-results/smoke',
    },
    stage('public', `not (${STAGE_TAGS})`),
    stage('logon-readonly', '@logon-readonly'),
    stage('content', '@content'),
    stage('structure', '@structure'),
    stage('site-settings', '@site-settings'),
  ],
});

