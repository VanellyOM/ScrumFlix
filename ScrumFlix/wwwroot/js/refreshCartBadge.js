/**
		 * Fetches the authoritative cart count from the server and updates the
		 * navbar badge: shows/hides it, updates the visible number, and keeps
		 * the visually-hidden accessibility text intact.
		 */
function refreshCartBadge() {
	fetch('/Cart/GetCartCount')
		.then(function (res) { return res.json(); })
		.then(function (data) {
			var count = Number(data.count) || 0;
			var badge = document.getElementById('cart-count-badge');
			if (!badge) return;

			badge.dataset.cartCount = count;
			badge.innerHTML = count + '<span class="visually-hidden"> items in cart</span>';

			if (count > 0) {
				badge.classList.remove('d-none');
			} else {
				badge.classList.add('d-none');
			}

			badge.setAttribute('aria-label', count + ' items in cart');
		})
		.catch(function (err) {
			console.warn('Cart badge refresh failed:', err);
		});
}

refreshCartBadge();