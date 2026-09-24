import { expect } from '@playwright/test';
import { When, Then } from './fixtures';

// Raw HTTP requests, without the browser: redirects are NOT followed, so a scenario can assert
// exactly what the server sent back (status, Location header, body).

const ROBOT_USER_AGENT = 'Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)';

When('I request {string}', async ({ request, world }, path: string) => {
  world.apiUrl = path;
  world.apiResponse = await request.get(path, { maxRedirects: 0 });
  world.apiBody = await world.apiResponse.text();
});

When('a search engine robot requests {string}', async ({ request, world }, path: string) => {
  world.apiUrl = path;
  world.apiResponse = await request.get(path, { maxRedirects: 0, headers: { 'User-Agent': ROBOT_USER_AGENT } });
  world.apiBody = await world.apiResponse.text();
});

Then('the HTTP status should be {int}', async ({ world }, status: number) => {
  expect(world.apiResponse!.status()).toBe(status);
});

Then('the HTTP status should not be {int}', async ({ world }, status: number) => {
  expect(world.apiResponse!.status()).not.toBe(status);
});

Then('the response should redirect to {string}', async ({ world, baseURL }, expected: string) => {
  const status = world.apiResponse!.status();
  expect([301, 302, 303, 307, 308]).toContain(status);
  const location = new URL(world.apiResponse!.headers()['location'], baseURL);
  expect(decodeURIComponent(location.pathname + location.search)).toBe(expected);
});

Then('the response should not redirect back to the same address', async ({ world, baseURL }) => {
  const location = world.apiResponse!.headers()['location'];
  if (!location) return;
  const requested = new URL(world.apiUrl!, baseURL);
  const target = new URL(location, baseURL);
  expect(target.pathname + target.search, 'server redirected the request to itself').not.toBe(requested.pathname + requested.search);
});

Then('the response body should contain {string}', async ({ world }, text: string) => {
  expect(world.apiBody).toContain(text);
});

Then('the response body should not contain {string}', async ({ world }, text: string) => {
  expect(world.apiBody).not.toContain(text);
});

Then('the response should be an HTML page', async ({ world }) => {
  expect(world.apiResponse!.headers()['content-type']).toContain('text/html');
  expect(world.apiBody).toMatch(/<html/i);
});
