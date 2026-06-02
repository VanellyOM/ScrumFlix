/*
 * File: /wwwroot/js/booking.js
 *
 * Quantity stepper and live price total for ShowtimeBooking.
 *
 * Loaded via <script src="~/js/booking.js"> which satisfies CSP script-src 'self'.
 * No inline script block needed in the view — eliminates the CSP violation that
 * occurs when a <script> block appears directly in a Razor view without a nonce
 * or hash in the Content-Security-Policy header.
 *
 * Expects these elements in the DOM:
 *   #qtyInput     — <input type="number"> bound to Quantity (asp-for)
 *   #btnMinus     — decrement button (type="button")  ← IDs now present in view
 *   #btnPlus      — increment button (type="button")  ← IDs now present in view
 *   #lineTotal    — <span> showing the running total
 *   #priceDisplay — <span data-price="12.00"> holding the per-ticket unit price
 *
 * Falls back gracefully: if any element is absent the script exits without error.
 *
 * Exposes:
 *   window.getQty() — returns the current clamped quantity integer.
 *                     Used by seatPicker.js to enforce the seat-selection cap
 *                     without coupling the two scripts via a shared variable.
 *
 * Fires on #qtyInput:
 *   CustomEvent "qtychanged" (bubbles: false) — dispatched after every quantity
 *   update so seatPicker.js can trim excess seat selections when quantity decreases.
 */
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {

        var input   = document.getElementById('qtyInput');
        var minus   = document.getElementById('btnMinus');
        var plus    = document.getElementById('btnPlus');
        var total   = document.getElementById('lineTotal');
        var priceEl = document.getElementById('priceDisplay');

        // Exit cleanly if required elements are missing
        // (e.g. sold-out state where the input is not rendered)
        if (!input || !priceEl) return;

        var price  = parseFloat(priceEl.dataset.price) || 0;
        var maxQty = parseInt(input.getAttribute('max'), 10) || 20;
        var minQty = parseInt(input.getAttribute('min'), 10) || 1;

        function clamp(n) {
            return Math.max(minQty, Math.min(maxQty, n));
        }

        // Exposed so seatPicker.js can read the live quantity without coupling
        // to internal state. Assigned before updateTotal is first called.
        window.getQty = function () {
            return clamp(parseInt(input.value, 10) || minQty);
        };

        function updateTotal() {
            var qty = window.getQty();
            input.value = qty;

            if (total) {
                total.textContent = '$' + (price * qty).toFixed(2);
            }

            // Notify seatPicker.js (or any other listener) that quantity changed.
            // seatPicker.js uses this to trim selected seats when qty decreases.
            input.dispatchEvent(new CustomEvent('qtychanged', { bubbles: false }));
        }

        if (minus) {
            minus.addEventListener('click', function () {
                input.value = clamp((parseInt(input.value, 10) || minQty) - 1);
                updateTotal();
            });
        }

        if (plus) {
            plus.addEventListener('click', function () {
                input.value = clamp((parseInt(input.value, 10) || minQty) + 1);
                updateTotal();
            });
        }

        input.addEventListener('input',  updateTotal);
        input.addEventListener('change', updateTotal);

        // Sync display on first load
        updateTotal();
    });
}());
