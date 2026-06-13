/**
 * File:    /wwwroot/js/sf-progress.js
 * Purpose: Generic client for the Phase 4.0 shared progress framework.
 *          Connects to /progressHub, joins an operation's broadcast group,
 *          and drives an sf-spinner element from "ProgressUpdate" events.
 *
 * Requires (loaded before this file via @section Scripts):
 *   - signalr.js / signalr.min.js
 *   - sf-spinner.js  (exposes window.sfSpinner)
 *
 * Server contract (ProgressReporter / ProgressHub):
 *   Event "ProgressUpdate" payload (ProgressState record, camelCase via
 *   System.Text.Json default):
 *     {
 *       operationId, operationName, status, percent, current, total,
 *       succeeded, skipped, failed, isComplete, isError, completionSummary
 *     }
 *
 * Usage
 * ─────
 *   var tracker = sfProgress.track({
 *     operationId: '...',          // returned by the triggering POST
 *     spinner:     '#some-spinner',// .sf-spinner wrapper or element
 *     statusEl:    '#status-msg',  // optional — receives state.status text
 *     cancelBtn:   '#cancel-btn',  // optional — wired to ClientCancel
 *     onUpdate:    function (state) { ... },   // optional, called on every event
 *     onComplete:  function (state) { ... },   // optional, called once on completion
 *     onError:     function (state) { ... }    // optional, called once on error
 *   });
 *
 *   // Later, to stop listening without cancelling the operation:
 *   tracker.stop();
 *
 * CSP compliance: no inline scripts or event handlers — all wiring here.
 */

(function (global) {
    'use strict';

    function resolve(el) {
        if (!el) return null;
        if (typeof el === 'string') return document.querySelector(el);
        return el;
    }

    /**
     * Connects to /progressHub, joins the given operation's group, and wires
     * the resulting "ProgressUpdate" events to sfSpinner + optional callbacks.
     *
     * @param {object} opts
     * @param {string} opts.operationId   - required operation id to subscribe to
     * @param {Element|string} [opts.spinner]   - .sf-spinner wrapper for sfSpinner.update/complete/error
     * @param {Element|string} [opts.statusEl]  - element to receive state.status text
     * @param {Element|string} [opts.cancelBtn] - button that requests cancellation via ClientCancel
     * @param {function}       [opts.onUpdate]   - called for every ProgressUpdate
     * @param {function}       [opts.onComplete] - called once when isComplete === true
     * @param {function}       [opts.onError]    - called once when isError === true
     * @returns {{ stop: function, connection: object|null, ready: Promise<boolean> }}
     *   `ready` resolves to true once connected and joined the operation
     *   group, or false if connecting/joining failed. Never rejects — safe
     *   to await before doing something time-sensitive (e.g. submitting a
     *   form that navigates the page) without risking an unhandled rejection
     *   or indefinite hang.
     */
    function track(opts) {
        opts = opts || {};

        var operationId = opts.operationId;
        var spinner     = resolve(opts.spinner);
        var statusEl    = resolve(opts.statusEl);
        var cancelBtn   = resolve(opts.cancelBtn);

        var result = { stop: function () {}, connection: null, ready: Promise.resolve(false) };

        if (!operationId || typeof signalR === 'undefined') {
            return result;
        }

        if (spinner) {
            sfSpinner.indeterminate(spinner, true);
        }

        var connection = new signalR.HubConnectionBuilder()
            .withUrl('/progressHub')
            .withAutomaticReconnect()
            .build();

        result.connection = connection;

        var finished = false;

        function handleUpdate(state) {
            if (!state || finished) return;

            if (spinner) {
                sfSpinner.indeterminate(spinner, false);

                if (state.isError) {
                    sfSpinner.error(spinner, state.status || 'Something went wrong.');
                } else if (state.isComplete) {
                    sfSpinner.complete(spinner, state.completionSummary || state.status || 'Complete!');
                } else {
                    sfSpinner.update(spinner, state.percent, state.status);
                }
            }

            if (statusEl) {
                statusEl.textContent = state.status || '';
            }

            if (typeof opts.onUpdate === 'function') {
                opts.onUpdate(state);
            }

            if (state.isComplete || state.isError) {
                finished = true;

                if (cancelBtn) cancelBtn.disabled = true;

                if (state.isComplete && typeof opts.onComplete === 'function') {
                    opts.onComplete(state);
                }
                if (state.isError && typeof opts.onError === 'function') {
                    opts.onError(state);
                }
            }
        }

        connection.on('ProgressUpdate', handleUpdate);

        // result.ready resolves true once connected AND joined, or false if
        // that fails — never rejects, so callers can safely
        // `await tracker.ready` before doing something time-sensitive (like
        // submitting a form that will navigate the page) without it ever
        // hanging or throwing.
        result.ready = connection.start()
            .then(function () {
                return connection.invoke('JoinOperation', operationId);
            })
            .then(function () {
                return true;
            })
            .catch(function (err) {
                console.error('ProgressHub connection error:', err);
                if (spinner) {
                    sfSpinner.indeterminate(spinner, false);
                    sfSpinner.error(spinner, 'Could not connect for progress updates.');
                }
                if (statusEl) {
                    var detail = (err && err.message) ? err.message : String(err);
                    statusEl.textContent = 'Could not connect for progress updates: ' + detail;
                }
                return false;
            });

        if (cancelBtn) {
            cancelBtn.addEventListener('click', function () {
                cancelBtn.disabled = true;
                connection.invoke('ClientCancel', operationId).catch(function (err) {
                    console.error('ProgressHub cancel error:', err);
                });
            });
        }

        result.stop = function () {
            connection.invoke('LeaveOperation', operationId).catch(function () {});
            connection.stop();
        };

        return result;
    }

    global.sfProgress = {
        track: track
    };

}(window));
