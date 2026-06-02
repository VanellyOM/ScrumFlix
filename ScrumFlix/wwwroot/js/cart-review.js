/*
 * File: /wwwroot/js/cart-review.js
 * Description:
 *     Handles quantity controls and confirmation prompts for CartReview.
 *     Replaces the inline submitQty() function, onchange="this.form.submit()",
 *     and onsubmit="return confirm(...)" handlers that violate CSP.
 */

(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {

        // Quantity adjustment buttons (+1 / -1)
        document.querySelectorAll('[data-qty-action]').forEach(function (button) {

            button.addEventListener('click', function () {

                var delta = parseInt(button.dataset.qtyAction);
                var form = button.closest('form');

                if (!form) {
                    return;
                }

                var input = form.querySelector('input[name="quantity"]');

                if (!input) {
                    return;
                }

                var current = parseInt(input.value || '1');
                var next = Math.max(0, Math.min(20, current + delta));

                input.value = next;
                form.submit();
            });
        });

        // Auto-submit when quantity input value changes
        document.querySelectorAll('[data-cart-qty-input]').forEach(function (input) {

            input.addEventListener('change', function () {
                var form = input.closest('form');

                if (form) {
                    form.submit();
                }
            });
        });

        // Clear cart confirmation
        document.querySelectorAll('[data-clear-cart-form]').forEach(function (form) {

            form.addEventListener('submit', function (e) {

                var confirmed = window.confirm('Clear your entire cart?');

                if (!confirmed) {
                    e.preventDefault();
                }
            });
        });
    });
})();
