import { test as base, createBdd } from 'playwright-bdd';
import type { APIResponse, Response } from '@playwright/test';

// Per-scenario state shared between steps
export type World = {
  // Response of the last browser navigation (page.goto), when a step made one
  response?: Response | null;
  // Last raw HTTP request made without the browser (see http.steps.ts)
  apiResponse?: APIResponse;
  apiBody?: string;
  apiUrl?: string;
  // Site this scenario's raw HTTP requests go to, when a step chose one other than BASE_URL
  // (see "a site that allows robots" in http.steps.ts); undefined means BASE_URL
  siteBaseUrl?: string;
  // Uncaught JavaScript errors thrown by any page in this scenario
  pageErrors: string[];
};

export const test = base.extend<{ world: World }>({
  world: async ({ page }, use) => {
    const world: World = { pageErrors: [] };
    page.on('pageerror', (error) => world.pageErrors.push(error.message));
    await use(world);
  },
});

export const { Given, When, Then } = createBdd(test);
