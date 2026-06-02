/*
 * File: /wwwroot/js/account-change-password.js
 * Description:
 *     Handles password visibility toggles and validation summary visibility
 *     for the ChangePassword view.
 */

(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {

        // Password visibility toggles
        document.querySelectorAll('.sf-pw-toggle').forEach(function (btn) {
            btn.addEventListener('click', function () {

                var targetId = btn.getAttribute('data-target');
                var input = document.getElementById(targetId);
                var icon = btn.querySelector('i');

                if (!input || !icon) {
                    return;
                }

                var isPassword = input.type === 'password';

                input.type = isPassword ? 'text' : 'password';

                icon.className = isPassword
                    ? 'bi bi-eye-slash'
                    : 'bi bi-eye';

                btn.setAttribute(
                    'aria-label',
                    isPassword ? 'Hide password' : 'Show password'
                );

                btn.setAttribute('aria-pressed', String(isPassword));
            });
        });

        // Validation summary visibility
        var summary = document.querySelector('[data-valmsg-summary]');

        if (summary && summary.querySelectorAll('li').length > 0) {
            summary.style.removeProperty('display');
        }
    });
})();
