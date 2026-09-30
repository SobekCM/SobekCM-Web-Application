/* Page thumbnail ribbon and help dialog shared by the SobekCM geospatial editors */
(function (window, document) {
    'use strict';

    var root, viewport, leftArrow, rightArrow;
    var tiles = [];
    var selected = -1;
    var onSelect = null;

    function init(selectCallback) {
        root = document.getElementById('sbkGeo_Ribbon');
        viewport = document.getElementById('sbkGeo_RibbonViewport');
        if (!root || !viewport) return;

        onSelect = selectCallback;
        leftArrow = document.getElementById('sbkGeo_RibbonLeft');
        rightArrow = document.getElementById('sbkGeo_RibbonRight');
        tiles = Array.prototype.slice.call(viewport.querySelectorAll('.sbkGeo_Tile'));

        tiles.forEach(function (tile, index) {
            tile.setAttribute('aria-selected', 'false');
            tile.addEventListener('click', function () { select(index); });
        });

        leftArrow.addEventListener('click', function () { scrollPage(-1); });
        rightArrow.addEventListener('click', function () { scrollPage(1); });
        viewport.addEventListener('scroll', updateArrows, { passive: true });
        window.addEventListener('resize', updateArrows);
        document.addEventListener('keydown', onKeyDown);

        // Thumbnails change the track width as they load
        tiles.forEach(function (tile) {
            var img = tile.querySelector('img');
            if (img) img.addEventListener('load', updateArrows);
        });

        updateArrows();
    }

    function scrollPage(direction) {
        viewport.scrollBy({ left: direction * viewport.clientWidth, behavior: 'smooth' });
    }

    function updateArrows() {
        if (!viewport) return;
        var overflow = viewport.scrollWidth > viewport.clientWidth + 1;
        root.classList.toggle('sbkGeo_RibbonScrollable', overflow);
        leftArrow.disabled = !overflow || viewport.scrollLeft <= 0;
        rightArrow.disabled = !overflow || viewport.scrollLeft + viewport.clientWidth >= viewport.scrollWidth - 1;
    }

    function onKeyDown(e) {
        if (e.altKey || e.ctrlKey || e.metaKey || e.defaultPrevented) return;
        if (e.key !== 'ArrowLeft' && e.key !== 'ArrowRight') return;

        // Leave arrow keys alone in form fields and on the map, which pans with them
        var target = e.target;
        if (target && (/^(INPUT|TEXTAREA|SELECT)$/.test(target.tagName) || target.isContentEditable)) return;
        if (target && target.closest && target.closest('.sbkGeo_Map')) return;

        var next = selected + (e.key === 'ArrowLeft' ? -1 : 1);
        if (next >= 0 && next < tiles.length) {
            e.preventDefault();
            select(next);
            tiles[next].focus({ preventScroll: true });
        }
    }

    function select(index) {
        if (index < 0 || index >= tiles.length || index === selected) return;

        if (selected >= 0) {
            tiles[selected].classList.remove('sbkGeo_Selected');
            tiles[selected].setAttribute('aria-selected', 'false');
        }
        selected = index;
        tiles[index].classList.add('sbkGeo_Selected');
        tiles[index].setAttribute('aria-selected', 'true');
        tiles[index].scrollIntoView({ block: 'nearest', inline: 'nearest', behavior: 'smooth' });

        if (onSelect) onSelect(index);
    }

    function setStatus(index, hasGeo, dirty) {
        var tile = tiles[index];
        if (!tile) return;
        tile.classList.toggle('sbkGeo_HasGeo', !!hasGeo);
        tile.classList.toggle('sbkGeo_Dirty', !!dirty);
    }

    window.SobekGeoRibbon = {
        init: init,
        select: select,
        setStatus: setStatus,
        selected: function () { return selected; }
    };

    // ---------- Help dialog, also shared by both editors ----------

    var helpHidden = false;

    /** Wires up the editor's help dialog and Help button, and opens the dialog unless the user has asked not
        to see it again */
    function initHelp(hidden) {
        var dialog = document.getElementById('sbkGeo_Help');
        if (!dialog) return;
        helpHidden = !!hidden;

        document.getElementById('sbkGeo_HelpButton').addEventListener('click', openHelp);
        document.getElementById('sbkGeo_HelpOk').addEventListener('click', function () { dialog.close(); });
        dialog.addEventListener('close', saveHelpPreference);
        if (!helpHidden) openHelp();
    }

    function openHelp() {
        var dialog = document.getElementById('sbkGeo_Help');
        if (dialog.open) return;
        document.getElementById('sbkGeo_HelpHide').checked = helpHidden;
        dialog.showModal();
    }

    /** Closing the dialog (OK or Escape) saves the "don't show this again" box to the user's settings, in the
        background so the editor, and any unsaved work in it, stays where it is */
    function saveHelpPreference() {
        var hidden = document.getElementById('sbkGeo_HelpHide').checked;
        if (hidden === helpHidden) return;
        helpHidden = hidden;
        postSetting({ action: 'help_pref', help_hidden: hidden ? 'true' : 'false' });
    }

    /** Posts one of the user's editor settings back to this page in the background, so the editor, and any
        unsaved work in it, stays where it is */
    function postSetting(values) {
        var body = new URLSearchParams();
        Object.keys(values).forEach(function (key) { body.append(key, values[key]); });

        // getAttribute, since the form's hidden "action" input shadows form.action
        var form = document.getElementById('itemNavForm');
        return fetch((form && form.getAttribute('action')) || window.location.href, { method: 'POST', body: body, credentials: 'same-origin' });
    }

    window.SobekGeoHelp = { init: initHelp };

    // ---------- Starting map view, shared by both editors ----------

    var noteTimer = null;

    /** Wires up the buttons that save the current map view as where the editors open for an item with no
        location yet, and that clear it again.  getMap returns the editor's map, once it exists. */
    function initView(getMap) {
        var saveButton = document.getElementById('sbkGeo_SaveView');
        var clearButton = document.getElementById('sbkGeo_ClearView');
        if (!saveButton || !clearButton) return;

        saveButton.addEventListener('click', function () {
            var map = getMap();
            if (!map) return;
            var center = map.getCenter();
            var view = center.lat().toFixed(6) + ',' + center.lng().toFixed(6) + ',' + map.getZoom();
            postSetting({ action: 'default_view', view: view }).then(function () {
                clearButton.hidden = false;
                showNote('saved');
            }).catch(function () { });
        });

        clearButton.addEventListener('click', function () {
            postSetting({ action: 'default_view', view: '' }).then(function () {
                clearButton.hidden = true;
                showNote('cleared');
            }).catch(function () { });
        });
    }

    /** Briefly confirms a save or clear next to the buttons */
    function showNote(which) {
        var note = document.getElementById('sbkGeo_ViewNote');
        if (!note) return;
        note.textContent = note.getAttribute('data-' + which) || '';
        note.hidden = false;
        if (noteTimer) window.clearTimeout(noteTimer);
        noteTimer = window.setTimeout(function () { note.hidden = true; }, 3000);
    }

    window.SobekGeoView = { init: initView };
})(window, document);
