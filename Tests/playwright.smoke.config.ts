import { defineConfig } from '@playwright/test';
import 'dotenv/config';

// Smoke-only subset of the full test suite, meant to be invoked by
// DevDeploy-Scripts\Upgrade-CustomerSite.ps1 right after a site comes back online.
// Deliberately scoped to ./tests/smoke so that larger/slower tests added to ./tests
// as the main suite grows are never picked up here.
export default defineConfig({
  testDir: './tests/smoke',
  fullyParallel: true,
  reporter: [
    ['list'],
    ['junit', { outputFile: 'test-results/smoke/junit.xml' }],
    ['html', { outputFolder: 'playwright-report/smoke', open: 'never' }],
  ],
  use: {
    baseURL: process.env.BASE_URL || 'https://demo.sobeklibrary.com',
    trace: 'on-first-retry',
  },
});
