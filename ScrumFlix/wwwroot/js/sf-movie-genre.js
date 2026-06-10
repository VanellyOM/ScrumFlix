/**
 * File:    /wwwroot/js/sf-movie-genre.js
 * Purpose: Two responsibilities for the Admin Movie Create and Edit forms:
 *
 *   1. Genre multiselect ↔ Primary Genre dropdown sync.
 *      Whenever the #genreMultiselect selection changes, the
 *      #primaryGenreSelect dropdown is filtered to show only the
 *      currently-selected genres. If the previously-chosen primary
 *      genre is deselected, the picker resets to the blank
 *      "Select primary genre…" option, or auto-selects the sole
 *      remaining genre when exactly one is chosen.
 *
 *   2. Delete-confirm handler (MovieEdit only).
 *      Intercepts any form carrying data-confirm and class sf-confirm-form
 *      and shows a window.confirm dialog before allowing the POST.
 *      This replaces the former onsubmit="return confirm(...)" attribute,
 *      which violated the CSP script-src 'unsafe-inline' exclusion.
 *
 * Extracted from inline <script> blocks in MovieCreate.cshtml and
 * MovieEdit.cshtml to comply with the project's strict CSP policy.
 *
 * Dependencies: none (vanilla JS, no framework required).
 *
 * Usage in a view's @section Scripts:
 *   <script src="~/js/sf-movie-genre.js" asp-append-version="true"></script>
 */
(function () {
    'use strict';

    function initGenreSync() {
        var multi   = document.getElementById('genreMultiselect');
        var primary = document.getElementById('primaryGenreSelect');
        if (!multi || !primary) return;

        function syncPrimary() {
            var selected = Array.from(multi.selectedOptions).map(function (o) {
                return o.value;
            });

            Array.from(primary.options).forEach(function (opt) {
                if (opt.value === '0') return;   // keep the placeholder always visible
                opt.hidden = !selected.includes(opt.value);
            });

            // If the current primary is no longer in the selection, clear it.
            if (!selected.includes(primary.value)) {
                primary.value = selected.length === 1 ? selected[0] : '0';
            }
        }

        multi.addEventListener('change', syncPrimary);
        syncPrimary();  // run on page load to reflect server-side defaults
    }

    function initConfirmForms() {
        document.querySelectorAll('form.sf-confirm-form').forEach(function (form) {
            form.addEventListener('submit', function (e) {
                var msg = form.dataset.confirm || 'Are you sure?';
                if (!window.confirm(msg)) e.preventDefault();
            });
        });
    }

    function init() {
        initGenreSync();
        initConfirmForms();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
}());
