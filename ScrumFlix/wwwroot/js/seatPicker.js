/*
 * File: /wwwroot/js/seatPicker.js
 *
 * SVG SEAT PICKER — Interactive seat-selection grid.
 *
 * SEAT STATES & ICONS:
 *   available  — cinema-chair outline SVG, theme-color stroke, fully opaque
 *   selected   — cinema-chair outline SVG, gold/accent stroke
 *   reserved   — corner-bracket "blocked" SVG, solid brand-red (#C8102E) fill
 *                Uses a distinct icon (not just color) to satisfy WCAG SC 1.4.1.
 *                Red was chosen because it reads against all three theme backgrounds
 *                and is universally understood as "not available".
 *
 * BUG FIXES vs PREVIOUS VERSION:
 *   1. Reserved seats were invisible — Razor rendered them as sf-seat-ghost
 *      (visibility:hidden). The view now emits a separate sf-seat-reserved-tile
 *      span for Reserved status, which this script initialises on DOMContentLoaded.
 *   2. Reserved opacity was 0.3 — made them nearly invisible even when rendered.
 *      Reserved seats now render at full opacity with a distinct icon.
 *   3. Reserved tiles were never selected by the '.sf-seat-tile' querySelectorAll
 *      because they didn't exist in the DOM as interactive elements. Fixed by
 *      running a separate init pass over [data-seat-state="reserved"] spans.
 *
 * DOM REQUIREMENTS:
 *   #seatGrid              — container with .sf-seat-tile buttons + .sf-seat-reserved-tile spans
 *   #seatNumbersInput      — <input type="hidden"> for POST
 *   #qtyInput              — quantity input (fires "qtychanged" custom event)
 *   #selectedSeatsDisplay, #selectedSeatsText, #seatCountWarning
 *
 * CSHTML TILE REQUIREMENTS:
 *   Available: <button class="sf-seat sf-seat-open sf-seat-tile" data-label="A3" ...>
 *   Reserved:  <span   class="sf-seat sf-seat-tile sf-seat-reserved-tile"
 *                      data-label="A8" data-seat-state="reserved" aria-disabled="true">
 *   Sold/Blocked: <span class="sf-seat sf-seat-ghost" aria-hidden="true"> (invisible)
 */
