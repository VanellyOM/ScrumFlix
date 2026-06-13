/**
 * File:    /wwwroot/js/sf-tmdb-sync.js
 * Purpose: Wires the TmdbSyncPage bulk-sync forms to the shared progress
 *          framework (ProgressHub / sf-progress.js) under the Phase 4.3
 *          background-queue model, and drives the sf-spinner component.
 *
 * Phase 4.3 model:
 *   1. On page load, open the /progressHub connection ONCE (sfProgress.connect).
 *      No operation is running yet — this just removes the connect-vs-navigation
 *      race that plagued the synchronous design.
 *   2. On bulk-sync submit: generate an operation id, set the hidden field,
 *      join that operation's group on the already-open connection, then POST the
 *      form via fetch. The server enqueues the sync on a background queue and
 *      returns { operationId } almost immediately — the long sync runs on
 *      QueuedHostedService while ProgressUpdate events stream in over the
 *      pre-established connection.
 *   3. On the terminal ProgressUpdate (isComplete), refresh the coverage stat
 *      cards in place via an HTMX outerHTML swap of #tmdb-coverage-stats —
 *      NOT a full-page reload.
 *
 * Requires (loaded before this file in TmdbSyncPage @section Scripts):
 *   - signalr.js / signalr.min.js
 *   - htmx.min.js
 *   - sf-spinner.js   (exposes window.sfSpinner)
 *   - sf-progress.js  (exposes window.sfProgress)
 *
 * HTML elements expected in TmdbSyncPage.cshtml:
 *   #tmdb-coverage-stats  — coverage stat-cards panel (HTMX swap target)
 *   #tmdb-sync-spinner    — .sf-spinner wrapper
 *   #sync-progress-wrap   — container hidden with d-none until sync starts
 *   #sync-status-msg      — status text below the spinner
 *   #cnt-synced / #cnt-skipped / #cnt-failed / #cnt-total  — live counters
 *   .sf-sync-trigger-form — the two bulk-sync <form> elements
 *   .sf-sync-operation-id — hidden <input name="operationId"> inside each form
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

    var syncRunning = false;

    // Open the hub connection once, now, on page load.
    var hub = (window.sfProgress && typeof sfProgress.connect === 'function')
        ? sfProgress.connect()
        : null;

    function showProgress() {
        progressWrap.classList.remove('d-none');
        syncButtons.forEach(function (b) { b.disabled = true; });
        sfSpinner.indeterminate(spinner, true);
        syncRunning = true;
    }

    function releaseButtons() {
        syncButtons.forEach(function (b) { b.disabled = false; });
        syncRunning = false;
    }

    function updateCounters(state) {
        if (cntSynced)  cntSynced.textContent  = state.succeeded ?? 0;
        if (cntSkipped) cntSkipped.textContent = state.skipped   ?? 0;
        if (cntFailed)  cntFailed.textContent  = state.failed    ?? 0;
        if (cntTotal)   cntTotal.textContent   = state.total     ?? 0;
    }

    function newOperationId() {
        if (window.crypto && typeof window.crypto.randomUUID === 'function') {
            return window.crypto.randomUUID().replace(/-/g, '');
        }
        return 'op' + Date.now().toString(16) + Math.random().toString(16).slice(2);
    }

    // Refresh the coverage stat cards in place (no full-page reload).
    function refreshCoverageStats() {
        if (typeof htmx === 'undefined') return;
        htmx.ajax('GET', '/Admin/AdminHome/TmdbSyncStatsPartial', {
            target: '#tmdb-coverage-stats',
            swap: 'outerHTML'
        });
    }

    function showFetchError(message) {
        if (statusMsg) statusMsg.textContent = message;
        sfSpinner.error(spinner, message);
        releaseButtons();
    }

    syncForms.forEach(function (form) {
        form.addEventListener('submit', function (evt) {
            if (syncRunning) { evt.preventDefault(); return; }

            // No progress framework / hub — fall back to the original full-page
            // POST (the controller runs a synchronous sync for non-AJAX requests).
            if (!hub) return;

            evt.preventDefault();

            var opField     = form.querySelector('.sf-sync-operation-id');
            var operationId = newOperationId();
            if (opField) opField.value = operationId;

            showProgress();

            // Join the operation group on the already-open connection.
            hub.join(operationId, {
                spinner:    spinner,
                statusEl:   statusMsg,
                onUpdate:   updateCounters,
                onComplete: function (state) {
                    updateCounters(state);
                    releaseButtons();
                    refreshCoverageStats();
                },
                onError: function (state) {
                    showFetchError(state.status || 'Sync failed.');
                }
            });

            // POST the trigger. The server enqueues the work and returns
            // { operationId } almost immediately; progress arrives via SignalR.
            var formData = new FormData(form);

            fetch(form.action, {
                method: 'POST',
                body: formData,
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).then(function (res) {
                if (!res.ok) {
                    return res.text().then(function (text) {
                        console.error('TmdbSyncRun: non-OK response', res.status, text.slice(0, 500));
                        showFetchError('Sync failed (HTTP ' + res.status + ').');
                    });
                }
                var contentType = res.headers.get('content-type') || '';
                if (contentType.indexOf('application/json') === -1) {
                    // Likely a redirect to Login/AccessDenied (session expired).
                    console.warn('TmdbSyncRun: unexpected response content-type', contentType);
                    showFetchError('Sync could not start — please sign in again.');
                    return;
                }
                // Body is just { operationId, ... } — progress drives the UI from here.
                return res.json().then(function (data) {
                    if (!data || !data.operationId) {
                        console.warn('TmdbSyncRun: response missing operationId', data);
                    }
                });
            }).catch(function (err) {
                console.error('TmdbSyncRun request failed:', err);
                showFetchError('Sync request failed — check your connection and try again.');
            });
        });
    });

}());
