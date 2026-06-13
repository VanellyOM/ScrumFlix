/**
 * File:    /wwwroot/js/sf-backup.js
 * Purpose: Client-side controller for the Admin Database Backup page.
 *
 *          - setAllCheckboxes(checked) — wires the "Select all" / "Clear all"
 *            buttons to toggle all .sf-backup-checkbox inputs at once.
 *          - Row count badge update: updates a running "X tables selected"
 *            counter in the submit button label as the admin checks/unchecks.
 *          - Phase 4.2 two-phase backup flow:
 *              1. On submit, mint an operation id, join its ProgressHub group
 *                 (via sf-progress.js) BEFORE posting, then POST the form via
 *                 fetch so the page does not navigate away while the backup
 *                 (with real per-table/per-section progress) is generated.
 *              2. On success, fetch GET /Admin/AdminBackup/DownloadBackup and
 *                 trigger the browser's file-save dialog for the returned .zip.
 *              3. On error, show an inline banner and re-enable the form.
 *
 * Requires (loaded via @section Scripts in Backup.cshtml):
 *   - signalr.js / signalr.min.js
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
    }

    /** Triggers a browser download for the staged .zip and tidies up the UI. */
    function downloadBackup(operationId) {
        var url = '/Admin/AdminBackup/DownloadBackup?operationId=' + encodeURIComponent(operationId);

        // Use a hidden <iframe> rather than a synthetic <a> click. In some
        // browsers (notably Firefox), programmatically clicking an <a> for a
        // same-document download can be treated as enough of a navigation
        // event to abort other in-flight requests on the page — including
        // the still-resolving TriggerBackup fetch and the ProgressHub
        // SignalR connection, surfacing as a spurious NetworkError even
        // though the backup already succeeded. An iframe navigation is
        // scoped to the iframe's own browsing context and doesn't affect
        // the parent document's requests.
        var iframe = document.createElement('iframe');
        iframe.style.display = 'none';
        iframe.src = url;
        document.body.appendChild(iframe);

        setTimeout(function () {
            iframe.remove();
            if (spinnerWrap) spinnerWrap.classList.add('d-none');
            if (spinnerEl) sfSpinner.reset(spinnerEl);
            resetUi();
        }, 1500);
    }

    function handleSubmit(evt) {
        evt.preventDefault();
        hideError();

        var operationId = newOperationId();
        if (opIdField) opIdField.value = operationId;

        spinnerWrap.classList.remove('d-none');
        sfSpinner.indeterminate(spinnerEl, true);
        submitBtn.disabled = true;
        if (cancelBtn) cancelBtn.disabled = false;

        var downloaded = false;
        function downloadOnce(id) {
            if (downloaded) return;
            downloaded = true;
            downloadBackup(id);
        }

        var tracker = null;
        if (window.sfProgress) {
            tracker = sfProgress.track({
                operationId: operationId,
                spinner:     spinnerEl,
                statusEl:    statusEl,
                cancelBtn:   cancelBtn,
                onComplete:  function () {
                    downloadOnce(operationId);
                },
                onError: function (state) {
                    showError(state.status || 'Backup failed.');
                    resetUi();
                }
            });
        }

        var formData = new FormData(form);

        fetch(form.action, {
            method: 'POST',
            body: formData,
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
            .then(function (res) {
                return res.text().then(function (text) {
                    var data = null;
                    try {
                        data = text ? JSON.parse(text) : null;
                    } catch (parseErr) {
                        console.error('TriggerBackup: non-JSON response', {
                            status: res.status,
                            statusText: res.statusText,
                            redirected: res.redirected,
                            url: res.url,
                            bodyPreview: text.slice(0, 500)
                        });
                    }
                    return { ok: res.ok, data: data, raw: text, status: res.status };
                });
            })
            .then(function (result) {
                // If the SignalR "complete" event already fired and triggered
                // the download (see onComplete below), the click on the
                // synthetic <a> can cause the browser to tear down other
                // in-flight requests on the page — including this fetch,
                // which then resolves with a non-ok/empty body or rejects
                // entirely. The backup already succeeded in that case, so
                // there's nothing left to report here.
                if (downloaded) return;

                if (!result.ok || result.data === null) {
                    showError(
                        (result.data && result.data.error) ||
                        ('Backup failed (HTTP ' + result.status + '). See console for details.')
                    );
                    resetUi();
                    if (tracker) tracker.stop();
                    return;
                }
                // Normally driven by the "ProgressUpdate" complete event
                // (onComplete above) so the spinner reaches 100% via real
                // progress before the download starts. If that event was
                // missed (e.g. SignalR disconnected mid-run), fall back to
                // downloading immediately now that the response confirms the
                // backup is ready.
                downloadOnce(result.data.operationId);
            })
            .catch(function (err) {
                // Same race as above: if the download already started via
                // onComplete, a NetworkError here is an artifact of that
                // navigation tearing down this fetch, not a real failure.
                if (downloaded) {
                    console.warn('TriggerBackup: fetch interrupted after download already started (safe to ignore).', err);
                    return;
                }

                console.error('TriggerBackup request failed:', err);
                showError('Backup request failed — check your connection and try again.');
                resetUi();
                if (tracker) tracker.stop();
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
