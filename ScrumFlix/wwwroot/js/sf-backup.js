/**
 * File:    /wwwroot/js/sf-backup.js
 * Purpose: Client-side controller for the Admin Database Backup page
 *          (Phase 4.3 background-queue model).
 *
 *          - setAllCheckboxes(checked) — "Select all" / "Clear all" buttons.
 *          - Row count badge update in the submit button label.
 *          - Backup flow:
 *              1. On page load, open the /progressHub connection ONCE
 *                 (sfProgress.connect) so there is no connect-vs-navigation race.
 *              2. On submit: generate an operation id, join its group on the
 *                 already-open connection, then POST the form via fetch. The
 *                 server enqueues generation on a background queue and returns
 *                 { operationId } almost immediately; per-table/per-section
 *                 progress streams in over SignalR.
 *              3. On the terminal ProgressUpdate (isComplete), swap in the
 *                 "Backup ready — Download" panel via an HTMX outerHTML swap of
 *                 #backup-result-panel. The user clicks Download (a GET link to
 *                 DownloadBackup) to stream the staged .zip. No iframe / synthetic
 *                 click, so none of the previous download-vs-fetch teardown races
 *                 apply.
 *              4. On error, show an inline banner and re-enable the form.
 *
 * Requires (loaded via @section Scripts in Backup.cshtml):
 *   - signalr.js / signalr.min.js
 *   - htmx.min.js
 *   - sf-spinner.js   (loaded globally by _AdminLayout)
 *   - sf-progress.js
 *
 * CSP compliance: no inline scripts or event handlers — all wiring here.
 */

(function () {
    'use strict';

    var checkboxes  = null;
    var submitBtn   = null;
    var form        = null;
    var opIdField   = null;
    var spinnerWrap = null;
    var spinnerEl   = null;
    var statusEl    = null;
    var cancelBtn   = null;
    var errorBanner = null;
    var errorText   = null;
    var hub         = null;
    var submitting  = false;

    function init() {
        checkboxes  = document.querySelectorAll('.sf-backup-checkbox');
        form        = document.getElementById('sf-backup-form');
        submitBtn   = document.querySelector('.sf-export-btn');
        opIdField   = document.getElementById('sf-backup-operation-id');
        spinnerWrap = document.querySelector('.sf-export-spinner-wrap');
        spinnerEl   = document.querySelector('.sf-export-spinner');
        statusEl    = spinnerWrap ? spinnerWrap.querySelector('.sf-spinner__status') : null;
        cancelBtn   = document.getElementById('sf-backup-cancel-btn');
        errorBanner = document.getElementById('sf-backup-error-banner');
        errorText   = document.getElementById('sf-backup-error-text');

        if (checkboxes.length) {
            checkboxes.forEach(function (cb) {
                cb.addEventListener('change', updateButtonLabel);
            });
            updateButtonLabel();
        }

        if (form && submitBtn && spinnerWrap && spinnerEl) {
            // Open the hub connection once, now, on page load.
            if (window.sfProgress && typeof sfProgress.connect === 'function') {
                hub = sfProgress.connect();
            }
            form.addEventListener('submit', handleSubmit);
        }
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

    function newOperationId() {
        if (window.crypto && typeof window.crypto.randomUUID === 'function') {
            return window.crypto.randomUUID().replace(/-/g, '');
        }
        return 'op' + Date.now().toString(16) + Math.random().toString(16).slice(2);
    }

    function showError(message) {
        if (errorBanner && errorText) {
            errorText.textContent = message;
            errorBanner.classList.remove('d-none');
        }
        if (spinnerEl) sfSpinner.error(spinnerEl, message);
    }

    function hideError() {
        if (errorBanner) errorBanner.classList.add('d-none');
    }

    function resetUi() {
        submitBtn.disabled = false;
        if (cancelBtn) cancelBtn.disabled = false;
        submitting = false;
    }

    /** Swaps in the "Backup ready — Download" panel for the completed operation. */
    function showResultPanel(operationId) {
        if (typeof htmx === 'undefined') {
            // Fallback: no HTMX — point the user straight at the download URL.
            if (statusEl) statusEl.textContent = 'Backup ready.';
            window.location.href =
                '/Admin/AdminBackup/DownloadBackup?operationId=' + encodeURIComponent(operationId);
            return;
        }
        htmx.ajax('GET', '/Admin/AdminBackup/BackupResultPanel?operationId=' + encodeURIComponent(operationId), {
            target: '#backup-result-panel',
            swap: 'outerHTML'
        });
        if (spinnerWrap) spinnerWrap.classList.add('d-none');
        if (spinnerEl) sfSpinner.reset(spinnerEl);
        resetUi();
    }

    function handleSubmit(evt) {
        // No progress framework available — allow the native POST (degraded).
        if (!hub) return;

        evt.preventDefault();
        if (submitting) return;
        submitting = true;
        hideError();

        var operationId = newOperationId();
        if (opIdField) opIdField.value = operationId;

        spinnerWrap.classList.remove('d-none');
        sfSpinner.indeterminate(spinnerEl, true);
        submitBtn.disabled = true;
        if (cancelBtn) cancelBtn.disabled = false;

        // Join the operation group on the already-open connection.
        hub.join(operationId, {
            spinner:    spinnerEl,
            statusEl:   statusEl,
            cancelBtn:  cancelBtn,
            onComplete: function () {
                showResultPanel(operationId);
            },
            onError: function (state) {
                showError(state.status || 'Backup failed.');
                resetUi();
            }
        });

        var formData = new FormData(form);

        fetch(form.action, {
            method: 'POST',
            body: formData,
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        }).then(function (res) {
            return res.text().then(function (text) {
                var data = null;
                try { data = text ? JSON.parse(text) : null; }
                catch (parseErr) {
                    console.error('TriggerBackup: non-JSON response', {
                        status: res.status, statusText: res.statusText,
                        redirected: res.redirected, url: res.url,
                        bodyPreview: text.slice(0, 500)
                    });
                }
                return { ok: res.ok, data: data, status: res.status };
            });
        }).then(function (result) {
            if (!result.ok || result.data === null) {
                showError(
                    (result.data && result.data.error) ||
                    ('Backup failed (HTTP ' + result.status + '). See console for details.')
                );
                resetUi();
                return;
            }
            // Accepted — generation is running on the background queue. Progress
            // and the terminal "complete" (which swaps in the download panel)
            // arrive over SignalR; nothing more to do with this response.
        }).catch(function (err) {
            console.error('TriggerBackup request failed:', err);
            showError('Backup request failed — check your connection and try again.');
            resetUi();
        });
    }

    // Expose setAllCheckboxes globally for the onclick buttons in the view
    window.setAllCheckboxes = setAllCheckboxes;

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

}());
