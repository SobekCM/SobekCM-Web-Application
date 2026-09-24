import { defineConfig } from '@playwright/test';
import { defineBddProject } from 'playwright-bdd';
import 'dotenv/config';

// Two projects share one run:
//   smoke - the plain Playwright specs under ./tests (also run on their own by playwright.smoke.config.ts)
//   bdd   - the Gherkin acceptance suite under ./features, compiled into .features-gen by `bddgen`
// `npm test` runs bddgen first; a bare `npx playwright test` would run stale (or no) generated BDD tests.
export default defineConfig({
  fullyParallel: true,
  // Capped so a full run can't look like a crawler to the app's per-IP rate limiter, if it's enabled
  workers: process.env.CI ? 4 : undefined,
  reporter: [
    ['list'],
    ['junit', { outputFile: 'test-results/junit.xml' }],
    ['html', { open: 'never' }],
  ],
  use: {
    baseURL: process.env.BASE_URL || 'https://demo.sobeklibrary.com',
    trace: 'retain-on-failure',
  },
  projects: [
    {
      name: 'smoke',
      testDir: './tests',
    },
    defineBddProject({
      name: 'bdd',
      features: 'features/**/*.feature',
      steps: 'steps/**/*.ts',
    }),
  ],
});
