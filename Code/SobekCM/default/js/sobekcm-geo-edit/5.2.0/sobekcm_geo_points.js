/* SobekCM location point editor: one point for the whole item and/or one per page */
(function (window, document) {
    'use strict';

    var data = null;
    var nodes = [];          // index 0 is the whole item, index N is page sequence N
    var current = -1;
    var map = null, geocoder = null;
    var domReady = false, mapsReady = false, mapStarted = false, submitting = false;

    function onDomReady() {
        var block = document.getElementById('sbkGeo_Data');
        if (!block) return;
        data = JSON.parse(block.textContent);

        nodes = data.nodes.map(function (n) {
            var point = (typeof n.lat === 'number' && typeof n.lng === 'number') ? { lat: n.lat, lng: n.lng } : null;
            return { label: n.label, point: point, original: point, marker: null, dirty: false };
        });

        document.getElementById('sbkGeo_Save').addEventListener('click', save);
        document.getElementById('sbkGeo_Cancel').addEventListener('click', cancel);
        document.getElementById('sbkGeo_ClearPoint').addEventListener('click', clearPoint);
        document.getElementById('sbkGeo_SearchButton').addEventListener('click', search);
        document.getElementById('sbkGeo_Search').addEventListener('keydown', function (e) {
            if (e.key === 'Enter') { e.preventDefault(); search(); }
        });
        window.addEventListener('beforeunload', function (e) {
            if (!submitting && anyDirty()) { e.preventDefault(); e.returnValue = ''; }
        });

        domReady = true;
        updateSaveButton();
        SobekGeoRibbon.init(selectNode);
        SobekGeoRibbon.select(0);

        // The Maps callback never fires if the script is blocked or the key is missing
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

        map = new google.maps.Map(document.getElementById('sbkGeo_Map'), {
            center: { lat: 20, lng: 0 },
            zoom: 3,
            clickableIcons: false,
            streetViewControl: false,
            gestureHandling: 'greedy',
            draggableCursor: 'crosshair'
        });
        geocoder = new google.maps.Geocoder();

        nodes.forEach(function (node, index) { if (node.point) node.marker = makeMarker(index); });
        refreshMarkers();
        fitToPoints();

        map.addListener('click', function (e) { setPoint(current, e.latLng); });
    }

    function showMapUnavailable() {
        var div = document.getElementById('sbkGeo_Map');
        div.innerHTML = '';
        var msg = document.createElement('div');
        msg.className = 'sbkGeo_MapUnavailable';
        msg.textContent = data.strings.mapUnavailable;
        div.appendChild(msg);
    }

    function fitToPoints() {
        var withPoints = nodes.filter(function (n) { return n.point; });
        if (withPoints.length === 1) {
            map.setCenter(withPoints[0].point);
            map.setZoom(12);
        } else if (withPoints.length > 1) {
            var bounds = new google.maps.LatLngBounds();
            withPoints.forEach(function (n) { bounds.extend(n.point); });
            map.fitBounds(bounds, 60);
        }
    }

    // Every marker is built here, so the legacy google.maps.Marker can later be swapped for
    // AdvancedMarkerElement (which requires a Map ID) in one place
    function makeMarker(index) {
        var marker = new google.maps.Marker({ map: map, position: nodes[index].point, title: nodes[index].label });
        marker.addListener('click', function () { if (index !== current) SobekGeoRibbon.select(index); });
        marker.addListener('dragend', function (e) { setPoint(index, e.latLng); });
        return marker;
    }

    function refreshMarkers() {
        nodes.forEach(function (node, index) {
            if (!node.marker) return;
            var active = index === current;
            node.marker.setDraggable(active);
            node.marker.setZIndex(active ? 1000 : index);
            node.marker.setIcon(active ? null : {
                path: google.maps.SymbolPath.CIRCLE,
                scale: 6,
                fillColor: '#777777',
                fillOpacity: 0.9,
                strokeColor: '#ffffff',
                strokeWeight: 2
            });
        });
    }

    function selectNode(index) {
        current = index;
        document.getElementById('sbkGeo_Current').textContent = nodes[index].label;
        if (!map) return;
        refreshMarkers();
        if (nodes[index].point) map.panTo(nodes[index].point);
    }

    function setPoint(index, latLng) {
        if (index < 0) return;
        var node = nodes[index];
        node.point = { lat: latLng.lat(), lng: latLng.lng() };
        if (node.marker) node.marker.setPosition(node.point);
        else node.marker = makeMarker(index);
        refreshMarkers();
        markChanged(index);
    }

    function clearPoint() {
        var node = nodes[current];
        if (!node || !node.point) return;
        node.point = null;
        if (node.marker) { node.marker.setMap(null); node.marker = null; }
        markChanged(current);
    }

    function samePoint(a, b) {
        if (!a || !b) return a === b;
        return Math.abs(a.lat - b.lat) < 1e-9 && Math.abs(a.lng - b.lng) < 1e-9;
    }

    function markChanged(index) {
        var node = nodes[index];
        node.dirty = !samePoint(node.point, node.original);
        SobekGeoRibbon.setStatus(index, !!node.point, node.dirty);
        updateSaveButton();
    }

    function anyDirty() {
        return nodes.some(function (n) { return n.dirty; });
    }

    function updateSaveButton() {
        document.getElementById('sbkGeo_Save').disabled = !anyDirty();
    }

    function search() {
        var query = document.getElementById('sbkGeo_Search').value.trim();
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
        var changes = [];
        nodes.forEach(function (node, index) {
            if (!node.dirty) return;
            changes.push(node.point
                ? { index: index, clear: false, lat: node.point.lat, lng: node.point.lng }
                : { index: index, clear: true });
        });
        if (changes.length === 0) return;
        submit('save', JSON.stringify({ changes: changes }));
    }

    function cancel() {
        submit('cancel', '');
    }

    function submit(action, payload) {
        submitting = true;
        document.getElementById('sbkGeo_Action').value = action;
        document.getElementById('sbkGeo_Payload').value = payload;
        document.getElementById('itemNavForm').submit();
    }

    window.gm_authFailure = function () { if (data) showMapUnavailable(); };

    window.SobekGeoPoints = { init: onMapsReady };

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', onDomReady);
    else onDomReady();
})(window, document);
