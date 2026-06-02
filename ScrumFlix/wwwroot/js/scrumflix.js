/*
 * File:    /wwwroot/js/scrumflix.js
 * Purpose: ScrumFlix client-side UX enhancements.
 *          Cart badge updates, seat hover effects, navbar highlights,
 *          smooth scroll, quantity keyboard support.
 *
 * Phase 3 — P3-UI-7 (sf:themechange seat hover):
 *   FIXED: Seat hover previously applied var(--sf-gold) — a Phase 2 primitive
 *          token that does not exist in the Phase 3 token system.
 *   NOW:   Seat hover reads the current computed value of --sf-color-accent
 *          (and its dim variant) from the <html> element at event time.
 *          This means hover colors automatically update when the theme changes —
 *          no re-binding required.
 *   ADDED: sf:themechange event listener that re-reads CSS token values and
 *          updates any currently-hovered seat immediately on theme switch.
 *
 * EXTENSION API:
 *   refreshCartBadge()  — callable from other scripts (e.g. ConcessionsCatalog AJAX)
 */

'use strict';

(function () {

    // ── Token reader ───────────────────────────────────────────────────────
    // Reads a CSS custom property from the root <html> element at call time.
    // Because sf-theme-switcher.js sets data-theme on <html> synchronously,
    // this always returns the current theme's value — no caching needed.
    function getCssToken(name) {
        return getComputedStyle(document.documentElement)
            .getPropertyValue(name)
            .trim();
    }


    document.addEventListener('DOMContentLoaded', function () {
        // 1. Initialize Cart Badge
        if (typeof refreshCartBadge === 'function') refreshCartBadge();

        // 2. Initialize Movie Catalog Cascading Dropdowns
        initMovieCatalog();
    });

    function initMovieCatalog() {
        const catalogContainer = document.getElementById('movieCatalog');
        const genreDropdown = document.getElementById('genreFilter');
        const movieDropdown = document.getElementById('movieFilter');
        const titleInput = document.getElementById('titleSearch');

        if (!catalogContainer || !genreDropdown || !movieDropdown) return;

        // Retrieve the JSON data from the data-attribute (the "Proper" way)
        const moviesByGenre = JSON.parse(catalogContainer.dataset.movies || '{}');

        function populateMovies(selectedGenre) {
            movieDropdown.innerHTML = '';
            movieDropdown.disabled = true;

            if (!selectedGenre || !moviesByGenre[selectedGenre]) {
                movieDropdown.innerHTML = '<option value="">— Select Genre First —</option>';
                return;
            }

            const placeholder = document.createElement('option');
            placeholder.value = '';
            placeholder.textContent = `— All ${selectedGenre} Movies —`;
            movieDropdown.appendChild(placeholder);

            moviesByGenre[selectedGenre].forEach(m => {
                const opt = document.createElement('option');
                opt.value = m.id;
                opt.textContent = m.name;
                movieDropdown.appendChild(opt);
            });

            movieDropdown.disabled = false;
        }

        genreDropdown.addEventListener('change', function () {
            populateMovies(this.value);

            // Sync chip pills visually
            document.querySelectorAll('.sf-genre-chip').forEach(chip => {
                const href = chip.getAttribute('href');
                const match = href ? href.match(/genre=([^&]*)/) : null;
                const chipGenre = match ? decodeURIComponent(match[1]) : '';
                chip.classList.toggle('active', chipGenre === this.value || (!chipGenre && !this.value));
            });

            if (titleInput) titleInput.value = '';
        });

        movieDropdown.addEventListener('change', function () {
            if (this.value) document.getElementById('cascadeForm').submit();
        });

        // Restore on page load if genre is pre-selected
        if (genreDropdown.value) populateMovies(genreDropdown.value);
    }

    // Expose so ConcessionsCatalog AJAX can call it after adding an item (old js)
    // Uncomment if cart doesn't work: window.refreshCartBadge = refreshCartBadge;

    // ── Seat hover — token-driven ──────────────────────────────────────────
    // Binds hover events to all .sf-seat-open elements.
    // Reads --sf-color-accent at event time so it respects the active theme
    // without needing to re-bind when sf:themechange fires.
    function bindSeatHover() {
        document.querySelectorAll('.sf-seat-open').forEach(function (seat) {
            // Remove any previously bound listeners before re-binding
            // (safe to call multiple times — old anonymous fns are replaced)
            seat.addEventListener('mouseenter', handleSeatEnter);
            seat.addEventListener('mouseleave', handleSeatLeave);
            seat.addEventListener('focus', handleSeatEnter);
            seat.addEventListener('blur', handleSeatLeave);
        });
    }

    function handleSeatEnter() {
        // Read live token values at hover time — automatically correct for current theme
        this.style.background = getCssToken('--sf-color-accent');
        this.style.borderColor = getCssToken('--sf-color-accent-dim');
        this.style.transform = 'scaleY(1.15)';
    }

    function handleSeatLeave() {
        // Clear inline styles — lets the stylesheet .sf-seat-open rule take over again
        this.style.background = '';
        this.style.borderColor = '';
        this.style.transform = '';
    }

    // ── sf:themechange listener ────────────────────────────────────────────
    // When the user switches themes, any seat currently showing a hover highlight
    // (set via inline style) would retain the old theme's color until the mouse
    // moves. This listener clears all active seat highlights immediately so they
    // re-resolve to the new theme's accent color on the next mouseenter.
    window.addEventListener('sf:themechange', function () {
        document.querySelectorAll('.sf-seat-open').forEach(function (seat) {
            // Only clear if an inline style is actually set (i.e. currently hovered)
            if (seat.style.background) {
                seat.style.background = '';
                seat.style.borderColor = '';
                seat.style.transform = '';
            }
        });
        // No need to re-bind events — handleSeatEnter reads the token live each time
    });

    // ── Auto-dismiss flash banners ─────────────────────────────────────────
    function initFlashBanners() {
        // Only auto-dismiss top-level page flash banners (not inline form alerts)
        var flash = document.querySelector('.sf-flash-banner:not(.sf-flash-banner--info):not([role="alert"])');
        if (flash) {
            setTimeout(function () {
                flash.style.transition = 'opacity 0.5s ease';
                flash.style.opacity = '0';
                setTimeout(function () { flash.remove(); }, 500);
            }, 4000);
        }
    }

    // ── Navbar active link highlight ───────────────────────────────────────
    function initNavHighlight() {
        var currentPath = window.location.pathname.toLowerCase();
        document.querySelectorAll('.sf-nav-link').forEach(function (link) {
            var href = (link.getAttribute('href') || '').toLowerCase();
            if (href && href !== '/' && currentPath.startsWith(href)) {
                link.classList.add('active');
                link.setAttribute('aria-current', 'page');
            }
        });
    }

    // ── Smooth scroll for on-page anchors ─────────────────────────────────
    function initSmoothScroll() {
        document.querySelectorAll('a[href^="#"]').forEach(function (anchor) {
            anchor.addEventListener('click', function (e) {
                var target = document.querySelector(this.getAttribute('href'));
                if (target) {
                    e.preventDefault();
                    target.scrollIntoView({ behavior: 'smooth', block: 'start' });
                    // Move focus to the target section for keyboard/screen reader users
                    target.setAttribute('tabindex', '-1');
                    target.focus({ preventScroll: true });
                }
            });
        });
    }

    // ── Quantity input keyboard support ───────────────────────────────────
    function initQtyKeyboard() {
        document.querySelectorAll('.sf-qty-input').forEach(function (input) {
            input.addEventListener('keydown', function (e) {
                var val = parseInt(this.value || '1', 10);
                if (e.key === 'ArrowUp') {
                    e.preventDefault();
                    this.value = Math.min(20, val + 1);
                    this.dispatchEvent(new Event('input'));
                }
                if (e.key === 'ArrowDown') {
                    e.preventDefault();
                    this.value = Math.max(1, val - 1);
                    this.dispatchEvent(new Event('input'));
                }
            });
        });
    }

    // ── Admin sidebar active link ──────────────────────────────────────────
    function initAdminNav() {
        var currentPath = window.location.pathname.toLowerCase();
        document.querySelectorAll('.sf-admin-nav-link').forEach(function (link) {
            var href = (link.getAttribute('href') || '').toLowerCase();
            // Skip in-page anchor links — those are managed by click handlers
            if (href.startsWith('#')) return;
            if (href && href !== '/' && currentPath.startsWith(href)) {
                link.classList.add('active');
                link.setAttribute('aria-current', 'page');
            }
        });
    }


    // ── Init ───────────────────────────────────────────────────────────────
    document.addEventListener('DOMContentLoaded', function () {
        refreshCartBadge();
        bindSeatHover();
        initFlashBanners();
        initNavHighlight();
        initSmoothScroll();
        initQtyKeyboard();
        initAdminNav();
    });

})();
