/**
 * File:    /wwwroot/js/sf-shift-form.js
 * Purpose: Client-side time validation for the Add/Edit Shift form
 *          (_ShiftForm.cshtml partial).
 *
 * Replaces the inline <script> block formerly in Schedule/_ShiftForm.cshtml.
 * Moved to an external file to comply with the CSP script-src directive,
 * which does not include 'unsafe-inline'.
 *
 * This script is loaded in the @section Scripts of Schedule/Index.cshtml
 * so it is available when the partial is first rendered and also after
 * HTMX swaps it back in on edit clicks (HTMX re-executes scripts in
 * swapped content when htmx.config.allowScriptTags is true, but since
 * this is an external reference it is safe to load once at page level).
 *
 * Behaviour:
 *   - Validates that End Time is after Start Time on change.
 *   - Shows an inline error message and sets a custom validity message.
 *   - Blocks HTMX form submission via htmx:beforeRequest when invalid.
 *
 * Note: The form, start, end, and error elements are looked up on each
 * event rather than cached at load time, because the partial can be
 * swapped by HTMX and the original DOM references become stale.
 */
(function () {
    'use strict';

    function getElements() {
        return {
            form: document.getElementById('shift-form'),
            sEl:  document.getElementById('sf-start'),
            eEl:  document.getElementById('sf-end'),
            err:  document.getElementById('sf-time-err')
        };
    }

    function validate() {
        var els = getElements();
        if (!els.sEl || !els.eEl) return true;

        if (els.sEl.value && els.eEl.value &&
            new Date(els.eEl.value) <= new Date(els.sEl.value)) {
            if (els.err) els.err.style.display = 'block';
            els.eEl.setCustomValidity('End time must be after start time.');
            return false;
        }
        if (els.err) els.err.style.display = 'none';
        els.eEl.setCustomValidity('');
        return true;
    }

    // Use event delegation on document so handlers survive HTMX swaps
    document.addEventListener('change', function (ev) {
        if (ev.target.id === 'sf-start' || ev.target.id === 'sf-end') {
            validate();
        }
    });

    document.addEventListener('htmx:beforeRequest', function (ev) {
        var els = getElements();
        if (els.form && ev.target === els.form) {
            if (!validate()) ev.preventDefault();
        }
    });
}());
