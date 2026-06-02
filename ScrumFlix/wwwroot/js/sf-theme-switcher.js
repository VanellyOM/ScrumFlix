/**
 * File:    /wwwroot/js/sf-theme-switcher.js
 * Purpose: ScrumFlix theme switcher — hybrid Cookie + LocalStorage persistence.
 *
 * THEMES:  'dark' (default) | 'light' | 'red'
 *
 * STORAGE STRATEGY (hybrid):
 *   - LocalStorage  → fast, synchronous read on page load (no flash)
 *   - Cookie        → server-readable (ASP.NET can pre-render correct theme class
 *                     server-side, eliminating FOUC entirely if wired to _Layout.cshtml)
 *   Both are written on every theme change and read on init with LocalStorage preferred.
 *
 * WCAG 2.2 AA:
 *   - Toggle button has aria-label that updates on change
 *   - Picker items use role="menuitem" + aria-checked
 *   - Theme change announced via aria-live region
 *   - No color change without text/icon backup signal
 *
 * USAGE (auto-init on DOMContentLoaded):
 *   Include this script AFTER sf-tokens.css and AFTER Bootstrap JS bundle.
 *   <script src="~/js/sf-theme-switcher.js" asp-append-version="true"></script>
 *
 * MANUAL API (window.ScrumFlixTheme):
 *   window.ScrumFlixTheme.set('dark' | 'light' | 'red')
 *   window.ScrumFlixTheme.get()   → string
 *   window.ScrumFlixTheme.cycle() → cycles dark → light → red → dark
 */

'use strict';

