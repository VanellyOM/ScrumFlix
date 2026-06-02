/**
 * File:    /wwwroot/js/sf-admin-tables.js
 * Purpose: Client-side column sort for Admin catalog tables.
 *          Used by Movies/MovieCatalog.cshtml and Concessions/ConcessionsCatalog.cshtml.
 *
 * Replaces the duplicate inline <script> blocks formerly in those two views.
 * Moved to an external file to comply with the CSP script-src directive,
 * which does not include 'unsafe-inline'.
 *
 * Usage in a view:
 *   <th onclick="sortTable('movies-table', 0)">Title</th>
 *   <script src="~/js/sf-admin-tables.js"></script>
 *
 * The table element must have an id and a <tbody>.
 * Numeric columns are sorted numerically; all others lexicographically.
 * Currency symbols and commas are stripped before numeric parsing.
 *
 * Clicking the same column header a second time reverses the sort direction.
 */
(function (global) {
    'use strict';

    // Track sort direction per table+column combination
    var sortState = {};

    /**
     * Sorts a table by the given column index.
     * @param {string} tableId - The id attribute of the <table> element.
     * @param {number} colIndex - Zero-based column index to sort by.
     */
    function sortTable(tableId, colIndex) {
        var tbl = document.getElementById(tableId);
        if (!tbl) return;

        var key = tableId + ':' + colIndex;
        sortState[key] = !sortState[key];   // toggle: true = ascending
        var asc = sortState[key];

        var tbody = tbl.querySelector('tbody');
        if (!tbody) return;

        var rows = Array.from(tbody.querySelectorAll('tr'));

        rows.sort(function (a, b) {
            var va = (a.cells[colIndex] ? a.cells[colIndex].textContent.trim() : '');
            var vb = (b.cells[colIndex] ? b.cells[colIndex].textContent.trim() : '');

            // Strip currency symbols and commas for numeric comparison
            var na = parseFloat(va.replace(/[$,]/g, ''));
            var nb = parseFloat(vb.replace(/[$,]/g, ''));
            var cmp = isNaN(na) || isNaN(nb)
                ? va.localeCompare(vb)
                : na - nb;

            return asc ? cmp : -cmp;
        });

        rows.forEach(function (r) { tbody.appendChild(r); });
    }

    // Expose globally so onclick="sortTable(...)" in view markup still works
    global.sortTable = sortTable;

}(window));
