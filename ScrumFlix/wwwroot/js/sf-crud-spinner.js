/**
 * File:    /wwwroot/js/sf-crud-spinner.js
 * Purpose: Show an indeterminate sf-spinner overlay during synchronous POST
 *          actions (delete, edit, single-movie sync, etc.) that do not have
 *          real server-side progress data.
 *
 * The overlay is shown immediately on form submit and disappears automatically
 * when the browser navigates away (redirect response). No cleanup needed.
 *
 * Usage
 * ─────
 * Add  data-crud-spinner  to any <form> element you want covered:
 *
 *   <form method="post" asp-action="MovieDelete" data-crud-spinner
 *         data-crud-spinner-label="Deleting movie…">
 *     ...
 *   </form>
 *
 * data-crud-spinner-label  (optional) — status text shown below the spinner.
 *                                       Defaults to "Working…"
 *
 * The spinner is shown as a fixed full-viewport overlay so it works in any
 * layout (Admin area, consumer pages) without per-page markup.
 *
 * The overlay HTML is injected once into <body> the first time any
 * data-crud-spinner form is found on the page.
 *
 * CSP compliance: no inline scripts or event handlers — all wiring here.
 * Loaded globally via _AdminLayout.cshtml @section Scripts (or per-page).
 *
 * Requires: sf-spinner.js (exposes window.sfSpinner), sf-spinner.css
 */

(function () {
    'use strict';

    var OVERLAY_ID = 'sf-crud-spinner-overlay';

    /** Build and inject the overlay into <body> once. */
    function ensureOverlay() {
        if (document.getElementById(OVERLAY_ID)) return;

        var overlay = document.createElement('div');
        overlay.id = OVERLAY_ID;
        overlay.setAttribute('aria-live', 'assertive');
        overlay.setAttribute('aria-label', 'Processing');
        overlay.setAttribute('role', 'status');

        // Inline style kept minimal — actual theming via sf-spinner.css tokens.
        // Fixed overlay so it covers the viewport regardless of scroll position.
        overlay.style.cssText = [
            'position:fixed',
            'inset:0',
            'z-index:9999',
            'display:none',
            'flex-direction:column',
            'align-items:center',
            'justify-content:center',
            'gap:0',
            // Semi-transparent backdrop using the app surface token (works across themes)
            'background:color-mix(in srgb, var(--sf-surface-base, #0d0d0d) 85%, transparent)',
            'backdrop-filter:blur(2px)',
            '-webkit-backdrop-filter:blur(2px)'
        ].join(';');

        // Spinner markup — identical structure to TmdbSyncPage spinner
        overlay.innerHTML = [
            '<div id="sf-crud-spinner-widget"',
            '     class="sf-spinner sf-spinner--lg"',
            '     role="progressbar"',
            '     aria-valuemin="0"',
            '     aria-valuemax="100"',
            '     aria-valuenow="0">',
            '  <svg class="sf-spinner__svg" viewBox="0 0 100 100" aria-hidden="true">',
            '    <circle class="sf-spinner__track" cx="50" cy="50" r="45"></circle>',
            '    <circle class="sf-spinner__ring"  cx="50" cy="50" r="45"></circle>',
            '  </svg>',
            '  <span class="sf-spinner__pct" aria-hidden="true">0%</span>',
            '</div>',
            '<p id="sf-crud-spinner-label" class="sf-spinner__status mt-3"></p>'
        ].join('');

        document.body.appendChild(overlay);
    }

    /** Show the overlay in indeterminate mode with an optional label. */
    function show(label) {
        ensureOverlay();

        var overlay     = document.getElementById(OVERLAY_ID);
        var spinnerEl   = document.getElementById('sf-crud-spinner-widget');
        var labelEl     = document.getElementById('sf-crud-spinner-label');

        if (!overlay || !spinnerEl) return;

        // Reset to indeterminate — we have no real percentage for sync POSTs
        if (window.sfSpinner) {
            sfSpinner.reset(spinnerEl);
            sfSpinner.indeterminate(spinnerEl, true);
        }

        if (labelEl) labelEl.textContent = label || 'Working\u2026';

        overlay.style.display = 'flex';
    }

    /** Hide the overlay (called on pagehide / popstate edge cases). */
    function hide() {
        var overlay = document.getElementById(OVERLAY_ID);
        if (overlay) overlay.style.display = 'none';
    }

    /**
     * Wire all [data-crud-spinner] forms on the current page.
     * Called once on DOMContentLoaded.
     */
    function wireForms() {
        var forms = document.querySelectorAll('[data-crud-spinner]');
        if (forms.length === 0) return;

        ensureOverlay(); // inject early so first paint is fast

        forms.forEach(function (form) {
            form.addEventListener('submit', function () {
                var label = form.getAttribute('data-crud-spinner-label') || 'Working\u2026';
                show(label);
            });
        });

        // Safety: hide if the user navigates back (browser back button)
        window.addEventListener('pageshow', function (e) {
            if (e.persisted) hide();
        });
    }

    // ── Init ──────────────────────────────────────────────────────────────
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', wireForms);
    } else {
        wireForms();
    }

    // Expose for manual control if needed (e.g. HTMX pages)
    window.sfCrudSpinner = { show: show, hide: hide };

}());
