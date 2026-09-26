import { expect, type Page } from '@playwright/test';
import { Given, When, Then } from './fixtures';
import { clickStubMarker, installGoogleMapsStub, markerLetter, stubRecord, type StubRecord } from '../support/google-maps-stub';

// Map search results (Google_Map_ResultsViewer) and map browse (Map_Browse_AggregationViewer), mostly checked
// against the Google Maps stub (support/google-maps-stub.ts): what matters here is what SobekCM asks Google to
// draw -- which maps, areas and markers, in what order -- not Google's own rendering.

// Waits for the page's map script to have created at least one map, then returns everything it created
async function drawnMaps(page: Page): Promise<StubRecord> {
  await expect.poll(async () => (await stubRecord(page)).maps.length, { message: 'the page never created a map' }).toBeGreaterThan(0);
  return stubRecord(page);
}

// Markers on the first map, in the order the page created them
async function pointMarkers(page: Page) {
  const record = await drawnMaps(page);
  const firstMap = record.maps[0].id;
  return record.markers.map((marker, index) => ({ ...marker, index })).filter((marker) => marker.mapId === firstMap);
}

function letters(count: number): string[] {
  return Array.from({ length: count }, (_, i) => String.fromCharCode(65 + i));
}

// ----- Setup -----

Given('Google Maps is stubbed', async ({ page }) => {
  await installGoogleMapsStub(page);
});

// ----- Maps and areas -----

Then('the map results should draw {int} map(s)', async ({ page }, count: number) => {
  await expect.poll(async () => (await stubRecord(page)).maps.length).toBe(count);
});

Then('each map should draw one area', async ({ page }) => {
  const record = await drawnMaps(page);
  for (const map of record.maps) {
    expect(record.polygons.filter((polygon) => polygon.mapId === map.id), `areas drawn on ${map.id}`).toHaveLength(1);
  }
});

// Size is the diagonal of each area's bounding box -- the same measure the Solr index sorts on
Then('the areas should be listed from smallest to largest', async ({ page }) => {
  const record = await drawnMaps(page);
  const sizes = record.polygons.map((polygon) => {
    const lats = polygon.path.map((p) => p[0]);
    const lngs = polygon.path.map((p) => p[1]);
    return Math.hypot(Math.max(...lats) - Math.min(...lats), Math.max(...lngs) - Math.min(...lngs));
  });
  expect(sizes.length, 'areas drawn').toBeGreaterThan(1);
  expect(sizes, 'area sizes, in the order listed').toEqual([...sizes].sort((a, b) => a - b));
});

// ----- Point markers -----

// Every point on the page is drawn on the first map; any maps after it draw areas
Then('the point results should share one map', async ({ page }) => {
  const record = await drawnMaps(page);
  const firstMap = record.maps[0].id;
  expect(record.markers.length, 'point markers drawn').toBeGreaterThan(1);
  expect(record.markers.filter((marker) => marker.mapId !== firstMap), 'point markers on any map but the first').toHaveLength(0);
  expect(record.polygons.filter((polygon) => polygon.mapId === firstMap), 'areas on the point map').toHaveLength(0);
});

// Google's lettered marker images only go up to Z; letters must never repeat (the viewer once fell back to A
// after the tenth point, so an eleventh point showed a second A)
Then('the point markers should be lettered in order, with no letter repeated', async ({ page }) => {
  const markers = await pointMarkers(page);
  expect(markers.map((marker) => markerLetter(marker.icon))).toEqual(letters(markers.length));
});

Then('each point marker should appear beside its titles in the list', async ({ page }) => {
  const markers = await pointMarkers(page);
  const listIcons = await page.locator('.sbkMrv_Marker img').evaluateAll((images) => images.map((image) => (image as HTMLImageElement).src));
  expect(listIcons.map(markerLetter), 'marker letters in the list').toEqual(markers.map((marker) => markerLetter(marker.icon)));
});

When('I click point marker {string}', async ({ page }, letter: string) => {
  const marker = (await pointMarkers(page)).find((m) => markerLetter(m.icon) === letter);
  expect(marker, `point marker ${letter}`).toBeTruthy();
  await clickStubMarker(page, marker!.index);
});

// The list marks everything at a point with data-sbkmrv-point="<map>_<point>", point 1 being marker A
Then('the titles at point marker {string} should be scrolled into view and highlighted', async ({ page }, letter: string) => {
  const point = letter.charCodeAt(0) - 64;
  const titles = page.locator(`[data-sbkmrv-point="1_${point}"]`);
  await expect(titles.first()).toBeInViewport();
  const count = await titles.count();
  expect(count, `titles listed at point ${letter}`).toBeGreaterThan(0);
  for (let i = 0; i < count; i++) {
    await expect(titles.nth(i)).toHaveClass(/sbkMrv_Highlight/);
  }
});

// Results are sorted by footprint size, and a point has size zero -- so on a page with both, the point map
// comes first and every map after it draws one area
Then('the points should come before the areas', async ({ page }) => {
  const record = await drawnMaps(page);
  const kinds = record.maps.map((map) =>
    record.markers.some((marker) => marker.mapId === map.id) ? 'points' :
    record.polygons.some((polygon) => polygon.mapId === map.id) ? 'area' : 'nothing');
  expect(kinds, 'what each map draws, in order').toContain('points');
  expect(kinds, 'what each map draws, in order').toContain('area');
  const firstArea = kinds.indexOf('area');
  expect(kinds.slice(0, firstArea).every((kind) => kind === 'points'), `maps in order: ${kinds.join(', ')}`).toBe(true);
  expect(kinds.slice(firstArea).every((kind) => kind === 'area'), `maps in order: ${kinds.join(', ')}`).toBe(true);
});

// ----- Map browse -----

Then('the map browse should show the collection\'s points', async ({ page }) => {
  const record = await drawnMaps(page);
  expect(record.maps, 'maps drawn').toHaveLength(1);
  expect(record.markers.length, 'points shown').toBeGreaterThan(0);
});

Then('the map browse should show no points', async ({ page }) => {
  const record = await drawnMaps(page);
  expect(record.markers, 'points shown').toHaveLength(0);
});

// ----- Real Google -----

Then('Google should draw the map', async ({ page }) => {
  // .gm-style is the container Google's own renderer adds inside the map element
  await expect(page.locator('.sbkMrv_MapCanvas .gm-style, #sbkMbav_MapDiv .gm-style').first()).toBeVisible({ timeout: 20000 });
});
