import { test, expect } from '@playwright/test';

test('GET /health returns healthy status', async ({ request, baseURL }) => {
  const url = new URL('/health', baseURL).toString();
  const response = await request.get('/health');
  const body = await response.text();

  // Only log on failure -- a 503 here is ambiguous between "app_offline.htm was still in
  // place" (body is the offline page HTML) and "the app is up but actually unhealthy"
  // (body is whatever the health check returned), and the two need different follow-up.
  if (response.status() !== 200) {
    console.log(`GET ${url} -> ${response.status()}`);
    console.log('Response headers:', response.headers());
    console.log('Response body (first 2000 chars):', body.slice(0, 2000));
  }

  expect(response.status()).toBe(200);
  expect(response.headers()['content-type']).toContain('text/plain');
  expect(body.trim()).toBe('Healthy');
});
