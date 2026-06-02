/*
 * File: /wwwroot/js/sf-movie-catalog.js
 * Namespace: ScrumFlix (client-side)
 * Purpose: AJAX cascading genre → movie dropdown for the Movie Catalog page.
 *          Fetches /Movies/GetMoviesByGenre on genre change, populates the
 *          movie dropdown, then auto-submits #cascadeForm.
 *          Replaces the superseded initMovieCatalog() in scrumflix.js.
 */

(function () {
    'use strict';

    /**
     * Populates the movie dropdown from a JSON array of { id, name } objects.
     * @param {HTMLSelectElement} dropdown - The movie select element.
     * @param {Array<{id: number, name: string}>} movies - Movie list from the API.
     * @param {string} selectedId - The movie ID to pre-select (may be empty string).
     */
    function populateMovieDropdown(dropdown, movies, selectedId) {
        dropdown.innerHTML = '';

        var placeholder = document.createElement('option');
        placeholder.value = '';
        placeholder.textContent = '— Select a Movie —';
        dropdown.appendChild(placeholder);

        movies.forEach(function (movie) {
            var option = document.createElement('option');
            option.value = movie.id;
            option.textContent = movie.name;
            if (String(movie.id) === String(selectedId)) {
                option.selected = true;
            }
            dropdown.appendChild(option);
        });

        dropdown.disabled = false;
    }

    /**
     * Clears the movie dropdown and puts it back into the disabled/empty state.
     * @param {HTMLSelectElement} dropdown - The movie select element.
     */
    function resetMovieDropdown(dropdown) {
        dropdown.innerHTML = '<option value="">— Select Genre First —</option>';
        dropdown.disabled = true;
    }

    /**
     * Fetches the movie list for a given genre from the server.
     * @param {string} genre - The genre string to query.
     * @returns {Promise<Array<{id: number, name: string}>>}
     */
    function fetchMoviesByGenre(genre) {
        var url = '/Movies/GetMoviesByGenre?genre=' + encodeURIComponent(genre);
        return fetch(url)
            .then(function (response) {
                if (!response.ok) {
                    throw new Error('Network response was not ok: ' + response.status);
                }
                return response.json();
            });
    }

    /**
     * Handles a genre dropdown change event.
     * Fetches movies for the selected genre and populates the movie dropdown,
     * then auto-submits the cascade form.
     * @param {Event} event - The change event from #genreDropdown.
     * @param {HTMLSelectElement} movieDropdown - The #movieDropdown element.
     * @param {HTMLFormElement} cascadeForm - The #cascadeForm element.
     */
    function onGenreChange(event, movieDropdown, cascadeForm) {
        var genre = event.target.value;

        if (!genre) {
            resetMovieDropdown(movieDropdown);
            cascadeForm.submit();
            return;
        }

        fetchMoviesByGenre(genre)
            .then(function (movies) {
                populateMovieDropdown(movieDropdown, movies, '');
                movieDropdown.disabled = false;
                cascadeForm.submit();
            })
            .catch(function (err) {
                console.error('[sf-movie-catalog] Failed to fetch movies for genre "' + genre + '":', err);
                resetMovieDropdown(movieDropdown);
            });
    }

    /**
     * Handles a movie dropdown change event.
     * Re-enables the dropdown (disabled controls are not submitted) and
     * auto-submits the cascade form.
     * @param {HTMLFormElement} cascadeForm - The #cascadeForm element.
     * @param {HTMLSelectElement} movieDropdown - The #movieDropdown element.
     */
    function onMovieChange(cascadeForm, movieDropdown) {
        movieDropdown.disabled = false;
        cascadeForm.submit();
    }

    /**
     * Entry point — wires up all cascade behaviour once the DOM is ready.
     */
    function init() {
        var cascadeForm   = document.getElementById('cascadeForm');
        var genreDropdown = document.getElementById('genreDropdown');
        var movieDropdown = document.getElementById('movieDropdown');

        if (!cascadeForm || !genreDropdown || !movieDropdown) {
            // Elements not present on this page — nothing to initialise.
            return;
        }

        // On page load: if a genre is already selected (e.g. back-navigation or
        // bookmarked URL), fetch and restore the movie dropdown.
        var currentGenre    = genreDropdown.value;
        var selectedMovieId = movieDropdown.dataset.selectedMovie || '';

        if (currentGenre) {
            fetchMoviesByGenre(currentGenre)
                .then(function (movies) {
                    populateMovieDropdown(movieDropdown, movies, selectedMovieId);
                })
                .catch(function (err) {
                    console.error('[sf-movie-catalog] Failed to restore movie dropdown on load:', err);
                    resetMovieDropdown(movieDropdown);
                });
        }

        // Wire genre change → fetch + auto-submit.
        genreDropdown.addEventListener('change', function (event) {
            onGenreChange(event, movieDropdown, cascadeForm);
        });

        // Wire movie change → re-enable + auto-submit.
        movieDropdown.addEventListener('change', function () {
            onMovieChange(cascadeForm, movieDropdown);
        });

        // Ensure movie dropdown is re-enabled before any form submission so
        // its value is included in the GET query string.
        cascadeForm.addEventListener('submit', function () {
            movieDropdown.disabled = false;
        });
    }

    document.addEventListener('DOMContentLoaded', init);
}());
