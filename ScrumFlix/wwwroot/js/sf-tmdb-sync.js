/**
 * File:    /wwwroot/js/sf-tmdb-sync.js
 * Purpose: Wires the TmdbSyncPage bulk-sync forms to the SignalR
 *          TmdbProgressHub and drives the sf-spinner component.
 *
 * Requires (loaded before this file in TmdbSyncPage @section Scripts):
 *   - signalr.js / signalr.min.js
 *   - sf-spinner.js  (exposes window.sfSpinner)
 *
 * HTML elements expected in TmdbSyncPage.cshtml:
 *   #tmdb-sync-spinner    — .sf-spinner wrapper
 *   #sync-progress-wrap   — container hidden with d-none until sync starts
 *   #sync-status-msg      — status text below the spinner
 *   #cnt-synced / #cnt-skipped / #cnt-failed / #cnt-total  — live counters
 *   .sf-sync-trigger-form — the two bulk-sync <form> elements
 *
 * CSP compliance: no inline script blocks — all logic lives here.
 */

(function () {
    'use strict';

    var spinner      = document.getElementById('tmdb-sync-spinner');
    var progressWrap = document.getElementById('sync-progress-wrap');
    var statusMsg    = document.getElementById('sync-status-msg');
    var cntSynced    = document.getElementById('cnt-synced');
    var cntSkipped   = document.getElementById('cnt-skipped');
    var cntFailed    = document.getElementById('cnt-failed');
    var cntTotal     = document.getElementById('cnt-total');
    var syncForms    = document.querySelectorAll('.sf-sync-trigger-form');
    var syncButtons  = document.querySelectorAll('.sf-sync-trigger-form button[type="submit"]');

    // Bail silently if none of the expected elements exist (wrong page).
    if (!progressWrap || !spinner || syncForms.length === 0) return;

    var connection  = null;
    var syncRunning = false;

    function showProgress() {
        progressWrap.classList.remove('d-none');
        syncButtons.forEach(function (b) { b.disabled = true; });
        sfSpinner.indeterminate(spinner, true);
        syncRunning = true;
    }

    function updateCounters(data) {
        if (cntSynced)  cntSynced.textContent  = data.synced  ?? 0;
        if (cntSkipped) cntSkipped.textContent = data.skipped ?? 0;
        if (cntFailed)  cntFailed.textContent  = data.failed  ?? 0;
        if (cntTotal)   cntTotal.textContent   = data.total   ?? 0;
    }

    function connectHub() {
        connection = new signalR.HubConnectionBuilder()
            .withUrl('/tmdbSyncHub')
            .withAutomaticReconnect()
            .build();

        connection.on('TmdbSyncProgress', function (data) {
            sfSpinner.indeterminate(spinner, false);
            sfSpinner.update(spinner, data.percent, data.message);
            if (statusMsg) statusMsg.textContent = data.message;
            updateCounters(data);
        });

        connection.on('TmdbSyncComplete', function (data) {
            sfSpinner.complete(spinner, 'Sync complete!');
            if (statusMsg) statusMsg.textContent = 'Sync complete!';
            updateCounters(data);
            syncButtons.forEach(function (b) { b.disabled = false; });
            syncRunning = false;
            // Reload the page after a short delay so the movie table refreshes
            setTimeout(function () { window.location.reload(); }, 2000);
        });

        connection.on('TmdbSyncError', function (data) {
            sfSpinner.error(spinner, data.message || 'Sync failed.');
            if (statusMsg) statusMsg.textContent = data.message || 'Sync failed.';
            syncButtons.forEach(function (b) { b.disabled = false; });
            syncRunning = false;
        });

        connection.start().catch(function (err) {
            console.error('TmdbProgressHub connection error:', err);
        });
    }

    // Show spinner and connect hub when a sync form is submitted
    syncForms.forEach(function (form) {
        form.addEventListener('submit', function () {
            if (syncRunning) return;
            showProgress();
            // Connect hub if not already connected
            if (!connection || connection.state === signalR.HubConnectionState.Disconnected) {
                connectHub();
            }
        });
    });

}());
