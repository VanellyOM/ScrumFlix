/// ============================================================================
/// Project: ScrumFlix
///
/// Purpose:
/// Acts as an image proxy between the client browser and TMDb.
///
/// Responsibilities:
/// - Receives image requests from the browser
/// - Builds the remote TMDb image URL
/// - Downloads the image using HttpClient
/// - Streams the image back to the browser
/// - Enables ImageSharp.Web processing/caching
///
/// Why use a controller proxy?
/// - Prevents direct TMDb exposure
/// - Allows centralized caching
/// - Enables future authorization/security
/// - Makes CDN integration easier later
/// ============================================================================

namespace ScrumFlix.Controllers;

[Route("tmdb")]
public class TmdbImageController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="httpClientFactory">
    /// Factory used to create HttpClient instances.
    /// </param>
    public TmdbImageController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Proxies poster images from TMDb.
    /// </summary>
    /// <param name="width">TMDb image width</param>
    /// <param name="path">TMDb image path</param>
    /// <returns>Image stream response</returns>
    [HttpGet("poster/{width}/{*path}")]
    public async Task<IActionResult> Poster(int width, string path)
    {
        string tmdbUrl = $"https://image.tmdb.org/t/p/w{width}/{path}";

        return await ProxyImage(tmdbUrl);
    }

    /// <summary>
    /// Proxies backdrop images from TMDb.
    /// </summary>
    [HttpGet("backdrop/{width}/{*path}")]
    public async Task<IActionResult> Backdrop(int width, string path)
    {
        string tmdbUrl = $"https://image.tmdb.org/t/p/w{width}/{path}";

        return await ProxyImage(tmdbUrl);
    }

    /// <summary>
    /// Proxies profile images from TMDb.
    /// </summary>
    [HttpGet("profile/{width}/{*path}")]
    public async Task<IActionResult> Profile(int width, string path)
    {
        string tmdbUrl = $"https://image.tmdb.org/t/p/w{width}/{path}";

        return await ProxyImage(tmdbUrl);
    }

    /// <summary>
    /// Downloads and streams an image from TMDb.
    /// </summary>
    /// <param name="url">Remote TMDb image URL</param>
    /// <returns>File stream response</returns>
    private async Task<IActionResult> ProxyImage(string url)
    {
        var client = _httpClientFactory.CreateClient();

        var response = await client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            return NotFound();
        }

        var stream = await response.Content.ReadAsStreamAsync();

        string contentType = response.Content.Headers.ContentType?.MediaType
            ?? "image/jpeg";

        return File(stream, contentType);
    }
}