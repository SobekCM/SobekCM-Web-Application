import { test, expect } from '@playwright/test';

test('site is not down for maintenance', async ({ request }) => {
  const response = await request.get('/');

  expect(response.status()).not.toBe(503);

  const body = await response.text();
  expect(body).not.toContain('Down for Maintenance');
});
