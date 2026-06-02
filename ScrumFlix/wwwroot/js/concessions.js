// site.js or concessions.js
(function () {
    // 1. Toast Notification Logic
    function showToast(message, isError) {
        var region = document.getElementById('sf-toast-region');
        if (!region) return;

        var t = document.createElement('div');
        // Uses classes defined in your CSS/CSHTML Phase 3 updates [cite: 2, 43]
        t.className = isError
            ? 'sf-flash-banner sf-flash-banner--error'
            : 'sf-flash-banner sf-flash-banner--success';

        t.style.cssText = 'pointer-events:auto; margin-top:.5rem; max-width:280px; opacity:1; transition:opacity .3s;';
        t.setAttribute('role', 'status');
        t.textContent = message;

        region.appendChild(t);
        setTimeout(function () {
            t.style.opacity = '0';
            setTimeout(function () { t.remove(); }, 320);
        }, 2200);
    }

    // 2. Quantity Button Logic (Fixes CSP script-src-attr errors) 
    document.addEventListener('click', function (e) {
        if (e.target.classList.contains('js-qty-btn')) {
            var container = e.target.closest('.sf-qty-control');
            var input = container.querySelector('.sf-qty-input');
            var val = parseInt(input.value) || 1;

            if (e.target.classList.contains('js-qty-minus')) {
                input.value = Math.max(1, val - 1);
            } else if (e.target.classList.contains('js-qty-plus')) {
                input.value = Math.min(20, val + 1);
            }
        }
    });

    // 3. AJAX Add-to-Cart Logic [cite: 46, 47]
    document.addEventListener('submit', async function (e) {
        var form = e.target.closest('form.js-add-concession');
        if (!form) return;

        e.preventDefault();
        var submitBtn = form.querySelector('button[type="submit"]');
        if (submitBtn) submitBtn.disabled = true;

        try {
            var resp = await fetch(form.action, {
                method: 'POST',
                credentials: 'same-origin',
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                body: new FormData(form)
            });

            if (resp.ok) {
                var data = await resp.json();
                // success/message keys from the backend response [cite: 49]
                showToast(data.message || 'Added to cart!', !data.success);
                if (data.success && typeof refreshCartBadge === 'function') {
                    refreshCartBadge();
                }
            } else {
                showToast('Could not add to cart. Please try again.', true);
            }
        } catch (err) {
            showToast('Network error — please try again.', true);
            console.error('AddConcession error:', err);
        } finally {
            if (submitBtn) submitBtn.disabled = false;
        }
    });
})();