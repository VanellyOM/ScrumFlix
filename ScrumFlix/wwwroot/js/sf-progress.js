/**
 * File:    /wwwroot/js/sf-progress.js
 * Purpose: Generic client for the shared progress framework. Connects to
 *          /progressHub, joins an operation's broadcast group, and drives an
 *          sf-spinner element from "ProgressUpdate" events.
 *
 * Two APIs:
 *   1. sfProgress.track({...})    — legacy one-shot: connect + join + drive in a
 *                                   single call. Kept for back-compat.
 *   2. sfProgress.connect({...})  — Phase 4.3: open the SignalR connection ONCE
 *                                   on page load (no operation id yet), then
 *                                   call .join(operationId, handlers) when the
 *                                   triggering POST returns an operation id.
 *                                   This removes the connect-vs-navigation race:
 *                                   by the time JoinOperation is invoked, the
 *                                   negotiate/connect handshake is already done.
 *
 * Requires (loaded before this file via @section Scripts):
 *   - signalr.js / signalr.min.js
 *   - sf-spinner.js  (exposes window.sfSpinner)
 *
 * Server contract (ProgressReporter / ProgressHub):
 *   Event "ProgressUpdate" payload (ProgressState record, camelCase):
 *     { operationId, operationName, status, percent, current, total,
 *       succeeded, skipped, failed, isComplete, isError, completionSummary }
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
     * Applies a ProgressState to a handler's spinner / status element /
     * callbacks. Shared by both track() and connect().join().
     * Returns true once a terminal (complete/error) state has been handled.
     */
    function applyState(h, state) {
        if (!state || h.finished) return h.finished;

        if (h.spinner) {
            sfSpinner.indeterminate(h.spinner, false);

            if (state.isError) {
                sfSpinner.error(h.spinner, state.status || 'Something went wrong.');
            } else if (state.isComplete) {
                sfSpinner.complete(h.spinner, state.completionSummary || state.status || 'Complete!');
            } else {
                sfSpinner.update(h.spinner, state.percent, state.status);
            }
        }

        if (h.statusEl) {
            h.statusEl.textContent = state.status || '';
        }

        if (typeof h.onUpdate === 'function') {
            h.onUpdate(state);
        }

        if (state.isComplete || state.isError) {
            h.finished = true;
            if (h.cancelBtn) h.cancelBtn.disabled = true;

            if (state.isComplete && typeof h.onComplete === 'function') h.onComplete(state);
            if (state.isError && typeof h.onError === 'function') h.onError(state);
        }

        return h.finished;
    }

    // -- Legacy one-shot API -------------------------------------------------

    /**
     * Connects to /progressHub, joins the given operation's group, and wires the
     * resulting "ProgressUpdate" events to sfSpinner + optional callbacks.
     *
     * @returns {{ stop: function, connection: object|null, ready: Promise<boolean> }}
     */
    function track(opts) {
        opts = opts || {};

        var operationId = opts.operationId;
        var h = {
            spinner:    resolve(opts.spinner),
            statusEl:   resolve(opts.statusEl),
            cancelBtn:  resolve(opts.cancelBtn),
            onUpdate:   opts.onUpdate,
            onComplete: opts.onComplete,
            onError:    opts.onError,
            finished:   false
        };

        var result = { stop: function () {}, connection: null, ready: Promise.resolve(false) };

        if (!operationId || typeof signalR === 'undefined') {
            return result;
        }

        if (h.spinner) sfSpinner.indeterminate(h.spinner, true);

        var connection = new signalR.HubConnectionBuilder()
            .withUrl('/progressHub')
            .withAutomaticReconnect()
            .build();

        result.connection = connection;

        connection.on('ProgressUpdate', function (state) {
            if (state && state.operationId === operationId) applyState(h, state);
        });

        result.ready = connection.start()
            .then(function () { return connection.invoke('JoinOperation', operationId); })
            .then(function () { return true; })
            .catch(function (err) {
                console.error('ProgressHub connection error:', err);
                if (h.spinner) {
                    sfSpinner.indeterminate(h.spinner, false);
                    sfSpinner.error(h.spinner, 'Could not connect for progress updates.');
                }
                if (h.statusEl) {
                    var detail = (err && err.message) ? err.message : String(err);
                    h.statusEl.textContent = 'Could not connect for progress updates: ' + detail;
                }
                return false;
            });

        if (h.cancelBtn) {
            h.cancelBtn.addEventListener('click', function () {
                h.cancelBtn.disabled = true;
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

    // -- Phase 4.3 connect-on-load API ---------------------------------------

    /**
     * Opens a /progressHub connection immediately (call on DOMContentLoaded).
     * The connection has no operation id yet — call .join(operationId, handlers)
     * once the triggering POST returns one.
     *
     * @param {object} [opts]
     * @param {function} [opts.onConnected]  - called with true/false once start() settles
     * @returns {{
     *   ready: Promise<boolean>,
     *   connection: object|null,
     *   join: function(operationId, handlers): { ready: Promise<boolean>, stop: function },
     *   stop: function
     * }}
     */
    function connect(opts) {
        opts = opts || {};

        var result = {
            ready: Promise.resolve(false),
            connection: null,
            join: function () { return { ready: Promise.resolve(false), stop: function () {} }; },
            stop: function () {}
        };

        if (typeof signalR === 'undefined') {
            console.error('sfProgress.connect: SignalR not loaded.');
            return result;
        }

        var connection = new signalR.HubConnectionBuilder()
            .withUrl('/progressHub')
            .withAutomaticReconnect()
            .build();

        result.connection = connection;

        // One dispatcher for the whole connection; routes each ProgressUpdate to
        // the handler registered for that operation id. Supports multiple
        // sequential operations on the same page without rebinding.
        var handlers = {}; // operationId -> handler descriptor

        connection.on('ProgressUpdate', function (state) {
            if (!state || !state.operationId) return;
            var h = handlers[state.operationId];
            if (!h) return;
            var done = applyState(h, state);
            if (done) delete handlers[state.operationId];
        });

        result.ready = connection.start()
            .then(function () {
                if (typeof opts.onConnected === 'function') opts.onConnected(true);
                return true;
            })
            .catch(function (err) {
                console.error('ProgressHub connection error (connect):', err);
                if (typeof opts.onConnected === 'function') opts.onConnected(false);
                return false;
            });

        /**
         * Subscribes to an operation's group on the already-open connection.
         * @param {string} operationId
         * @param {object} h  - { spinner, statusEl, cancelBtn, onUpdate, onComplete, onError }
         */
        result.join = function (operationId, h) {
            h = h || {};
            var desc = {
                spinner:    resolve(h.spinner),
                statusEl:   resolve(h.statusEl),
                cancelBtn:  resolve(h.cancelBtn),
                onUpdate:   h.onUpdate,
                onComplete: h.onComplete,
                onError:    h.onError,
                finished:   false
            };

            handlers[operationId] = desc;

            if (desc.spinner) sfSpinner.indeterminate(desc.spinner, true);

            if (desc.cancelBtn) {
                desc.cancelBtn.addEventListener('click', function () {
                    desc.cancelBtn.disabled = true;
                    connection.invoke('ClientCancel', operationId).catch(function (err) {
                        console.error('ProgressHub cancel error:', err);
                    });
                });
            }

            // The connection is (or will shortly be) up; chain off ready so a
            // JoinOperation invoke never races the negotiate handshake.
            var joinReady = result.ready.then(function (ok) {
                if (!ok) return false;
                return connection.invoke('JoinOperation', operationId)
                    .then(function () { return true; })
                    .catch(function (err) {
                        console.error('ProgressHub JoinOperation error:', err);
                        return false;
                    });
            });

            return {
                ready: joinReady,
                stop: function () {
                    delete handlers[operationId];
                    connection.invoke('LeaveOperation', operationId).catch(function () {});
                }
            };
        };

        result.stop = function () {
            try { connection.stop(); } catch (e) { /* no-op */ }
        };

        return result;
    }

    global.sfProgress = {
        track: track,
        connect: connect
    };

}(window));
