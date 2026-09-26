import type { Page } from '@playwright/test';

// A stand-in for the Google Maps JavaScript API, so map scenarios run without a key, without the network,
// and without depending on Google's rendering. Every map, area (polygon) and marker the page's own script
// creates is recorded in window.__sbkMaps, along with each marker's click handler, which the steps inspect
// and can "click" (see maps.steps.ts). It covers exactly the parts of the API SobekCM's map pages use:
// the map results view (Google_Map_ResultsViewer, which loads the API with callback=initMap) and map browse
// (Map_Browse_AggregationViewer, which loads it synchronously and then Google's KeyDragZoom library).
// A couple of real-key smoke scenarios check the genuine article separately.

export type StubMarker = { mapId: string | null; icon: string; position: [number, number] };
export type StubPolygon = { mapId: string | null; path: [number, number][] };
export type StubRecord = { maps: { id: string | null }[]; polygons: StubPolygon[]; markers: StubMarker[] };

const STUB_SCRIPT = String.raw`
(function () {
  if (window.google && window.google.maps && window.google.maps.__sbkStub) return;
  var rec = window.__sbkMaps = { maps: [], polygons: [], markers: [] };
  var handlers = window.__sbkMapHandlers = [];

  function LatLng(lat, lng) { this._lat = Number(lat); this._lng = Number(lng); }
  LatLng.prototype.lat = function () { return this._lat; };
  LatLng.prototype.lng = function () { return this._lng; };

  function mapId(map) { return (map && map.__el && map.__el.id) || null; }

  function Map(el, options) { this.__el = el; this.__options = options || {}; this.__center = this.__options.center; rec.maps.push({ id: el ? el.id : null }); }
  ['setCenter', 'setZoom', 'fitBounds', 'panTo', 'setOptions', 'addListener'].forEach(function (name) { Map.prototype[name] = function (value) { if (name === 'setCenter' || name === 'panTo') this.__center = value; }; });
  Map.prototype.getCenter = function () { return this.__center; };
  Map.prototype.getZoom = function () { return this.__options.zoom; };
  // Normally added by Google's KeyDragZoom library, which the stub replaces
  Map.prototype.enableKeyDragZoom = function () {};

  function Polygon(options) {
    options = options || {};
    this.__rec = { mapId: null, path: (options.paths || []).map(function (p) { return [p.lat(), p.lng()]; }) };
    rec.polygons.push(this.__rec);
    if (options.map) this.setMap(options.map);
  }
  Polygon.prototype.setMap = function (map) { this.__rec.mapId = mapId(map); };

  function Marker(options) {
    options = options || {};
    var icon = options.icon;
    this.__rec = { mapId: mapId(options.map), icon: icon && icon.url ? icon.url : String(icon || ''), position: options.position ? [options.position.lat(), options.position.lng()] : [0, 0] };
    this.__index = rec.markers.length;
    rec.markers.push(this.__rec);
  }
  Marker.prototype.setMap = function (map) { this.__rec.mapId = mapId(map); };

  function LatLngBounds() {}
  LatLngBounds.prototype.extend = function () { return this; };
  function InfoWindow() {}
  InfoWindow.prototype.setContent = function () {};
  InfoWindow.prototype.open = function () {};
  InfoWindow.prototype.close = function () {};
  function Size(w, h) { this.width = w; this.height = h; }
  function Point(x, y) { this.x = x; this.y = y; }
  function MarkerImage(url) { this.url = url; }

  var event = {
    addListener: function (target, name, handler) {
      if (target && target.__index !== undefined && name === 'click') handlers[target.__index] = handler;
      return { remove: function () {} };
    },
    addDomListener: function () { return { remove: function () {} }; },
    trigger: function () {}
  };

  window.google = { maps: {
    __sbkStub: true, Map: Map, LatLng: LatLng, LatLngBounds: LatLngBounds, Polygon: Polygon, Marker: Marker,
    InfoWindow: InfoWindow, Size: Size, Point: Point, MarkerImage: MarkerImage, event: event,
    MapTypeId: { ROADMAP: 'roadmap', TERRAIN: 'terrain', SATELLITE: 'satellite', HYBRID: 'hybrid' }
  } };

  // The results view loads the API with callback=initMap, and Google calls that once it's ready. An async
  // script can run before the page's own inline script has defined the callback, so wait for the document.
  var callback = new URL(document.currentScript ? document.currentScript.src : location.href).searchParams.get('callback');
  if (callback) {
    var go = function () { if (typeof window[callback] === 'function') window[callback](); };
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', go); else go();
  }
})();
`;

/** Routes the page's requests for the Google Maps API (and Google's KeyDragZoom add-on) to the stub.
 *  Call before the page is opened. */
export async function installGoogleMapsStub(page: Page) {
  await page.route(/^https:\/\/maps\.googleapis\.com\/maps\/api\/js/, (route) =>
    route.fulfill({ status: 200, contentType: 'application/javascript', body: STUB_SCRIPT }));
  await page.route(/keydragzoom[^/]*\.js/i, (route) =>
    route.fulfill({ status: 200, contentType: 'application/javascript', body: '/* KeyDragZoom replaced by the Google Maps stub */' }));
}

/** What the page's map script has created so far */
export async function stubRecord(page: Page): Promise<StubRecord> {
  return page.evaluate(() => (window as unknown as { __sbkMaps?: StubRecord }).__sbkMaps ?? { maps: [], polygons: [], markers: [] });
}

/** Calls the click handler the page attached to the given marker (index into the recorded markers) */
export async function clickStubMarker(page: Page, markerIndex: number) {
  const clicked = await page.evaluate((index) => {
    const handler = (window as unknown as { __sbkMapHandlers?: (() => void)[] }).__sbkMapHandlers?.[index];
    if (!handler) return false;
    handler();
    return true;
  }, markerIndex);
  if (!clicked) throw new Error(`The page attached no click handler to marker ${markerIndex}`);
}

/** Letter of a lettered Google marker icon (markerA.png -> "A"), or null for any other icon */
export function markerLetter(icon: string): string | null {
  const match = /marker([A-Z])\.png$/.exec(icon);
  return match ? match[1] : null;
}
