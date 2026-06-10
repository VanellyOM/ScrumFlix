/**
 * File:    /wwwroot/js/sf-spinner.js
 * Purpose: Controller for the .sf-spinner circular progress component.
 *
 * Exposes a single global object: window.sfSpinner
 *
 * API
 * ───
 *   sfSpinner.update(el, percent, message?)
 *     Update a spinner to a specific percentage (0–100).
 *     el       — the .sf-spinner wrapper element (or a CSS selector string)
 *     percent  — integer 0–100 (clamped automatically)
 *     message  — optional string for .sf-spinner__status below the spinner
 *
 *   sfSpinner.complete(el, message?)
 *     Transition the spinner to the complete/success state.
 *     Replaces the SVG with a themed checkmark, stops all animation.
 *     message defaults to "Complete!"
 *
 *   sfSpinner.error(el, message?)
 *     Transition the spinner to the error state (ring turns to danger color).
 *     message defaults to "Something went wrong."
 *
 *   sfSpinner.reset(el)
 *     Return the spinner to its initial 0% state, ready to run again.
 *
 *   sfSpinner.indeterminate(el, on)
 *     Toggle indeterminate (chasing) animation.
 *     on = true  → indeterminate mode (no real progress data yet)
 *     on = false → back to determinate mode
 *
 *   sfSpinner.fromSignalR(el, hubConnection, eventName, message?)
 *     Convenience helper: wires a SignalR hub event directly to sfSpinner.update.
 *     The hub event must emit { percent: Number, message?: String }.
 *     Example:
 *       sfSpinner.fromSignalR(el, connection, 'TmdbSyncProgress');
 *
 * CSP compliance
 * ──────────────
 *   No inline event handlers. All event binding done in JS.
 *   Loaded via <script src="~/js/sf-spinner.js" asp-append-version="true"></script>
 *   in the relevant view's @section Scripts block.
 *
 * SVG circumference
 * ─────────────────
 *   r = 45  →  C = 2π × 45 ≈ 282.74
 *   stroke-dasharray: 282.74
 *   stroke-dashoffset = 282.74 - (282.74 × pct / 100)
 *   0%   → offset 282.74 (ring fully hidden)
 *   100% → offset 0      (ring fully drawn)
 */

