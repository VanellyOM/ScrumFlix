/* File: /wwwroot/js/login.js
 * Handles:
 *   1. Password visibility toggle
 *   2. Validation error banner — reads ASP.NET summary, shows branded error box.
 *      The hidden #sf-login-summary div is populated by ASP.NET on failed POST;
 *      JS reads its <li> items and surfaces them in the visible #sf-login-errors
 *      banner. This avoids the CSS-only approach that showed an empty box on GET.
 *   3. Input error state — adds .sf-login-input--error to fields that failed
 *      server-side validation, without relying on Bootstrap :invalid or jQuery
 *      Unobtrusive's default class which rendered native browser warning icons.
 */
document.addEventListener('DOMContentLoaded', function () {

    // ── 1. Password toggle ─────────────────────────────────────────────────
    var pwInput   = document.getElementById('passwordInput');
    var pwToggle  = document.getElementById('togglePassword');
    var pwIcon    = document.getElementById('togglePasswordIcon');

    if (pwInput && pwToggle && pwIcon) {
        pwToggle.addEventListener('click', function () {
            var isHidden = pwInput.type === 'password';
            pwInput.type = isHidden ? 'text' : 'password';
            pwIcon.classList.toggle('bi-eye',       !isHidden);
            pwIcon.classList.toggle('bi-eye-slash',  isHidden);
            pwToggle.setAttribute('aria-pressed', String(isHidden));
            pwToggle.setAttribute('aria-label',   isHidden ? 'Hide password' : 'Show password');
        });
    }

    // ── 2. Validation error banner ─────────────────────────────────────────
    var summary   = document.getElementById('sf-login-summary');
    var banner    = document.getElementById('sf-login-errors');
    var errorText = document.getElementById('sf-login-error-text');

    if (summary && banner) {
        // Filter to non-empty messages only — ASP.NET emits an empty <li>
        // on clean GET requests which would otherwise trigger the banner.
        var messages = Array.from(summary.querySelectorAll('li'))
            .map(function (li) { return li.textContent.trim(); })
            .filter(function (t) { return t.length > 0; });

        if (messages.length > 0) {
            if (errorText) {
                errorText.textContent = messages.join(' ');
            }
            banner.style.display = 'flex';
            banner.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        }
    }

    // ── 3. Per-field error highlighting ────────────────────────────────────
    // jQuery Unobtrusive adds .input-validation-error; we map that to our own
    // class so we control the visual — no browser warning icons, no Bootstrap
    // :invalid box-shadow bleeding through.
    function applyFieldErrors() {
        document.querySelectorAll('.input-validation-error').forEach(function (el) {
            el.classList.add('sf-login-input--error');
        });
        // Also suppress jQuery's own error label elements (we use the banner instead)
        document.querySelectorAll('.field-validation-error').forEach(function (el) {
            el.style.display = 'none';
        });
    }

    applyFieldErrors();

    // Re-apply after jQuery validation runs (it fires slightly after DOMContentLoaded)
    setTimeout(applyFieldErrors, 100);

});
