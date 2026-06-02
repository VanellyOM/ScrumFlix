/// ============================================================================
/// File: /Services/TMDB/TmdbImageService.cs
/// Project: ScrumFlix
///
/// Purpose:
/// Generates local proxy routes for TMDb images.
///
/// Instead of exposing direct TMDb image URLs to clients,
/// the application generates local routes that are handled
/// by the TMDb image proxy controller.
///
/// Benefits:
/// - Centralized image delivery
/// - Easier caching
/// - Better resizing control
/// - Future CDN compatibility
/// - Prevents direct TMDb hotlinking
/// ============================================================================

namespace ScrumFlix.Services.TMDB;

public class TmdbImageService : ITmdbImageService
{
    /// <summary>
    /// Generates a proxied poster image route.
    /// </summary>
    /// <param name="posterPath">TMDb poster path</param>
    /// <param name="width">Desired image width</param>
    /// <returns>Local application image route</returns>
    public string GetPosterUrl(string? posterPath, int width = 500)
    {
        if (string.IsNullOrWhiteSpace(posterPath))
        {
            return "/images/placeholders/poster-placeholder.png";
        }

        return $"/tmdb/poster/{width}{posterPath}";
    }

    /// <summary>
    /// Generates a proxied backdrop image route.
    /// </summary>
    public string GetBackdropUrl(string? backdropPath, int width = 1280)
    {
        if (string.IsNullOrWhiteSpace(backdropPath))
        {
            return "/images/placeholders/backdrop-placeholder.jpg";
        }

        return $"/tmdb/backdrop/{width}{backdropPath}";
    }

    /// <summary>
    /// Generates a proxied actor/profile image route.
    /// </summary>
    public string GetProfileUrl(string? profilePath, int width = 300)
    {
        if (string.IsNullOrWhiteSpace(profilePath))
        {
            return "/images/placeholders/profile-placeholder.png";
        }

        return $"/tmdb/profile/{width}{profilePath}";
    }
}