(function (global) {
    'use strict';

    var CIRCUMFERENCE = 282.74;

    function resolve(el) {
        if (typeof el === 'string') {
            return document.querySelector(el);
        }
        return el;
    }

    function getRing(el) {
        return el.querySelector('.sf-spinner__ring');
    }

    function getPct(el) {
        return el.querySelector('.sf-spinner__pct');
    }

    function getStatus(el) {
        // .sf-spinner__status may be a sibling, not a child
        return el.parentElement
            ? el.parentElement.querySelector('.sf-spinner__status')
            : null;
    }

    function setAriaValue(el, pct) {
        el.setAttribute('aria-valuenow', pct);
    }

    /**
     * Update the spinner ring and percentage label.
     * @param {Element|string} el      - .sf-spinner wrapper or CSS selector
     * @param {number}         percent - 0 to 100 (clamped)
     * @param {string}        [msg]    - optional status message
     */
    function update(el, percent, msg) {
        el = resolve(el);
        if (!el) return;

        var clamped = Math.min(Math.max(Math.round(percent), 0), 100);
        var offset  = CIRCUMFERENCE - (CIRCUMFERENCE * clamped / 100);

        var ring = getRing(el);
        if (ring) ring.style.strokeDashoffset = offset;

        var pctEl = getPct(el);
        if (pctEl) pctEl.textContent = clamped + '%';

        setAriaValue(el, clamped);

        var statusEl = getStatus(el);
        if (statusEl && msg !== undefined) statusEl.textContent = msg;
    }

    /**
     * Transition to complete (success) state.
     * @param {Element|string} el   - .sf-spinner wrapper or CSS selector
     * @param {string}        [msg] - status message, default "Complete!"
     */
    function complete(el, msg) {
        el = resolve(el);
        if (!el) return;

        update(el, 100);
        el.classList.add('sf-spinner--complete');
        el.classList.remove('sf-spinner--error', 'sf-spinner--indeterminate');

        // Inject the checkmark icon if not already there
        if (!el.querySelector('.sf-spinner__check')) {
            var check = document.createElement('span');
            check.className = 'sf-spinner__check';
            check.setAttribute('aria-hidden', 'true');
            // Bootstrap Icon — bi-check-lg
            check.innerHTML = '<i class="bi bi-check-lg"></i>';
            el.appendChild(check);
        }

        setAriaValue(el, 100);

        var statusEl = getStatus(el);
        if (statusEl) statusEl.textContent = msg !== undefined ? msg : 'Complete!';
    }

    /**
     * Transition to error state (ring color → danger, optional message).
     * @param {Element|string} el   - .sf-spinner wrapper or CSS selector
     * @param {string}        [msg] - status message, default "Something went wrong."
     */
    function error(el, msg) {
        el = resolve(el);
        if (!el) return;

        el.classList.add('sf-spinner--error');
        el.classList.remove('sf-spinner--complete', 'sf-spinner--indeterminate');

        var statusEl = getStatus(el);
        if (statusEl) statusEl.textContent = msg !== undefined ? msg : 'Something went wrong.';
    }

    /**
     * Reset to initial empty state (0%, no completion markup).
     * @param {Element|string} el - .sf-spinner wrapper or CSS selector
     */
    function reset(el) {
        el = resolve(el);
        if (!el) return;

        el.classList.remove(
            'sf-spinner--complete',
            'sf-spinner--error',
            'sf-spinner--indeterminate'
        );

        // Remove injected checkmark if present
        var check = el.querySelector('.sf-spinner__check');
        if (check) check.remove();

        var ring = getRing(el);
        if (ring) ring.style.strokeDashoffset = CIRCUMFERENCE;

        var pctEl = getPct(el);
        if (pctEl) pctEl.textContent = '0%';

        setAriaValue(el, 0);

        var statusEl = getStatus(el);
        if (statusEl) statusEl.textContent = '';
    }

    /**
     * Toggle indeterminate (chasing) animation mode.
     * Use when you're waiting for the first real progress event.
     * @param {Element|string} el - .sf-spinner wrapper or CSS selector
     * @param {boolean}        on - true to enter indeterminate, false to exit
     */
    function indeterminate(el, on) {
        el = resolve(el);
        if (!el) return;

        if (on) {
            el.classList.add('sf-spinner--indeterminate');
            el.classList.remove('sf-spinner--complete', 'sf-spinner--error');
        } else {
            el.classList.remove('sf-spinner--indeterminate');
        }
    }

    /**
     * Wire a SignalR hub event directly to the spinner.
     * The hub event must emit an object: { percent: Number, message?: String }
     *
     * @param {Element|string} el        - .sf-spinner wrapper or CSS selector
     * @param {object}         hubConn   - SignalR HubConnection instance
     * @param {string}         eventName - hub event name (e.g. 'TmdbSyncProgress')
     * @param {string}        [doneMsg]  - message shown on 100%, default "Complete!"
     *
     * Example:
     *   sfSpinner.fromSignalR('#tmdb-spinner', connection, 'TmdbSyncProgress');
     *
     * The server-side Hub should broadcast:
     *   await Clients.Caller.SendAsync("TmdbSyncProgress", new { percent = 42, message = "Syncing…" });
     */
    function fromSignalR(el, hubConn, eventName, doneMsg) {
        el = resolve(el);
        if (!el || !hubConn) return;

        // Start indeterminate until first event arrives
        indeterminate(el, true);

        hubConn.on(eventName, function (data) {
            var pct = (data && typeof data.percent === 'number') ? data.percent : 0;
            var msg = (data && data.message)                     ? data.message : undefined;

            indeterminate(el, false);

            if (pct >= 100) {
                complete(el, doneMsg);
            } else {
                update(el, pct, msg);
            }
        });
    }

    /**
     * Convenience: drive spinner from a POST form that returns progress via
     * fetch + ReadableStream (NDJSON). Each line must be valid JSON:
     *   {"percent": 42, "message": "Processing…"}
     *
     * @param {Element|string} el      - .sf-spinner wrapper or CSS selector
     * @param {string}         url     - fetch endpoint
     * @param {object}        [opts]   - fetch options (body, headers, etc.)
     * @param {string}        [doneMsg]
     */
    function fromStream(el, url, opts, doneMsg) {
        el = resolve(el);
        if (!el) return;

        indeterminate(el, true);

        var fetchOpts = Object.assign({
            method: 'POST',
            headers: { 'Accept': 'application/x-ndjson' }
        }, opts || {});

        fetch(url, fetchOpts)
            .then(function (res) {
                if (!res.ok || !res.body) {
                    error(el, 'Server error.');
                    return;
                }
                indeterminate(el, false);
                var reader  = res.body.getReader();
                var decoder = new TextDecoder();
                var buffer  = '';

                function read() {
                    reader.read().then(function (chunk) {
                        if (chunk.done) return;
                        buffer += decoder.decode(chunk.value, { stream: true });
                        var lines = buffer.split('\n');
                        buffer = lines.pop();

                        lines.forEach(function (line) {
                            line = line.trim();
                            if (!line) return;
                            try {
                                var data = JSON.parse(line);
                                if (data.percent >= 100) {
                                    complete(el, doneMsg);
                                } else {
                                    update(el, data.percent, data.message);
                                }
                            } catch (e) { /* malformed line — skip */ }
                        });
                        read();
                    }).catch(function () { error(el); });
                }
                read();
            })
            .catch(function () { error(el); });
    }

    // ── Public API ────────────────────────────────────────────────────────
    global.sfSpinner = {
        update:       update,
        complete:     complete,
        error:        error,
        reset:        reset,
        indeterminate: indeterminate,
        fromSignalR:  fromSignalR,
        fromStream:   fromStream,
        CIRCUMFERENCE: CIRCUMFERENCE
    };

}(window));
