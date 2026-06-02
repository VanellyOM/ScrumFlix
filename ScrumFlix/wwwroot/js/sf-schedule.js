/**
 * File:    /wwwroot/js/sf-schedule.js
 * Purpose: SignalR hub connection and HTMX refresh triggers for the Admin
 *          Schedule page (Schedule/Index.cshtml).
 *
 * Replaces the inline <script> block formerly in Schedule/Index.cshtml.
 * Moved to an external file to comply with the CSP script-src directive,
 * which does not include 'unsafe-inline'.
 *
 * Dependencies (must be loaded before this file):
 *   - signalr.min.js  (/js/signalr/dist/browser/signalr.min.js)
 *   - htmx            (cdn.jsdelivr.net — already in _Layout.cshtml)
 *
 * Behaviour:
 *   - Connects to /scheduleHub via SignalR.
 *   - On "ShiftsUpdated": refreshes the #shifts-grid partial via HTMX.
 *   - On "AssignmentsUpdated": refreshes the #assignments-grid partial,
 *     passing the currently selected locationId from the Gantt dropdown.
 */
(function () {
    'use strict';

    const connection = new signalR.HubConnectionBuilder()
        .withUrl('/scheduleHub')
        .withAutomaticReconnect()
        .build();

    connection.on('ShiftsUpdated', function () {
        htmx.ajax('GET', '/Admin/Schedule/ShiftsGrid', {
            target: '#shifts-grid',
            swap:   'innerHTML'
        });
    });

    connection.on('AssignmentsUpdated', function () {
        const locEl = document.getElementById('gantt-location');
        const locId = locEl ? locEl.value : 0;
        htmx.ajax('GET', '/Admin/Schedule/AssignmentsGrid?locationId=' + locId, {
            target: '#assignments-grid',
            swap:   'innerHTML'
        });
    });

    connection.start().catch(function (err) {
        console.error('SignalR connection error:', err);
    });
}());