(function () {
    'use strict';

    /* ═══════════════════════════════════════════════════════════════════════
       CINEMA CHAIR SVG  (cinema-chair-svgrepo-com.svg, viewBox 0 0 24 24)
       Used for: Available and Selected states.
    ═══════════════════════════════════════════════════════════════════════ */
    var CHAIR_PATHS = [
        'M9.14,1.5h5.73a2.86,2.86,0,0,1,2.86,2.86v9.55a0,0,0,0,1,0,0H6.27a0,0,0,0,1,0,0V4.36A2.86,2.86,0,0,1,9.14,1.5Z',
        'M1.5,10.09H6.27a0,0,0,0,1,0,0v3.34a2.39,2.39,0,0,1-2.39,2.39h0A2.39,2.39,0,0,1,1.5,13.43V10.09A0,0,0,0,1,1.5,10.09Z',
        'M17.73,10.09H22.5a0,0,0,0,1,0,0v3.34a2.39,2.39,0,0,1-2.39,2.39h0a2.39,2.39,0,0,1-2.39-2.39V10.09A0,0,0,0,1,17.73,10.09Z',
        'M4.6,15.7h0a2.08,2.08,0,0,0-.23.95,2,2,0,0,0,2,2H17.61a2,2,0,0,0,2-2,2.08,2.08,0,0,0-.23-.95,0,0,0,0,1,0,0',
    ];
    var CHAIR_EXTRAS = [
        { type: 'polyline', points: '17.79 13.98 17.73 13.91 6.27 13.91 6.21 13.98' },
        { type: 'line', x1: '7.23',  y1: '22.5', x2: '7.23',  y2: '18.68' },
        { type: 'line', x1: '16.77', y1: '22.5', x2: '16.77', y2: '18.68' },
        { type: 'line', x1: '5.32',  y1: '22.5', x2: '9.14',  y2: '22.5' },
        { type: 'line', x1: '14.86', y1: '22.5', x2: '18.68', y2: '22.5' },
    ];

    /* ═══════════════════════════════════════════════════════════════════════
       RESERVED ICON  (fullscreen-svgrepo-com.svg corner-bracket shape)
       viewBox 0 0 512 512 — corner brackets read as "blocked/occupied".
       Color: hardcoded #C8102E (brand red) — visible on ALL three themes.
       This is intentional: reserved status should be unmissable regardless
       of which theme the user has selected.
    ═══════════════════════════════════════════════════════════════════════ */
    /* RESERVED: orange — signals "temporarily held, may free up soon".
       Used only during active concurrency (someone mid-checkout), so it
       should feel urgent but not as final as the sold state. */
    var RESERVED_ICON_COLOR = '#D97706';

    /* SOLD / BLOCKED: high-contrast red — signals "permanently gone".
       Strong red (#C8102E brand red) reads clearly on all three themes
       and is universally understood as "not available". */
    var SOLD_ICON_COLOR = '#C8102E';

    function buildSoldSVG() {
        return '<svg viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">' +
            '<circle cx="12" cy="12" r="10" fill="none" stroke="' + SOLD_ICON_COLOR + '" stroke-width="1.5"/>' +
            '<line x1="8" y1="8" x2="16" y2="16" stroke="' + SOLD_ICON_COLOR + '" stroke-width="1.75" stroke-linecap="round"/>' +
            '<line x1="16" y1="8" x2="8" y2="16" stroke="' + SOLD_ICON_COLOR + '" stroke-width="1.75" stroke-linecap="round"/>' +
            '</svg>';
    }

    function buildReservedSVG() {
        return '<svg viewBox="0 0 512 512" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">' +
            '<path fill="' + RESERVED_ICON_COLOR + '" d="' +
            'M93.1,139.6l46.5-46.5L93.1,46.5L139.6,0H0v139.6l46.5-46.5L93.1,139.6z ' +
            'M93.1,372.4l-46.5,46.5L0,372.4V512h139.6l-46.5-46.5l46.5-46.5L93.1,372.4z ' +
            'M372.4,139.6H139.6v232.7h232.7V139.6z M325.8,325.8H186.2V186.2h139.6V325.8z ' +
            'M372.4,0l46.5,46.5l-46.5,46.5l46.5,46.5l46.5-46.5l46.5,46.5V0H372.4z ' +
            'M418.9,372.4l-46.5,46.5l46.5,46.5L372.4,512H512V372.4l-46.5,46.5L418.9,372.4z' +
            '"/></svg>';
    }

    /* ═══════════════════════════════════════════════════════════════════════
       COLOR TOKEN RESOLUTION
       Reads CSS custom properties from :root so theme tokens apply.
    ═══════════════════════════════════════════════════════════════════════ */
    function getCssToken(name) {
        return getComputedStyle(document.documentElement).getPropertyValue(name).trim();
    }

    function getChairColors(state) {
        switch (state) {
            case 'selected':
                return {
                    stroke:  getCssToken('--sf-seat-selected-border') || '#C9A012',
                    opacity: '1',
                };
            default: /* available */
                return {
                    stroke:  getCssToken('--sf-seat-open-border') || '#6B7280',
                    opacity: '1',
                };
        }
    }

    /* ═══════════════════════════════════════════════════════════════════════
       SVG BUILDERS
    ═══════════════════════════════════════════════════════════════════════ */
    function buildChairSVG(state) {
        var c  = getChairColors(state);
        var sw = '1.91';

        var paths = CHAIR_PATHS.map(function (d) {
            return '<path fill="none" stroke="' + c.stroke +
                '" stroke-miterlimit="10" stroke-width="' + sw + '" d="' + d + '"/>';
        });

        var extras = CHAIR_EXTRAS.map(function (el) {
            if (el.type === 'polyline') {
                return '<polyline fill="none" stroke="' + c.stroke +
                    '" stroke-miterlimit="10" stroke-width="' + sw +
                    '" points="' + el.points + '"/>';
            }
            return '<line stroke="' + c.stroke + '" stroke-width="' + sw +
                '" x1="' + el.x1 + '" y1="' + el.y1 +
                '" x2="' + el.x2 + '" y2="' + el.y2 + '"/>';
        });

        return '<svg viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg" ' +
            'aria-hidden="true" style="opacity:' + c.opacity + '">' +
            paths.join('') + extras.join('') + '</svg>';
    }

    function buildSeatSVG(state) {
        if (state === 'reserved') return buildReservedSVG();
        if (state === 'sold')     return buildSoldSVG();
        return buildChairSVG(state);
    }

    /* ═══════════════════════════════════════════════════════════════════════
       LABEL SPAN
       Overlaid on each tile. Reserved seats show their label in a lighter
       color since the red icon background provides contrast.
    ═══════════════════════════════════════════════════════════════════════ */
    function buildLabelSpan(label, state) {
        var color;
        if (state === 'selected') {
            color = getCssToken('--sf-seat-text-selected') || '#111';
        } else if (state === 'reserved') {
            color = 'rgba(255,255,255,0.9)';
        } else if (state === 'sold') {
            color = SOLD_ICON_COLOR;
        } else {
            color = getCssToken('--sf-seat-text-available') ||
                    getCssToken('--sf-text-muted') || '#8888a0';
        }
        return '<span class="sf-seat-tile-label" aria-hidden="true" ' +
            'style="color:' + color + '">' + label + '</span>';
    }

    function renderTile(tile, state) {
        var label = tile.getAttribute('data-label') || '';
        tile.innerHTML = buildSeatSVG(state) + buildLabelSpan(label, state);
    }

    /* ═══════════════════════════════════════════════════════════════════════
       MAIN INIT
    ═══════════════════════════════════════════════════════════════════════ */
    document.addEventListener('DOMContentLoaded', function () {

        var grid        = document.getElementById('seatGrid');
        var hiddenInput = document.getElementById('seatNumbersInput');
        var qtyInput    = document.getElementById('qtyInput');
        var display     = document.getElementById('selectedSeatsDisplay');
        var displayText = document.getElementById('selectedSeatsText');
        var warning     = document.getElementById('seatCountWarning');

        if (!grid || !hiddenInput) return;

        var selected = [];

        /* ── Render all available seats ─────────────────────────────────── */
        var availTiles = grid.querySelectorAll('.sf-seat-tile:not(.sf-seat-reserved-tile)');
        availTiles.forEach(function (tile) {
            renderTile(tile, 'available');
        });

        /* ── Render all reserved seats ──────────────────────────────────── */
        /* These are <span> elements (not buttons) — no click handler needed. */
        // var resvTiles = grid.querySelectorAll('.sf-seat-reserved-tile');
        // resvTiles.forEach(function (tile) {
        //     renderTile(tile, 'reserved');
        // });

        /* ── Render all reserved seats ──────────────────────────────────── */
        /* Spans are given focus and labels so screen reader users can navigate the map. */
        var resvTiles = grid.querySelectorAll('.sf-seat-reserved-tile');
        resvTiles.forEach(function (tile) {
            var label = tile.getAttribute('data-label') || '';
            renderTile(tile, 'reserved');

            // Accessibility Additions
            tile.setAttribute('tabindex', '0');
            tile.setAttribute('role', 'img'); // Informs users it represents a structural state
            tile.setAttribute('aria-label', 'Seat ' + label + ' — Reserved');
            tile.setAttribute('title', 'Seat ' + label + ' — Reserved');
        });

        /* ── Render all sold / blocked seats ────────────────────────────── */
        /* Non-interactive spans — just render the X icon, no click handler. */
        var soldTiles = grid.querySelectorAll('.sf-seat-sold-tile');
        soldTiles.forEach(function (tile) {
            renderTile(tile, 'sold');
        });

        /* ── Legend swatches ────────────────────────────────────────────── */
        document.querySelectorAll('.sf-legend-swatch[data-seat-state]').forEach(function (sw) {
            sw.innerHTML = buildSeatSVG(sw.getAttribute('data-seat-state') || 'available');
        });

        /* ── Helpers ────────────────────────────────────────────────────── */
        function getQtyCap() {
            if (typeof window.getQty === 'function') return window.getQty();
            return parseInt((qtyInput && qtyInput.value) || '1', 10) || 1;
        }

        function sync() {
            hiddenInput.value = selected.join(',');
            if (display) {
                display.style.display = selected.length > 0 ? '' : 'none';
                if (displayText) displayText.textContent = selected.join(', ');
            }
            if (warning) {
                warning.style.display = (selected.length >= getQtyCap()) ? '' : 'none';
            }
        }

        function deselectTile(label) {
            selected = selected.filter(function (l) { return l !== label; });
            var tile = grid.querySelector('.sf-seat-tile:not(.sf-seat-reserved-tile)[data-label="' + label + '"]');
            if (tile) {
                tile.classList.remove('sf-seat-selected');
                tile.setAttribute('aria-pressed', 'false');
                tile.setAttribute('aria-label', 'Seat ' + label + ' available');
                tile.setAttribute('title', label + ' — Available');
                renderTile(tile, 'available');
            }
        }

        function selectTile(label) {
            if (selected.indexOf(label) !== -1) return;
            selected.push(label);
            var tile = grid.querySelector('.sf-seat-tile:not(.sf-seat-reserved-tile)[data-label="' + label + '"]');
            if (tile) {
                tile.classList.add('sf-seat-selected');
                tile.setAttribute('aria-pressed', 'true');
                tile.setAttribute('aria-label', 'Seat ' + label + ' selected');
                tile.setAttribute('title', label + ' — Selected');
                renderTile(tile, 'selected');
            }
        }

        function toggleTile(label) {
            if (selected.indexOf(label) !== -1) {
                deselectTile(label);
            } else {
                if (selected.length >= getQtyCap()) {
                    if (warning) warning.style.display = '';
                    return;
                }
                selectTile(label);
            }
            sync();
        }

        function trimToQty() {
            var cap = getQtyCap();
            while (selected.length > cap) deselectTile(selected[selected.length - 1]);
            sync();
        }

        /* ── Wire up available tile clicks ──────────────────────────────── */
        availTiles.forEach(function (tile) {
            tile.addEventListener('click', function () {
                var label = tile.getAttribute('data-label');
                if (label) toggleTile(label);
            });
            tile.addEventListener('keydown', function (e) {
                if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    var label = tile.getAttribute('data-label');
                    if (label) toggleTile(label);
                }
            });
        });

        /* ── Re-render on theme change ──────────────────────────────────── */
        /* Available tiles use CSS token colors so must re-render on theme change.
           Reserved and sold tiles use hardcoded colors, but we re-render them
           anyway so the label color stays consistent with the new theme context. */
        window.addEventListener('sf:themechange', function () {
            availTiles.forEach(function (tile) {
                var label = tile.getAttribute('data-label');
                renderTile(tile, selected.indexOf(label) !== -1 ? 'selected' : 'available');
            });
            resvTiles.forEach(function (tile) {
                renderTile(tile, 'reserved');
            });
            soldTiles.forEach(function (tile) {
                renderTile(tile, 'sold');
            });
            document.querySelectorAll('.sf-legend-swatch[data-seat-state]').forEach(function (sw) {
                sw.innerHTML = buildSeatSVG(sw.getAttribute('data-seat-state') || 'available');
            });
        });

        /* ── Quantity change ────────────────────────────────────────────── */
        if (qtyInput) qtyInput.addEventListener('qtychanged', trimToQty);

        /* ── Re-hydrate from POST round-trip ────────────────────────────── */
        var existing = hiddenInput.value ? hiddenInput.value.split(',') : [];
        existing.forEach(function (label) {
            label = label.trim();
            if (label) selectTile(label);
        });

        sync();
    });
}());
