import { test, expect } from '@playwright/test';

test('GET /health returns healthy status', async ({ request }) => {
  const response = await request.get('/health');

  expect(response.status()).toBe(200);
  expect(response.headers()['content-type']).toContain('text/plain');

  const body = await response.text();
  expect(body.trim()).toBe('Healthy');
});
