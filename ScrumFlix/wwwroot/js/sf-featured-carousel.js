/**
 * File:    /wwwroot/js/sf-featured-carousel.js
 * Purpose: Initializes the Splide.js featured-movies carousel on the home
 *          dashboard and wires the YouTube trailer modal.
 *
 * Dependencies (loaded before this file via _Layout.cshtml):
 *   - Splide.js  (cdnjs.cloudflare.com)
 *   - Bootstrap 5.3.x (already on page)
 *
 * No inline <script> blocks anywhere — all logic lives here.
 */

document.addEventListener('DOMContentLoaded', function () {

    // ── 1. Splide carousel init (homepage only) ────────────────────────────

    var carouselEl = document.getElementById('sf-featured-carousel');

    if (carouselEl) {
        // Cap perPage to slide count so Splide never clones duplicates into view.
        var slideCount = carouselEl.querySelectorAll('.splide__slide').length;
        var effectivePerPage = Math.min(5, slideCount);
        var carouselType = slideCount > 5 ? 'loop' : 'slide';

        var splide = new Splide('#sf-featured-carousel', {
            type: carouselType,
            perPage: effectivePerPage,
            perMove: 1,
            gap: '1.25rem',
            padding: { left: '0', right: '0' },
            arrows: slideCount > effectivePerPage,
            pagination: slideCount > effectivePerPage,
            rewind: carouselType === 'slide',
            speed: 400,
            breakpoints: {
                1199: { perPage: Math.min(4, slideCount), gap: '1rem' },
                991:  { perPage: Math.min(3, slideCount), gap: '1rem' },
                767:  { perPage: Math.min(2, slideCount), gap: '.75rem' },
                575:  {
                    perPage: 1,
                    padding: { left: '0', right: slideCount > 1 ? '2.5rem' : '0' },
                    gap: '.75rem'
                }
            }
        });

        splide.mount();
    }

    // ── 2. Read showtime JSON island ───────────────────────────────────────

    var showtimeDataEl = document.getElementById('sf-featured-showtimes-data');
    var showtimeMap = {};
    if (showtimeDataEl) {
        try {
            showtimeMap = JSON.parse(showtimeDataEl.textContent);
        } catch (e) {
            console.warn('sf-featured-carousel: could not parse showtime data', e);
        }
    }

    // ── 3. Trailer modal wiring ────────────────────────────────────────────

    var modalEl = document.getElementById('sf-trailer-modal');
    if (!modalEl) return;

    var bsModal = bootstrap.Modal.getOrCreateInstance(modalEl);
    var iframe = document.getElementById('sf-trailer-iframe');
    var modalTitle = document.getElementById('sf-trailer-modal-label');
    var footerTitle = document.getElementById('sf-trailer-footer-title');
    var footerSub = document.getElementById('sf-trailer-footer-sub');
    var thumbEl = document.getElementById('sf-trailer-thumb');
    var showtimesEl = document.getElementById('sf-trailer-showtimes');
    var detailBtn = document.getElementById('sf-trailer-detail-btn');

    // Delegate click on all .sf-trailer-btn elements (present + future slides)
    document.addEventListener('click', function (e) {
        var btn = e.target.closest('.sf-trailer-btn');
        if (!btn) return;

        e.preventDefault();
        e.stopPropagation();

        var youtubeKey = btn.dataset.youtubeKey || '';
        var title = btn.dataset.movieTitle || '';
        var rating = btn.dataset.rating || '';
        var runtime = btn.dataset.runtime || '';
        var detailUrl = btn.dataset.detailUrl || '#';

        // Determine movieId from the closest article's data or by matching title
        // The trailer badge lives inside the card; walk up to the <li> which contains
        // a data attribute we can use, OR we parse it from the detail URL.
        var movieId = _extractMovieIdFromUrl(detailUrl);

        // Populate modal header
        if (modalTitle) modalTitle.textContent = title;

        // Populate footer meta
        if (footerTitle) footerTitle.textContent = title;
        if (footerSub) footerSub.textContent = [rating, runtime].filter(Boolean).join(' · ');

        // Poster: use TMDB poster passed via data-poster-url; YouTube thumbnail as fallback.
        var posterUrl = btn.dataset.posterUrl || '';
        if (thumbEl) {
            thumbEl.src = posterUrl
                ? posterUrl
                : (youtubeKey ? 'https://img.youtube.com/vi/' + youtubeKey + '/mqdefault.jpg' : '');
            thumbEl.alt = title + ' poster';
        }

        // Showtime pills in modal footer
        if (showtimesEl) {
            showtimesEl.innerHTML = '';
            var pills = (movieId && showtimeMap[movieId]) ? showtimeMap[movieId] : [];
            pills.forEach(function (st) {
                var a = document.createElement('a');
                a.href = '/Showtimes/ShowtimeBooking/' + st.id;
                a.className = 'sf-showtime-pill';
                a.textContent = st.time;
                a.setAttribute('aria-label', 'Book ' + title + ' at ' + st.time);
                showtimesEl.appendChild(a);
            });
        }

        // "Get Tickets" button
        if (detailBtn) detailBtn.href = detailUrl;

        // Set iframe src AFTER modal is shown (avoids layout shift)
        var autoplaySrc = 'https://www.youtube.com/embed/' + youtubeKey + '?rel=0&autoplay=1';

        modalEl.addEventListener('shown.bs.modal', function onShown() {
            if (iframe) iframe.src = autoplaySrc;
            modalEl.removeEventListener('shown.bs.modal', onShown);
        });

        bsModal.show();
    });

    // Stop video when modal closes
    modalEl.addEventListener('hidden.bs.modal', function () {
        if (iframe) iframe.src = '';
    });

    // ── Helpers ────────────────────────────────────────────────────────────

    /**
     * Extracts the numeric movie ID from a detail URL like "/Movies/MovieDetail/42".
     * Returns a string key (matches keys in showtimeMap) or null.
     */
    function _extractMovieIdFromUrl(url) {
        if (!url) return null;
        var match = url.match(/\/(\d+)\/?(?:\?.*)?$/);
        return match ? match[1] : null;
    }

});