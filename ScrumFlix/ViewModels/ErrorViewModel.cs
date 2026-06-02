namespace ScrumFlix.ViewModels;

/// <summary>
/// ViewModel for the Error view.
/// ShowDetail is true for developers (Development env) or logged-in staff.
/// </summary>
public class ErrorViewModel
{
    public int    StatusCode   { get; set; }
    public string OriginalPath { get; set; } = "";
    public string RequestId    { get; set; } = "";
    public bool   IsStaff      { get; set; }
    public bool   ShowDetail   { get; set; }

    public string FriendlyTitle => StatusCode switch
    {
        400 => "Bad Request",
        401 => "Sign-In Required",
        403 => "Access Denied",
        404 => "Page Not Found",
        408 => "Request Timed Out",
        429 => "Too Many Requests",
        500 => "Something Went Wrong",
        502 => "Bad Gateway",
        503 => "Service Unavailable",
        _   => "An Error Occurred"
    };

    public string FriendlyMessage => StatusCode switch
    {
        404 => "The page you're looking for doesn't exist or may have been moved. Check the URL and try again.",
        403 => "You don't have permission to access this page. If you think this is a mistake, please contact a manager.",
        401 => "You need to sign in before accessing that page.",
        500 => "Something went wrong on our end. Our team has been notified and is looking into it.",
        503 => "The service is temporarily unavailable. Please try again in a few minutes.",
        _   => "An unexpected error occurred. Please try again or return to the home page."
    };

    public string IconClass => StatusCode switch
    {
        404 => "bi-map",
        403 => "bi-shield-lock",
        401 => "bi-door-closed",
        500 => "bi-exclamation-triangle",
        503 => "bi-cloud-slash",
        _   => "bi-x-circle"
    };
}
