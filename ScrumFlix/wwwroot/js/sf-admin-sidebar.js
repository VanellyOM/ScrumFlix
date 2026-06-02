/**
 * File:    /wwwroot/js/sf-admin-sidebar.js
 * Purpose: Admin sidebar collapse/expand toggle with localStorage persistence.
 *
 * Replaces the inline <script> block formerly in _AdminSidebar.cshtml.
 * Moved to an external file to comply with the CSP script-src directive,
 * which does not include 'unsafe-inline'.
 *
 * Behaviour:
 *   - On load: restores the saved open/collapsed state from localStorage.
 *   - Default state: open on viewports >= 992px (lg), collapsed on mobile.
 *   - On click: toggles the 'sf-sidebar-collapsed' class on the <aside>,
 *     updates aria-expanded on the toggle button, and saves the new state.
 */
(function () {
    'use strict';

    const STORAGE_KEY = 'sf-sidebar-open';
    const sidebar = document.getElementById('sf-admin-sidebar');
    const toggle  = document.getElementById('sf-sidebar-toggle');

    if (!sidebar || !toggle) return;

    // Restore last saved state; default to open on large screens, closed on mobile
    const saved       = localStorage.getItem(STORAGE_KEY);
    const defaultOpen = window.innerWidth >= 992;
    const isOpen      = saved !== null ? saved === '1' : defaultOpen;

    if (!isOpen) sidebar.classList.add('sf-sidebar-collapsed');
    toggle.setAttribute('aria-expanded', isOpen ? 'true' : 'false');

    toggle.addEventListener('click', function () {
        const nowOpen = sidebar.classList.toggle('sf-sidebar-collapsed') === false;
        toggle.setAttribute('aria-expanded', nowOpen ? 'true' : 'false');
        localStorage.setItem(STORAGE_KEY, nowOpen ? '1' : '0');
    });
}());
