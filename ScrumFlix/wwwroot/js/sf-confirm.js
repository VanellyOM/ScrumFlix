/**
 * File:    /wwwroot/js/sf-confirm.js
 * Purpose: CSP-safe delete/destructive-action confirmation.
 *          Intercepts any form with class="sf-confirm-form" and data-confirm="…"
 *          and shows a native confirm() dialog before allowing submission.
 *
 * Usage in a view:
 *   <form method="post" class="sf-confirm-form" data-confirm="Delete this? Cannot be undone.">
 *
 * Loaded globally by _AdminLayout — no per-page script needed.
 * Element-guarded: safe to load on pages with no confirm forms.
 */
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('form.sf-confirm-form').forEach(function (form) {
            form.addEventListener('submit', function (e) {
                var msg = form.dataset.confirm || 'Are you sure?';
                if (!window.confirm(msg)) e.preventDefault();
            });
        });
    });

}());
