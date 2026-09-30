/* Draws an item's georeferenced page images back over its Google map, at each page's saved image extent.

   Only what is worth drawing is loaded: a sheet's full image is fetched once the sheet is in view and at
   least MIN_WIDTH_PX wide on screen, and at most MAX_SHEETS are drawn at once (the largest on screen first).
   A small map, or a many-tile aerial flight zoomed out, just shows the footprint outlines until you zoom in. */
(function (window, document) {
    'use strict';

    var MAX_SHEETS = 12;
    var MIN_WIDTH_PX = 80;

    var map = null;
    var sheets = [];
    var showImages = true;
    var opacity = 1;

    function attach(googleMap) {
        var block = document.getElementById('sbkGeo_Overlays');
        if (!block || !googleMap || map) return;
        var data = JSON.parse(block.textContent);
        map = googleMap;

        // Rotated sheets are drawn flat, so keep the map top-down
        map.setTilt(0);

        var SheetOverlay = createSheetOverlay();
        sheets = data.sheets.map(function (s) {
            var sheet = {
                label: s.label,
                image: s.image,
                link: s.link,
                corners: s.corners.map(function (c) { return new google.maps.LatLng(c[0], c[1]); }),
                bounds: new google.maps.LatLngBounds(),
                shown: false,
                loaded: false,
                overlay: null,
                footprint: s.footprint ? createFootprint(s) : null
            };
            sheet.corners.forEach(function (c) { sheet.bounds.extend(c); });
            sheet.overlay = new SheetOverlay(sheet);
            sheet.overlay.setMap(map);
            return sheet;
        });

        addControl(data.strings);
        map.addListener('idle', update);
        update();
    }

    /** The page's footprint, styled like the other outlines on the map. Once the page image is showing it
        goes fully transparent, so it no longer hides the map, but stays clickable through to the page. */
    function createFootprint(s) {
        var style = s.highlight
            ? { strokeColor: '#33cc00', strokeOpacity: 0.8, strokeWeight: 4, fillColor: '#22bb22', fillOpacity: 0.2 }
            : { strokeColor: '#3333FF', strokeOpacity: 0.2, strokeWeight: 0, fillColor: '#3333FF', fillOpacity: 0.2 };
        var polygon = new google.maps.Polygon({
            map: map,
            paths: s.footprint.map(function (c) { return { lat: c[0], lng: c[1] }; }),
            strokeColor: style.strokeColor,
            strokeOpacity: style.strokeOpacity,
            strokeWeight: style.strokeWeight,
            fillColor: style.fillColor,
            fillOpacity: style.fillOpacity
        });
        if (s.link) polygon.addListener('click', function () { window.location.href = s.link; });
        var footprint = { polygon: polygon, style: style, faded: false };

        // Same hover label as the map's other outlines (a lone highlighted footprint has none, as before).
        // A faded footprint shows only the label, not the thick hover outline, so the image stays clear.
        var label = s.highlight ? '' : (s.label || '');
        var labels = label ? labelOverlay() : null;
        if (labels) {
            polygon.addListener('mousemove', function () {
                labels.setLabel(label);
                if (!footprint.faded) polygon.setOptions({ strokeWeight: 10, strokeOpacity: 1.0 });
            });
            polygon.addListener('mouseout', function () {
                labels.setLabel('');
                if (!footprint.faded) polygon.setOptions({ strokeWeight: style.strokeWeight, strokeOpacity: style.strokeOpacity });
            });
        }
        return footprint;
    }

    /** The hover label overlay from the map's own script, created the same way it creates it */
    function labelOverlay() {
        var owner = window.sobekcm_map;
        if (!owner || !owner.globals || !window.SobekCM || !window.SobekCM.Polygon_Label_Overlay) return null;
        if (!owner.globals.polygonLabelOverlay) {
            owner.globals.polygonLabelOverlay = new window.SobekCM.Polygon_Label_Overlay(owner.globals.innermap);
            google.maps.event.addListener(owner.globals.innermap, 'mousemove', function () { owner.globals.polygonLabelOverlay.setLabel(''); });
        }
        return owner.globals.polygonLabelOverlay;
    }

    function fadeFootprint(sheet, fade) {
        var footprint = sheet.footprint;
        if (!footprint || footprint.faded === fade) return;
        footprint.faded = fade;
        footprint.polygon.setOptions(fade
            ? { strokeOpacity: 0, fillOpacity: 0 }
            : { strokeWeight: footprint.style.strokeWeight, strokeOpacity: footprint.style.strokeOpacity, fillOpacity: footprint.style.fillOpacity });
    }

    function createSheetOverlay() {
        function SheetOverlay(sheet) {
            this.sheet = sheet;
            this.div = null;
            this.img = null;
        }
        SheetOverlay.prototype = new google.maps.OverlayView();

        SheetOverlay.prototype.onAdd = function () {
            var div = document.createElement('div');
            div.style.position = 'absolute';
            div.style.transformOrigin = '50% 50%';
            div.style.pointerEvents = 'none';
            div.style.display = 'none';
            var img = document.createElement('img');
            img.alt = this.sheet.label || '';
            img.style.width = '100%';
            img.style.height = '100%';
            img.style.display = 'block';
            var sheet = this.sheet, overlay = this;
            img.addEventListener('load', function () { sheet.loaded = true; overlay.draw(); });
            div.appendChild(img);
            this.div = div;
            this.img = img;

            // The lowest pane, so footprint outlines, points, and their page links stay on top and clickable
            this.getPanes().mapPane.appendChild(div);
        };

        SheetOverlay.prototype.draw = function () {
            var sheet = this.sheet, div = this.div;
            if (!div) return;
            if (!sheet.shown || !showImages) {
                div.style.display = 'none';
                fadeFootprint(sheet, false);
                return;
            }

            if (!this.img.getAttribute('src')) this.img.src = sheet.image;

            // Corners are saved in top-left, top-right, bottom-right, bottom-left order
            var projection = this.getProjection();
            var px = sheet.corners.map(function (c) { return projection.fromLatLngToDivPixel(c); });
            var width = Math.hypot(px[1].x - px[0].x, px[1].y - px[0].y);
            var height = Math.hypot(px[2].x - px[1].x, px[2].y - px[1].y);
            var cx = (px[0].x + px[1].x + px[2].x + px[3].x) / 4;
            var cy = (px[0].y + px[1].y + px[2].y + px[3].y) / 4;
            var angle = Math.atan2(px[1].y - px[0].y, px[1].x - px[0].x);

            div.style.display = 'block';
            div.style.left = (cx - width / 2) + 'px';
            div.style.top = (cy - height / 2) + 'px';
            div.style.width = width + 'px';
            div.style.height = height + 'px';
            div.style.transform = 'rotate(' + angle + 'rad)';
            this.img.style.opacity = opacity;
            fadeFootprint(sheet, sheet.loaded);
        };

        SheetOverlay.prototype.onRemove = function () {
            if (this.div) this.div.parentNode.removeChild(this.div);
            this.div = null;
        };

        return SheetOverlay;
    }

    /** Picks which sheets to draw for the current view */
    function update() {
        var view = map.getBounds();
        var projection = map.getProjection();
        if (!view || !projection) return;
        var scale = Math.pow(2, map.getZoom());

        var candidates = [];
        sheets.forEach(function (sheet) {
            sheet.shown = false;
            if (!view.intersects(sheet.bounds)) return;
            var a = projection.fromLatLngToPoint(sheet.corners[0]);
            var b = projection.fromLatLngToPoint(sheet.corners[1]);
            sheet.widthPx = Math.hypot(b.x - a.x, b.y - a.y) * scale;
            if (sheet.widthPx >= MIN_WIDTH_PX) candidates.push(sheet);
        });

        candidates.sort(function (x, y) { return y.widthPx - x.widthPx; });
        candidates.slice(0, MAX_SHEETS).forEach(function (sheet) { sheet.shown = true; });
        sheets.forEach(function (sheet) { sheet.overlay.draw(); });
    }

    function redraw() {
        sheets.forEach(function (sheet) { sheet.overlay.draw(); });
    }

    function addControl(strings) {
        var box = document.createElement('div');
        box.style.cssText = 'background:#fff;margin:10px;padding:6px 10px;border-radius:2px;box-shadow:0 1px 4px rgba(0,0,0,.3);font:13px Roboto,Arial,sans-serif;';

        var controls = document.createElement('div');
        controls.style.cssText = 'display:flex;align-items:center;justify-content:center;gap:12px;';

        // Not obvious that the page images open a larger view, so say so under the controls
        var prompt = document.createElement('div');
        prompt.style.cssText = 'margin-top:4px;font-size:12px;color:#555;text-align:center;';
        prompt.textContent = strings.prompt || '';
        prompt.hidden = !strings.prompt;

        var showLabel = document.createElement('label');
        showLabel.style.cssText = 'display:flex;align-items:center;gap:4px;cursor:pointer;';
        var show = document.createElement('input');
        show.type = 'checkbox';
        show.checked = true;
        show.addEventListener('change', function () {
            showImages = show.checked;
            slider.disabled = !showImages;
            prompt.style.visibility = showImages ? '' : 'hidden';
            redraw();
        });
        showLabel.appendChild(show);
        showLabel.appendChild(document.createTextNode(strings.showImages));

        var sliderLabel = document.createElement('label');
        sliderLabel.style.cssText = 'display:flex;align-items:center;gap:6px;';
        sliderLabel.appendChild(document.createTextNode(strings.transparency));
        var slider = document.createElement('input');
        slider.type = 'range';
        slider.min = '0';
        slider.max = '90';
        slider.step = '5';
        slider.value = String(Math.round((1 - opacity) * 100));
        slider.addEventListener('input', function () { opacity = 1 - (parseFloat(slider.value) / 100); redraw(); });
        sliderLabel.appendChild(slider);

        controls.appendChild(showLabel);
        controls.appendChild(sliderLabel);
        box.appendChild(controls);
        box.appendChild(prompt);
        map.controls[google.maps.ControlPosition.TOP_CENTER].push(box);
    }

    window.SobekGeoDisplay = { attach: attach };
})(window, document);