(function () {

    /* ── Constants ──────────────────────────────────────────────────────── */
    const STORAGE_KEY = 'sf-theme';
    const COOKIE_NAME = 'sf-theme';
    const COOKIE_DAYS = 365;
    const ATTR = 'data-theme';
    const DEFAULT_THEME = 'dark';
    const VALID_THEMES = ['dark', 'light', 'red'];

    const THEME_META = {
        dark: { label: 'Dark', icon: 'bi-moon-stars-fill', swatch: '🎬' },
        light: { label: 'Light', icon: 'bi-sun-fill', swatch: '☀️' },
        red: { label: 'Red & Black', icon: 'bi-fire', swatch: '🔴' },
    };

    /* ── Cookie helpers ─────────────────────────────────────────────────── */

    /**
     * Write a cookie readable by both JS and ASP.NET server middleware.
     * SameSite=Lax is sufficient; no sensitive data.
     */
    function setCookie(name, value, days) {
        const expires = new Date(Date.now() + days * 864e5).toUTCString();
        document.cookie =
            encodeURIComponent(name) + '=' + encodeURIComponent(value) +
            '; expires=' + expires +
            '; path=/' +
            '; SameSite=Lax';
    }

    function getCookie(name) {
        const key = encodeURIComponent(name) + '=';
        const parts = document.cookie.split('; ');
        for (const part of parts) {
            if (part.startsWith(key)) {
                return decodeURIComponent(part.slice(key.length));
            }
        }
        return null;
    }

    /* ── Storage read / write ───────────────────────────────────────────── */

    function readStoredTheme() {
        // LocalStorage is preferred (synchronous, no network, no size limit)
        try {
            const ls = localStorage.getItem(STORAGE_KEY);
            if (ls && VALID_THEMES.includes(ls)) return ls;
        } catch (_) { /* private browsing may throw */ }

        // Cookie fallback (server-set or previously written)
        const cookie = getCookie(COOKIE_NAME);
        if (cookie && VALID_THEMES.includes(cookie)) return cookie;

        // OS preference fallback
        if (window.matchMedia && window.matchMedia('(prefers-color-scheme: light)').matches) {
            return 'light';
        }

        return DEFAULT_THEME;
    }

    function writeStoredTheme(theme) {
        // Write both stores so each is current
        try {
            localStorage.setItem(STORAGE_KEY, theme);
        } catch (_) { /* ignore in restricted environments */ }

        setCookie(COOKIE_NAME, theme, COOKIE_DAYS);
    }

    /* ── DOM application ────────────────────────────────────────────────── */

    /**
     * Apply theme to <html> element. Called both on init and on user toggle.
     * Fires a CustomEvent so other scripts can react (e.g., chart re-theming).
     */
    function applyTheme(theme, announce) {
        if (!VALID_THEMES.includes(theme)) theme = DEFAULT_THEME;

        const html = document.documentElement;
        const prev = html.getAttribute(ATTR);

        html.setAttribute(ATTR, theme);

        // Update toggle button(s)
        updateToggleButtons(theme);

        // Update picker checkmarks
        updatePickerItems(theme);

        // Announce to screen readers if theme changed and caller requests it
        if (announce && prev !== theme) {
            announceThemeChange(theme);
        }

        // Dispatch event for other components (charts, maps, etc.)
        try {
            window.dispatchEvent(new CustomEvent('sf:themechange', {
                detail: { theme, prev },
                bubbles: false,
            }));
        } catch (_) { /* IE11 fallback not needed — Bootstrap 5.3 already dropped IE */ }
    }

    /* ── Toggle button UI ───────────────────────────────────────────────── */

    function updateToggleButtons(theme) {
        const meta = THEME_META[theme];

        document.querySelectorAll('.sf-theme-toggle').forEach(btn => {
            // Update accessible label
            btn.setAttribute('aria-label', 'Theme: ' + meta.label + '. Click to change.');
            btn.setAttribute('title', 'Current theme: ' + meta.label);

            // Swap icon — clear all, activate current
            btn.querySelectorAll('[data-theme-icon]').forEach(icon => {
                const active = icon.dataset.themeIcon === theme;
                icon.classList.toggle('sf-icon-active', active);
                icon.setAttribute('aria-hidden', 'true');
            });
        });
    }

    function updatePickerItems(theme) {
        document.querySelectorAll('.sf-theme-picker-item').forEach(item => {
            const isActive = item.dataset.themeValue === theme;
            item.setAttribute('aria-checked', String(isActive));
            item.setAttribute('aria-pressed', String(isActive));
        });
    }

    /* ── Accessibility announcement ─────────────────────────────────────── */

    function getOrCreateAnnouncer() {
        let el = document.getElementById('sf-theme-announcer');
        if (!el) {
            el = document.createElement('div');
            el.id = 'sf-theme-announcer';
            el.setAttribute('aria-live', 'polite');
            el.setAttribute('aria-atomic', 'true');
            el.setAttribute('role', 'status');
            // Visually hidden
            Object.assign(el.style, {
                position: 'absolute',
                width: '1px',
                height: '1px',
                padding: '0',
                overflow: 'hidden',
                clip: 'rect(0,0,0,0)',
                whiteSpace: 'nowrap',
                border: '0',
            });
            document.body.appendChild(el);
        }
        return el;
    }

    function announceThemeChange(theme) {
        const announcer = getOrCreateAnnouncer();
        // Clear then set — ensures repeated same-value changes are announced
        announcer.textContent = '';
        requestAnimationFrame(() => {
            announcer.textContent = THEME_META[theme].label + ' theme applied.';
        });
    }

    /* ── Picker dropdown ────────────────────────────────────────────────── */

    function openPicker(btn) {
        const picker = btn.querySelector('.sf-theme-picker') ||
            btn.parentElement.querySelector('.sf-theme-picker');
        if (!picker) return;

        const isOpen = picker.classList.contains('sf-theme-picker--open');
        closeAllPickers();

        if (!isOpen) {
            picker.classList.add('sf-theme-picker--open');
            btn.setAttribute('aria-expanded', 'true');

            // Focus first item
            const first = picker.querySelector('.sf-theme-picker-item');
            if (first) first.focus();

            // Close on outside click
            document.addEventListener('click', outsideClickHandler, { once: false });
        }
    }

    function closeAllPickers() {
        document.querySelectorAll('.sf-theme-picker--open').forEach(p => {
            p.classList.remove('sf-theme-picker--open');
            const btn = p.closest('.sf-theme-toggle') ||
                p.parentElement.querySelector('.sf-theme-toggle');
            if (btn) btn.setAttribute('aria-expanded', 'false');
        });
        document.removeEventListener('click', outsideClickHandler);
    }

    function outsideClickHandler(e) {
        if (!e.target.closest('.sf-theme-toggle') &&
            !e.target.closest('.sf-theme-picker')) {
            closeAllPickers();
        }
    }

    /* ── Public API ─────────────────────────────────────────────────────── */

    const ThemeAPI = {
        /**
         * Set theme, persist, and apply to DOM.
         * @param {'dark'|'light'|'red'} theme
         */
        set(theme) {
            if (!VALID_THEMES.includes(theme)) {
                console.warn('[ScrumFlixTheme] Unknown theme:', theme, '— ignoring.');
                return;
            }
            writeStoredTheme(theme);
            applyTheme(theme, true);
            closeAllPickers();
        },

        /** Returns current active theme string. */
        get() {
            return document.documentElement.getAttribute(ATTR) || DEFAULT_THEME;
        },

        /** Cycles through: dark → light → red → dark */
        cycle() {
            const current = this.get();
            const idx = VALID_THEMES.indexOf(current);
            const next = VALID_THEMES[(idx + 1) % VALID_THEMES.length];
            this.set(next);
        },
    };

    // Expose on window
    window.ScrumFlixTheme = ThemeAPI;

    /* ── Eliminate FOUC: apply theme BEFORE paint ───────────────────────── */
    // This block runs synchronously during script parse — before DOMContentLoaded.
    // It must stay inside the IIFE and NOT wait for any event.
    (function applyImmediate() {
        const theme = readStoredTheme();
        document.documentElement.setAttribute(ATTR, theme);
    })();

    /* ── Wire up interactive controls after DOM is ready ───────────────── */

    function init() {
        const currentTheme = readStoredTheme();

        // Sync DOM attribute (may have been set by server-side Razor logic)
        applyTheme(currentTheme, false);

        // ── Bind toggle buttons ────────────────────────────────────────────
        document.querySelectorAll('.sf-theme-toggle').forEach(btn => {

            const hasPicker = btn.querySelector('.sf-theme-picker');

            // Helper function to handle the actual activation
            const handleActivation = (e) => {
                if (hasPicker) {
                    e.stopPropagation();
                    openPicker(btn);
                } else {
                    ThemeAPI.cycle();
                }
            };

            // 1. Mouse Click
            btn.addEventListener('click', handleActivation);

            // 2. Keyboard Activation (NEW: Necessary because it's now a <div>)
            btn.addEventListener('keydown', function (e) {
                if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault(); // Prevent page scroll on Space
                    handleActivation(e);
                }

                if (e.key === 'Escape') {
                    closeAllPickers();
                    this.focus();
                }
            });

            // Set initial aria state
            btn.setAttribute('aria-expanded', 'false');
            if (hasPicker) {
                btn.setAttribute('aria-haspopup', 'true');
            }
        });


        // ── Bind picker items ──────────────────────────────────────────────
        document.querySelectorAll('.sf-theme-picker-item').forEach(item => {
            item.setAttribute('role', 'menuitemradio');

            item.addEventListener('click', function (e) {
                e.stopPropagation();
                const theme = this.dataset.themeValue;
                if (theme) ThemeAPI.set(theme);
            });

            // Keyboard navigation within picker
            item.addEventListener('keydown', function (e) {
                const items = Array.from(
                    this.closest('.sf-theme-picker').querySelectorAll('.sf-theme-picker-item')
                );
                const idx = items.indexOf(this);

                if (e.key === 'ArrowDown') {
                    e.preventDefault();
                    items[(idx + 1) % items.length].focus();
                } else if (e.key === 'ArrowUp') {
                    e.preventDefault();
                    items[(idx - 1 + items.length) % items.length].focus();
                } else if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    this.click();
                } else if (e.key === 'Escape') {
                    closeAllPickers();
                    const toggle = this.closest('.sf-theme-toggle') ||
                        this.closest('[aria-haspopup]');
                    if (toggle) toggle.focus();
                }
            });
        });

        // ── OS colour-scheme preference listener ──────────────────────────
        // Only applies if user has NOT explicitly set a theme preference.
        if (window.matchMedia) {
            window.matchMedia('(prefers-color-scheme: light)').addEventListener('change', e => {
                // Respect stored preference — only auto-switch if nothing stored
                try {
                    const stored = localStorage.getItem(STORAGE_KEY);
                    if (!stored) {
                        ThemeAPI.set(e.matches ? 'light' : 'dark');
                    }
                } catch (_) { /* ignore */ }
            });
        }
    }

    // Run init after DOM is parsed
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

})();
