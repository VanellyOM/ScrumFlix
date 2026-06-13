/**
 * File:    /wwwroot/js/sf-tmdb-sync.js
 * Purpose: Wires the TmdbSyncPage bulk-sync forms to the Phase 4.0 shared
 *          progress framework (ProgressHub / sf-progress.js) and drives the
 *          sf-spinner component.
 *
 * Requires (loaded before this file in TmdbSyncPage @section Scripts):
 *   - signalr.js / signalr.min.js
 *   - sf-spinner.js   (exposes window.sfSpinner)
 *   - sf-progress.js  (exposes window.sfProgress)
 *
 * HTML elements expected in TmdbSyncPage.cshtml:
 *   #tmdb-sync-spinner    — .sf-spinner wrapper
 *   #sync-progress-wrap   — container hidden with d-none until sync starts
 *   #sync-status-msg      — status text below the spinner
 *   #cnt-synced / #cnt-skipped / #cnt-failed / #cnt-total  — live counters
 *   .sf-sync-trigger-form — the two bulk-sync <form> elements
 *   .sf-sync-operation-id — hidden <input name="operationId"> inside each form
 *
 * Flow:
 *   1. On submit, prevent the default full-page POST (which would navigate
 *      the page and kill the in-flight SignalR negotiate/connection before
 *      any progress could be shown).
 *   2. Generate a client-side operation id, connect to /progressHub, and
 *      join that operation's group via sfProgress.track().
 *   3. POST the form via fetch (X-Requested-With: XMLHttpRequest), which
 *      runs the synchronous server-side sync while the SignalR connection
 *      stays alive on this page and receives "ProgressUpdate" events.
 *   4. On completion (either the "ProgressUpdate" complete event or the
 *      fetch response, whichever arrives first), reload the page so the
 *      dashboard reflects the new TMDb sync stats — matching the original
 *      full-page-POST behaviour's end state.
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

    function showProgress() {
        progressWrap.classList.remove('d-none');
        syncButtons.forEach(function (b) { b.disabled = true; });
        sfSpinner.indeterminate(spinner, true);
        syncRunning = true;
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
        // Fallback for older browsers without crypto.randomUUID
        return 'op' + Date.now().toString(16) + Math.random().toString(16).slice(2);
    }

    // Reload the page once, regardless of how many "finish" signals arrive
    // (ProgressUpdate complete event AND/OR the fetch response).
    var reloaded = false;
    function reloadOnce() {
        if (reloaded) return;
        reloaded = true;
        setTimeout(function () {
            window.location.reload();
        }, 800);
    }

    function showFetchError(message) {
        if (statusMsg) statusMsg.textContent = message;
        sfSpinner.error(spinner, message);
        syncButtons.forEach(function (b) { b.disabled = false; });
        syncRunning = false;
    }

    syncForms.forEach(function (form) {
        form.addEventListener('submit', function (evt) {
            if (syncRunning) return;

            if (!window.sfProgress) {
                // No progress framework available — fall back to the
                // original full-page POST behaviour.
                return;
            }

            evt.preventDefault();

            var opField = form.querySelector('.sf-sync-operation-id');
            var operationId = newOperationId();
            if (opField) opField.value = operationId;

            showProgress();

            var tracker = sfProgress.track({
                operationId: operationId,
                spinner:     spinner,
                statusEl:    statusMsg,
                onUpdate:    updateCounters,
                onComplete:  function (state) {
                    updateCounters(state);
                    reloadOnce();
                },
                onError: function (state) {
                    showFetchError(state.status || 'Sync failed.');
                }
            });

            // Submit via fetch — no page navigation, so the SignalR
            // connection above stays alive for the duration of the sync.
            var formData = new FormData(form);

            tracker.ready.then(function () {
                return fetch(form.action, {
                    method: 'POST',
                    body: formData,
                    headers: { 'X-Requested-With': 'XMLHttpRequest' }
                });
            }).then(function (res) {
                if (!res.ok) {
                    return res.text().then(function (text) {
                        console.error('TmdbSyncRun: non-OK response', res.status, text.slice(0, 500));
                        showFetchError('Sync failed (HTTP ' + res.status + ').');
                    });
                }

                var contentType = res.headers.get('content-type') || '';
                if (contentType.indexOf('application/json') === -1) {
                    // Likely a redirect to Login/AccessDenied (session expired
                    // mid-sync) rather than the expected JSON payload.
                    console.warn('TmdbSyncRun: unexpected response content-type', contentType);
                    reloadOnce();
                    return;
                }

                // Success — if the "ProgressUpdate" complete event already
                // reloaded the page, this is a no-op. If it was missed,
                // this triggers the reload as a fallback.
                return res.json().then(function (data) {
                    updateCounters(data);
                    reloadOnce();
                });
            }).catch(function (err) {
                console.error('TmdbSyncRun request failed:', err);
                showFetchError('Sync request failed — check your connection and try again.');
            });
        });
    });

}());
