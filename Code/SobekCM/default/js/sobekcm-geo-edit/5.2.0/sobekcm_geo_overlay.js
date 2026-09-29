/* SobekCM page image georeferencing editor: overlay each page image on the map, line it up, and save
   either the image's (rotated) outline or a hand-drawn polygon as the page's footprint.

   Image placement is kept in Google's world coordinates (Web Mercator, 256 units across the whole
   world at zoom 0, y increasing southward like the screen), so it can be drawn at any zoom and its
   corners come out exact. A placement is { center: {lat,lng}, w: width in world units, rotation: degrees
   clockwise }; the height is w * aspect, where aspect is the image's height / width. */
(function (window, document) {
    'use strict';

    var MODE_NONE = 'none', MODE_RECTANGLE = 'rectangle', MODE_CUSTOM = 'custom';
    var MIN_SIZE_PX = 24;
    var WORLD_ZOOM = 3;                 // starting zoom when the item has no location yet
    var NEW_IMAGE_WIDTH = 1 / 6;        // a newly placed image's width, as a fraction of the map's width

    var data = null;
    var pages = [];
    var current = -1;
    var map = null, geocoder = null, overlay = null;
    var footprint = null;          // google.maps.Polygon for the current page
    var drawing = null;            // in-progress polygon drawing
    var domReady = false, mapsReady = false, mapStarted = false, submitting = false;

    // ---------- Startup ----------

    function onDomReady() {
        var block = document.getElementById('sbkGeo_Data');
        if (!block) return;
        data = JSON.parse(block.textContent);

        pages = data.pages.map(function (p) {
            return {
                seq: p.seq,
                label: p.label,
                image: p.image,
                saved: p.polygon,
                mode: p.polygon ? p.polygon.mode : MODE_NONE,
                custom: null,
                placement: null,
                aspect: null,
                aspectLocked: false,
                opacity: 0.6,
                visible: true,
                original: null,
                dirty: false
            };
        });

        byId('sbkGeo_Save').addEventListener('click', save);
        byId('sbkGeo_Cancel').addEventListener('click', function () { submit('cancel', ''); });
        byId('sbkGeo_SearchButton').addEventListener('click', search);
        byId('sbkGeo_Search').addEventListener('keydown', function (e) {
            if (e.key === 'Enter') { e.preventDefault(); search(); }
        });
        byId('sbkGeo_CenterImage').addEventListener('click', centerImage);
        byId('sbkGeo_ToggleImage').addEventListener('click', toggleImage);
        byId('sbkGeo_Transparency').addEventListener('input', onTransparency);
        byId('sbkGeo_Rotation').addEventListener('input', onRotationInput);
        byId('sbkGeo_UsePerimeter').addEventListener('click', usePerimeter);
        byId('sbkGeo_DrawPolygon').addEventListener('click', startDrawing);
        byId('sbkGeo_FinishPolygon').addEventListener('click', finishDrawing);
        byId('sbkGeo_ClearPolygon').addEventListener('click', clearFootprint);
        document.addEventListener('keydown', function (e) { if (e.key === 'Escape' && drawing) cancelDrawing(); });
        window.addEventListener('beforeunload', function (e) {
            if (!submitting && anyDirty()) { e.preventDefault(); e.returnValue = ''; }
        });

        domReady = true;
        updateSaveButton();
        SobekGeoRibbon.init(selectPage);

        window.setTimeout(function () { if (!mapStarted) showMapUnavailable(); }, 10000);
        if (mapsReady) startMap();
    }

    function onMapsReady() {
        mapsReady = true;
        if (domReady) startMap();
    }

    function startMap() {
        if (mapStarted) return;
        mapStarted = true;

        var center = data.center;
        map = new google.maps.Map(byId('sbkGeo_Map'), {
            center: center ? { lat: center.lat, lng: center.lng } : { lat: 20, lng: 0 },
            zoom: center ? center.zoom : WORLD_ZOOM,
            clickableIcons: false,
            streetViewControl: false,
            rotateControl: false,
            tilt: 0,
            heading: 0,
            gestureHandling: 'greedy'
        });
        geocoder = new google.maps.Geocoder();
        overlay = createOverlay();
        overlay.setMap(map);

        map.addListener('click', function (e) { if (drawing) addVertex(e.latLng); });
        map.addListener('mousemove', function (e) { if (drawing) drawRubberBand(e.latLng); });

        // World coordinates need the map projection, which is ready on the first idle
        google.maps.event.addListenerOnce(map, 'idle', function () {
            pages.forEach(loadSavedGeometry);
            if (!center) fitAllFootprints();

            // Honor a tile clicked while the map was still loading
            var selected = SobekGeoRibbon.selected();
            if (selected >= 0) selectPage(selected);
            else SobekGeoRibbon.select(0);
        });
    }

    function showMapUnavailable() {
        var div = byId('sbkGeo_Map');
        div.innerHTML = '';
        var msg = document.createElement('div');
        msg.className = 'sbkGeo_MapUnavailable';
        msg.textContent = data.strings.mapUnavailable;
        div.appendChild(msg);
    }

    // ---------- Geometry helpers ----------

    function byId(id) { return document.getElementById(id); }

    function toWorld(latLng) {
        return map.getProjection().fromLatLngToPoint(new google.maps.LatLng(latLng.lat, latLng.lng));
    }

    function fromWorld(x, y) {
        var ll = map.getProjection().fromPointToLatLng(new google.maps.Point(x, y));
        return { lat: ll.lat(), lng: ll.lng() };
    }

    function normalizeDegrees(deg) {
        deg = deg % 360;
        return deg < 0 ? deg + 360 : deg;
    }

    function rotate(x, y, deg) {
        var r = deg * Math.PI / 180, c = Math.cos(r), s = Math.sin(r);
        return { x: x * c - y * s, y: x * s + y * c };
    }

    function aspectOf(page) {
        return page.aspect || 0.75;
    }

    /** The image's four corners, in order top-left, top-right, bottom-right, bottom-left */
    function corners(page) {
        var p = page.placement, c = toWorld(p.center);
        var hw = p.w / 2, hh = p.w * aspectOf(page) / 2;
        return [[-hw, -hh], [hw, -hh], [hw, hh], [-hw, hh]].map(function (o) {
            var r = rotate(o[0], o[1], p.rotation);
            return fromWorld(c.x + r.x, c.y + r.y);
        });
    }

    function round7(v) { return Math.round(v * 1e7) / 1e7; }

    /** What would be saved for this page right now, used both to post and to detect unsaved changes */
    function exportGeometry(page) {
        if (page.mode === MODE_RECTANGLE && page.placement) {
            return {
                mode: MODE_RECTANGLE,
                rotation: Math.round(page.placement.rotation * 100) / 100,
                points: corners(page).map(function (ll) { return [round7(ll.lat), round7(ll.lng)]; })
            };
        }
        if (page.mode === MODE_CUSTOM && page.custom && page.custom.length >= 3) {
            return {
                mode: MODE_CUSTOM,
                rotation: page.placement ? Math.round(page.placement.rotation * 100) / 100 : 0,
                points: page.custom.map(function (ll) { return [round7(ll.lat), round7(ll.lng)]; })
            };
        }
        return null;
    }

    /** Rebuilds the image placement from the page's saved polygon */
    function loadSavedGeometry(page) {
        var saved = page.saved;
        if (saved && saved.points && saved.points.length >= 2) {
            var world = saved.points.map(function (pt) { return toWorld({ lat: pt[0], lng: pt[1] }); });
            var rotation = normalizeDegrees(saved.rotation || 0);

            if (saved.mode === MODE_RECTANGLE && world.length === 4) {
                // Our own rotated rectangle: corners in TL, TR, BR, BL order
                var w = Math.hypot(world[1].x - world[0].x, world[1].y - world[0].y);
                var h = Math.hypot(world[2].x - world[1].x, world[2].y - world[1].y);
                var cx = (world[0].x + world[1].x + world[2].x + world[3].x) / 4;
                var cy = (world[0].y + world[1].y + world[2].y + world[3].y) / 4;
                page.placement = { center: fromWorld(cx, cy), w: w, rotation: normalizeDegrees(Math.atan2(world[1].y - world[0].y, world[1].x - world[0].x) * 180 / Math.PI) };
                if (w > 0) { page.aspect = h / w; page.aspectLocked = true; }
            } else {
                // A legacy two-corner box, or a custom polygon: place the image on its bounding box
                var minX = Infinity, maxX = -Infinity, minY = Infinity, maxY = -Infinity;
                world.forEach(function (pt) {
                    minX = Math.min(minX, pt.x); maxX = Math.max(maxX, pt.x);
                    minY = Math.min(minY, pt.y); maxY = Math.max(maxY, pt.y);
                });
                page.placement = { center: fromWorld((minX + maxX) / 2, (minY + maxY) / 2), w: maxX - minX, rotation: rotation };
                if (saved.mode === MODE_RECTANGLE && maxX > minX) { page.aspect = (maxY - minY) / (maxX - minX); page.aspectLocked = true; }
                if (saved.mode === MODE_CUSTOM) page.custom = saved.points.map(function (pt) { return { lat: pt[0], lng: pt[1] }; });
            }
        }
        page.original = JSON.stringify(exportGeometry(page));
    }

    function fitAllFootprints() {
        var bounds = new google.maps.LatLngBounds(), any = false;
        pages.forEach(function (page) {
            (page.saved && page.saved.points || []).forEach(function (pt) { bounds.extend({ lat: pt[0], lng: pt[1] }); any = true; });
        });
        if (any) map.fitBounds(bounds, 60);
    }

    // ---------- Image overlay ----------

    function createOverlay() {
        function ImageOverlay() {
            this.div = null;
            this.img = null;
        }
        ImageOverlay.prototype = new google.maps.OverlayView();

        ImageOverlay.prototype.onAdd = function () {
            var div = document.createElement('div');
            div.className = 'sbkGeo_Overlay';
            var img = document.createElement('img');
            img.alt = '';
            img.draggable = false;
            img.addEventListener('load', onImageLoaded);
            div.appendChild(img);

            ['NW', 'NE', 'SE', 'SW'].forEach(function (corner) {
                div.appendChild(makeHandle('sbkGeo_Handle sbkGeo_HandleResize sbkGeo_Handle' + corner, 'resize'));
            });
            var stem = document.createElement('span');
            stem.className = 'sbkGeo_RotateStem';
            div.appendChild(stem);
            div.appendChild(makeHandle('sbkGeo_Handle sbkGeo_HandleRotate', 'rotate'));
            div.appendChild(makeHandle('sbkGeo_Handle sbkGeo_HandleMove', 'move'));

            div.addEventListener('pointerdown', function (e) { beginGesture(e, 'move'); });
            google.maps.OverlayView.preventMapHitsAndGesturesFrom(div);

            this.div = div;
            this.img = img;
            this.getPanes().overlayMouseTarget.appendChild(div);
            showPageImage();
        };

        ImageOverlay.prototype.draw = function () {
            var page = pages[current];
            var div = this.div;
            if (!div) return;
            if (!page || !page.placement || !page.visible) {
                div.style.display = 'none';
                return;
            }

            var projection = this.getProjection();
            var c = projection.fromLatLngToDivPixel(new google.maps.LatLng(page.placement.center.lat, page.placement.center.lng));
            var scale = Math.pow(2, map.getZoom());
            var wpx = page.placement.w * scale;
            var hpx = wpx * aspectOf(page);

            div.style.display = 'block';
            div.style.left = (c.x - wpx / 2) + 'px';
            div.style.top = (c.y - hpx / 2) + 'px';
            div.style.width = wpx + 'px';
            div.style.height = hpx + 'px';
            div.style.transform = 'rotate(' + page.placement.rotation + 'deg)';
            this.img.style.opacity = page.opacity;

            // Once a custom footprint exists its vertices sit under the image, so only the handles stay grabbable
            div.classList.toggle('sbkGeo_BodyPassive', page.mode === MODE_CUSTOM);
            div.classList.toggle('sbkGeo_Drawing', !!drawing);
        };

        ImageOverlay.prototype.onRemove = function () {
            if (this.div) this.div.parentNode.removeChild(this.div);
            this.div = null;
        };

        return new ImageOverlay();
    }

    function makeHandle(className, gesture) {
        var handle = document.createElement('span');
        handle.className = className;
        handle.addEventListener('pointerdown', function (e) { e.stopPropagation(); beginGesture(e, gesture); });
        return handle;
    }

    function onImageLoaded() {
        var page = pages[current];
        var img = overlay.img;
        if (!page || !img.naturalWidth || img.getAttribute('data-seq') !== String(page.seq)) return;
        if (!page.aspectLocked) {
            page.aspect = img.naturalHeight / img.naturalWidth;
            overlay.draw();
            renderFootprint();
        }
    }

    function containerPoint(e) {
        var rect = map.getDiv().getBoundingClientRect();
        return { x: e.clientX - rect.left, y: e.clientY - rect.top };
    }

    function centerPixel(page) {
        return overlay.getProjection().fromLatLngToContainerPixel(new google.maps.LatLng(page.placement.center.lat, page.placement.center.lng));
    }

    /** Drag to move, drag a corner to resize (keeping the aspect ratio), or drag the top handle to rotate */
    function beginGesture(e, gesture) {
        var page = pages[current];
        if (!page || !page.placement || drawing || e.button !== 0) return;
        e.preventDefault();

        var target = e.currentTarget;
        target.setPointerCapture(e.pointerId);

        var start = containerPoint(e);
        var startCenter = centerPixel(page);
        var startW = page.placement.w;
        var startRadius = Math.hypot(start.x - startCenter.x, start.y - startCenter.y) || 1;

        function onMove(ev) {
            var pt = containerPoint(ev);
            if (gesture === 'move') {
                var moved = overlay.getProjection().fromContainerPixelToLatLng(new google.maps.Point(startCenter.x + pt.x - start.x, startCenter.y + pt.y - start.y));
                page.placement.center = { lat: moved.lat(), lng: moved.lng() };
            } else if (gesture === 'resize') {
                var radius = Math.hypot(pt.x - startCenter.x, pt.y - startCenter.y);
                var minW = MIN_SIZE_PX / Math.pow(2, map.getZoom());
                page.placement.w = Math.max(minW, startW * radius / startRadius);
            } else if (gesture === 'rotate') {
                var angle = Math.atan2(pt.y - startCenter.y, pt.x - startCenter.x) * 180 / Math.PI + 90;
                if (ev.shiftKey) angle = Math.round(angle / 15) * 15;
                page.placement.rotation = normalizeDegrees(Math.round(angle * 10) / 10);
                byId('sbkGeo_Rotation').value = page.placement.rotation;
            }
            placementChanged();
        }

        function onUp(ev) {
            target.releasePointerCapture(ev.pointerId);
            target.removeEventListener('pointermove', onMove);
            target.removeEventListener('pointerup', onUp);
            target.removeEventListener('pointercancel', onUp);
        }

        target.addEventListener('pointermove', onMove);
        target.addEventListener('pointerup', onUp);
        target.addEventListener('pointercancel', onUp);
    }

    function placementChanged() {
        overlay.draw();
        if (pages[current].mode === MODE_RECTANGLE) renderFootprint();
        markChanged(current);
    }

    /** Puts the current page's image in the middle of the map, about half the map's width */
    function centerImage() {
        var page = pages[current];
        if (!page || !map) return;
        var mapCenter = map.getCenter();
        var w = NEW_IMAGE_WIDTH * map.getDiv().clientWidth / Math.pow(2, map.getZoom());
        page.placement = { center: { lat: mapCenter.lat(), lng: mapCenter.lng() }, w: w, rotation: page.placement ? page.placement.rotation : 0 };
        page.visible = true;
        placementChanged();
    }

    function toggleImage() {
        var page = pages[current];
        if (!page) return;
        page.visible = !page.visible;
        overlay.draw();
    }

    function onTransparency(e) {
        var page = pages[current];
        if (!page) return;
        page.opacity = 1 - (parseFloat(e.target.value) / 100);
        overlay.draw();
    }

    function onRotationInput(e) {
        var page = pages[current];
        var value = parseFloat(e.target.value);
        if (!page || !page.placement || isNaN(value)) return;
        page.placement.rotation = normalizeDegrees(value);
        placementChanged();
    }

    // ---------- Footprint polygon ----------

    function renderFootprint() {
        if (footprint) { footprint.setMap(null); footprint = null; }
        var page = pages[current];
        if (!page || !map) return;

        if (page.mode === MODE_RECTANGLE && page.placement) {
            footprint = new google.maps.Polygon({
                map: map,
                paths: corners(page),
                clickable: false,
                strokeColor: '#1a73e8',
                strokeWeight: 2,
                fillColor: '#1a73e8',
                fillOpacity: 0.08
            });
        } else if (page.mode === MODE_CUSTOM && page.custom) {
            footprint = new google.maps.Polygon({
                map: map,
                paths: page.custom,
                editable: true,
                strokeColor: '#d93025',
                strokeWeight: 2,
                fillColor: '#d93025',
                fillOpacity: 0.12,
                zIndex: 10
            });
            var path = footprint.getPath();
            var sync = function () {
                page.custom = path.getArray().map(function (ll) { return { lat: ll.lat(), lng: ll.lng() }; });
                markChanged(current);
            };
            path.addListener('set_at', sync);
            path.addListener('insert_at', sync);
            path.addListener('remove_at', sync);

            // Right-click a corner to remove it, as long as a real polygon is left
            footprint.addListener('contextmenu', function (e) {
                if (e.vertex != null && path.getLength() > 3) path.removeAt(e.vertex);
            });
        }
    }

    function usePerimeter() {
        var page = pages[current];
        if (!page) return;
        if (drawing) cancelDrawing();
        if (!page.placement) centerImage();
        page.mode = MODE_RECTANGLE;
        page.custom = null;
        overlay.draw();
        renderFootprint();
        markChanged(current);
    }

    function clearFootprint() {
        var page = pages[current];
        if (!page) return;
        if (drawing) cancelDrawing();
        page.mode = MODE_NONE;
        page.custom = null;
        overlay.draw();
        renderFootprint();
        markChanged(current);
    }

    // ---------- Drawing a custom polygon ----------

    function startDrawing() {
        if (!map || current < 0) return;
        if (drawing) cancelDrawing();

        drawing = {
            path: [],
            line: new google.maps.Polyline({ map: map, clickable: false, strokeColor: '#d93025', strokeWeight: 2 }),
            start: null
        };
        // An existing editable polygon would swallow the clicks, so hide it until drawing ends
        if (footprint) { footprint.setMap(null); footprint = null; }
        map.setOptions({ draggableCursor: 'crosshair' });
        byId('sbkGeo_DrawPolygon').classList.add('sbkGeo_Active');
        byId('sbkGeo_FinishPolygon').hidden = false;
        byId('sbkGeo_DrawHint').hidden = false;
        overlay.draw();
    }

    function addVertex(latLng) {
        drawing.path.push({ lat: latLng.lat(), lng: latLng.lng() });
        drawing.line.setPath(drawing.path);
        if (!drawing.start) {
            drawing.start = new google.maps.Marker({
                map: map,
                position: drawing.path[0],
                zIndex: 2000,
                icon: { path: google.maps.SymbolPath.CIRCLE, scale: 7, fillColor: '#ffffff', fillOpacity: 1, strokeColor: '#d93025', strokeWeight: 3 }
            });
            drawing.start.addListener('click', finishDrawing);
        }
    }

    function drawRubberBand(latLng) {
        if (drawing.path.length === 0) return;
        drawing.line.setPath(drawing.path.concat([{ lat: latLng.lat(), lng: latLng.lng() }]));
    }

    function finishDrawing() {
        if (!drawing) return;
        var path = drawing.path;
        endDrawing();
        if (path.length < 3) return;

        var page = pages[current];
        page.mode = MODE_CUSTOM;
        page.custom = path;
        overlay.draw();
        renderFootprint();
        markChanged(current);
    }

    function cancelDrawing() {
        endDrawing();
    }

    function endDrawing() {
        if (!drawing) return;
        drawing.line.setMap(null);
        if (drawing.start) drawing.start.setMap(null);
        drawing = null;
        map.setOptions({ draggableCursor: null });
        byId('sbkGeo_DrawPolygon').classList.remove('sbkGeo_Active');
        byId('sbkGeo_FinishPolygon').hidden = true;
        byId('sbkGeo_DrawHint').hidden = true;
        overlay.draw();
        renderFootprint();
    }

    // ---------- Pages, search, save ----------

    function selectPage(index) {
        if (!map) return;
        if (drawing) cancelDrawing();
        current = index;
        var page = pages[index];
        byId('sbkGeo_Current').textContent = page.label;

        // A page seen for the first time with nothing saved starts in the middle of the current view
        if (!page.placement) centerImage();
        else {
            var bounds = new google.maps.LatLngBounds();
            (page.mode === MODE_CUSTOM && page.custom ? page.custom : corners(page)).forEach(function (ll) { bounds.extend(ll); });
            map.fitBounds(bounds, 80);
        }

        showPageImage();

        byId('sbkGeo_Transparency').value = Math.round((1 - page.opacity) * 100);
        byId('sbkGeo_Rotation').value = page.placement ? page.placement.rotation : 0;

        overlay.draw();
        renderFootprint();
    }

    /** Points the overlay image at the current page, once both the overlay and a page are ready */
    function showPageImage() {
        var page = pages[current];
        if (!page || !overlay || !overlay.img) return;
        overlay.img.setAttribute('data-seq', String(page.seq));
        if (overlay.img.getAttribute('src') !== page.image) overlay.img.src = page.image;
        if (overlay.img.complete) onImageLoaded();
    }

    function markChanged(index) {
        var page = pages[index];
        if (!page) return;
        page.dirty = JSON.stringify(exportGeometry(page)) !== page.original;
        SobekGeoRibbon.setStatus(index, page.mode !== MODE_NONE, page.dirty);
        updateSaveButton();
    }

    function anyDirty() {
        return pages.some(function (p) { return p.dirty; });
    }

    function updateSaveButton() {
        byId('sbkGeo_Save').disabled = !anyDirty();
    }

    function search() {
        var query = byId('sbkGeo_Search').value.trim();
        if (!query || !geocoder) return;
        geocoder.geocode({ address: query }, function (results, status) {
            if (status === 'OK' && results.length > 0) {
                var geometry = results[0].geometry;
                if (geometry.viewport) map.fitBounds(geometry.viewport);
                else { map.setCenter(geometry.location); map.setZoom(14); }
            } else {
                window.alert(data.strings.searchNotFound);
            }
        });
    }

    function save() {
        if (drawing) finishDrawing();
        var changes = [];
        pages.forEach(function (page) {
            if (!page.dirty) return;
            var geometry = exportGeometry(page);
            changes.push(geometry
                ? { index: page.seq, clear: false, mode: geometry.mode, rotation: geometry.rotation, points: geometry.points }
                : { index: page.seq, clear: true });
        });
        if (changes.length === 0) return;
        submit('save', JSON.stringify({ changes: changes }));
    }

    function submit(action, payload) {
        submitting = true;
        byId('sbkGeo_Action').value = action;
        byId('sbkGeo_Payload').value = payload;
        byId('itemNavForm').submit();
    }

    window.gm_authFailure = function () { if (data) showMapUnavailable(); };

    window.SobekGeoOverlay = { init: onMapsReady };

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', onDomReady);
    else onDomReady();
})(window, document);
