/**
 * File:    /wwwroot/js/sf-movie-preview.js
 * Purpose: Admin MovieCatalog — fetch the MovieDetailPreview partial and
 *          display it inside a Bootstrap 5 modal.
 *
 * Trigger
 * ───────
 * Any element with  data-movie-preview-url="/Admin/Movies/MovieDetailPreview/42"
 * opens the modal on click. The attribute value is the fetch URL.
 *
 * The modal markup (#sf-movie-preview-modal) is injected into <body> once on
 * first use — no per-page HTML needed in the view.
 *
 * Loading state
 * ─────────────
 * While the fetch is in flight the modal body shows a centred spinner (reuses
 * the existing .sf-spinner--indeterminate component already on the page).
 * On success the spinner is replaced with the partial HTML.
 * On failure an inline error message is shown.
 *
 * CSP compliance: no inline scripts or event handler attributes.
 * Loaded per-page via @section Scripts in MovieCatalog.cshtml.
 *
 * Requires: Bootstrap 5 (global), sf-spinner.js (global via _AdminLayout)
 */

(function () {
    'use strict';

    var MODAL_ID      = 'sf-movie-preview-modal';
    var MODAL_BODY_ID = 'sf-movie-preview-body';
    var MODAL_TITLE_ID = 'sf-movie-preview-label';

    // ── Build and inject the modal shell once ──────────────────────────────
    function ensureModal() {
        if (document.getElementById(MODAL_ID)) return;

        var html = [
            '<div class="modal fade" id="' + MODAL_ID + '" tabindex="-1"',
            '     aria-labelledby="' + MODAL_TITLE_ID + '" aria-modal="true" role="dialog">',
            '  <div class="modal-dialog modal-xl modal-dialog-scrollable">',
            '    <div class="modal-content sf-modal-content">',
            '      <div class="modal-header sf-modal-header">',
            '        <h2 class="modal-title fs-5" id="' + MODAL_TITLE_ID + '">Movie Preview</h2>',
            '        <div class="d-flex align-items-center gap-2 ms-auto">',
            '          <span class="badge bg-secondary" style="font-size:.7rem;">Staff Preview</span>',
            '          <button type="button" class="btn-close btn-close-white"',
            '                  data-bs-dismiss="modal" aria-label="Close preview"></button>',
            '        </div>',
            '      </div>',
            '      <div class="modal-body p-0" id="' + MODAL_BODY_ID + '">',
            '        ' + loadingHtml(),
            '      </div>',
            '    </div>',
            '  </div>',
            '</div>'
        ].join('\n');

        var wrapper = document.createElement('div');
        wrapper.innerHTML = html;
        document.body.appendChild(wrapper.firstElementChild);
    }

    // Loading state HTML — reuses sf-spinner CSS classes
    function loadingHtml() {
        return [
            '<div class="d-flex flex-column align-items-center justify-content-center py-5 gap-3"',
            '     style="min-height:320px;">',
            '  <div class="sf-spinner sf-spinner--lg sf-spinner--indeterminate"',
            '       role="progressbar" aria-valuemin="0" aria-valuemax="100" aria-valuenow="0"',
            '       aria-label="Loading movie preview">',
            '    <svg class="sf-spinner__svg" viewBox="0 0 100 100" aria-hidden="true">',
            '      <circle class="sf-spinner__track" cx="50" cy="50" r="45"></circle>',
            '      <circle class="sf-spinner__ring"  cx="50" cy="50" r="45"></circle>',
            '    </svg>',
            '  </div>',
            '  <p class="sf-spinner__status">Loading preview\u2026</p>',
            '</div>'
        ].join('\n');
    }

    // Error state HTML
    function errorHtml(message) {
        return [
            '<div class="d-flex flex-column align-items-center justify-content-center py-5 gap-3"',
            '     style="min-height:320px;">',
            '  <i class="bi bi-exclamation-circle fs-1" style="color:var(--sf-color-danger)"></i>',
            '  <p class="sf-text-muted">' + (message || 'Could not load preview.') + '</p>',
            '</div>'
        ].join('\n');
    }

    // ── Fetch partial and populate modal ──────────────────────────────────
    function openPreview(url, title) {
        ensureModal();

        var modalEl  = document.getElementById(MODAL_ID);
        var bodyEl   = document.getElementById(MODAL_BODY_ID);
        var titleEl  = document.getElementById(MODAL_TITLE_ID);

        if (!modalEl || !bodyEl) return;

        // Update title optimistically (will be refined once partial loads)
        if (titleEl) titleEl.textContent = title || 'Movie Preview';

        // Show loading state
        bodyEl.innerHTML = loadingHtml();

        // Show the modal immediately (don't wait for fetch)
        var bsModal = bootstrap.Modal.getOrCreateInstance(modalEl, {
            backdrop: true,
            keyboard: true,
            focus:    true
        });
        bsModal.show();

        // Fetch the partial
        fetch(url, {
            method:  'GET',
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
        .then(function (res) {
            if (!res.ok) throw new Error('HTTP ' + res.status);
            return res.text();
        })
        .then(function (html) {
            bodyEl.innerHTML = html;

            // Update modal title from the rendered h1 if present
            var h1 = bodyEl.querySelector('.sf-md-title');
            if (h1 && titleEl) titleEl.textContent = h1.textContent.trim();
        })
        .catch(function (err) {
            console.error('sf-movie-preview: fetch failed', err);
            bodyEl.innerHTML = errorHtml('Preview could not be loaded. Try opening the full page.');
        });
    }

    // ── Wire all preview triggers on the page ─────────────────────────────
    function wireTriggers() {
        document.querySelectorAll('[data-movie-preview-url]').forEach(function (el) {
            el.addEventListener('click', function (e) {
                e.preventDefault();
                var url   = el.getAttribute('data-movie-preview-url');
                var title = el.getAttribute('data-movie-preview-title') || 'Movie Preview';
                openPreview(url, title);
            });
        });
    }

    // ── Init ──────────────────────────────────────────────────────────────
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', wireTriggers);
    } else {
        wireTriggers();
    }

}());
