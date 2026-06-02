/// ============================================================================
/// File: /Services/TMDB/ITmdbImageService.cs
/// Project: ScrumFlix
///
/// Purpose:
/// Defines helper methods for generating proxied TMDb image URLs.
///
/// Why this exists:
/// Prevents controllers/views from manually constructing image URLs.
/// Centralizes image logic.
/// ============================================================================

namespace ScrumFlix.Services.TMDB;

public interface ITmdbImageService
{
    string GetPosterUrl(string? posterPath, int width = 500);

    string GetBackdropUrl(string? backdropPath, int width = 1280);

    string GetProfileUrl(string? profilePath, int width = 300);
}