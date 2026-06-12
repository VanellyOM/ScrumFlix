/**
 * File:    /wwwroot/js/sf-backup.js
 * Purpose: Client-side helpers for the Admin Database Backup page.
 *
 *          - setAllCheckboxes(checked) — wires the "Select all" / "Clear all"
 *            buttons to toggle all .sf-backup-checkbox inputs at once.
 *
 *          - Row count badge update: updates a running "X tables selected"
 *            counter in the submit button label as the admin checks/unchecks.
 *
 * CSP compliance: no inline scripts or event handlers — all wiring here.
 * Loaded via @section Scripts in Backup.cshtml.
 *
 * The spinner on submit is handled by sf-crud-spinner.js (loaded globally by
 * _AdminLayout), wired via the data-crud-spinner attribute on the form.
 */

(function () {
    'use strict';

    var checkboxes  = null;
    var submitBtn   = null;

    function init() {
        checkboxes = document.querySelectorAll('.sf-backup-checkbox');
        submitBtn  = document.querySelector('[data-crud-spinner] [type="submit"]');
        if (!checkboxes.length) return;

        checkboxes.forEach(function (cb) {
            cb.addEventListener('change', updateButtonLabel);
        });

        updateButtonLabel();
    }

    /**
     * Sets all backup checkboxes to checked=true or checked=false.
     * Exposed globally so the inline-free "Select all / Clear all" buttons
     * can call it via onclick without violating CSP.
     */
    function setAllCheckboxes(checked) {
        if (!checkboxes) return;
        checkboxes.forEach(function (cb) { cb.checked = checked; });
        updateButtonLabel();
    }

    /** Updates the submit button text to show how many tables are selected. */
    function updateButtonLabel() {
        if (!submitBtn || !checkboxes) return;
        var selected = Array.prototype.filter.call(checkboxes, function (cb) {
            return cb.checked;
        }).length;
        var icon  = '<i class="bi bi-download me-2" aria-hidden="true"></i>';
        var label = selected === 1
            ? 'Generate &amp; download backup (1 table)'
            : 'Generate &amp; download backup (' + selected + ' tables)';
        submitBtn.innerHTML = icon + label;
        submitBtn.disabled  = selected === 0;
    }

    // Expose setAllCheckboxes globally for the onclick buttons in the view
    window.setAllCheckboxes = setAllCheckboxes;

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

}());
