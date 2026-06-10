/**
 * File:    /wwwroot/js/sf-export.js
 * Purpose: Wires the Export Reports page forms to the sf-spinner component,
 *          showing an indeterminate progress spinner while a file download
 *          is being prepared by the server.
 *
 * Requires (loaded globally by _AdminLayout):
 *   - sf-spinner.js  (exposes window.sfSpinner)
 *
 * HTML elements expected in Exports.cshtml:
 *   .sf-export-form          — each export <form> element
 *   .sf-export-spinner-wrap  — the d-none container holding the spinner for that form
 *   .sf-export-spinner       — the .sf-spinner element inside that container
 *   .sf-export-btn           — the submit button inside that form
 *
 * Behaviour:
 *   1. On submit: show spinner, disable button, show "Preparing…" message.
 *   2. File downloads do not trigger any DOM event, so the page stays loaded.
 *      We detect "done" by listening for window focus returning (user dismisses
 *      the save dialog) with a minimum display time of 1 second to prevent flicker.
 *   3. On window focus: hide spinner, re-enable button, reset state.
 *   4. If the server returns an error (page reloads with a flash banner), the
 *      spinner is never shown on the new load so no cleanup is needed.
 *
 * CSP compliance: no inline script blocks — all logic lives here.
 */

(function () {
    'use strict';

    var forms = document.querySelectorAll('.sf-export-form');
    if (!forms.length) return;

    // Minimum ms to show the spinner (prevents instant flicker on fast responses)
    var MIN_DISPLAY_MS = 1200;

    forms.forEach(function (form) {
        var spinnerWrap = form.querySelector('.sf-export-spinner-wrap');
        var spinnerEl   = form.querySelector('.sf-export-spinner');
        var btn         = form.querySelector('.sf-export-btn');

        if (!spinnerWrap || !spinnerEl || !btn) return;

        var startTime = null;
        var focusHandler = null;

        form.addEventListener('submit', function () {
            // Show spinner immediately
            spinnerWrap.classList.remove('d-none');
            sfSpinner.indeterminate(spinnerEl, true);
            btn.disabled = true;

            startTime = Date.now();

            // Listen for window regaining focus — this fires when the OS
            // file-save dialog closes after a successful download.
            focusHandler = function () {
                var elapsed = Date.now() - startTime;
                var remaining = Math.max(0, MIN_DISPLAY_MS - elapsed);

                setTimeout(function () {
                    sfSpinner.reset(spinnerEl);
                    spinnerWrap.classList.add('d-none');
                    btn.disabled = false;
                }, remaining);

                window.removeEventListener('focus', focusHandler);
                focusHandler = null;
            };

            window.addEventListener('focus', focusHandler);
        });
    });

}());
