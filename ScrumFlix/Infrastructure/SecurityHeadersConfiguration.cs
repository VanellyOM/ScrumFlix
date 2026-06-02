/*
 * File:        /ScrumFlix/Infrastructure/SecurityHeadersConfiguration.cs
 * Namespace:   ScrumFlix.Infrastructure
 * Purpose:     Centralizes all NetEscapades.AspNetCore.SecurityHeaders policy
 *              configuration so Program.cs stays clean.
 *
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║  HEADER OVERVIEW                                                             ║
 * ║                                                                              ║
 * ║  Strict-Transport-Security (HSTS)                                           ║
 * ║    Forces HTTPS for 1 year, including subdomains.  max-age=31536000.        ║
 * ║                                                                              ║
 * ║  Content-Security-Policy (CSP)                                              ║
 * ║    Controls which resources the browser may load.  Tuned for ScrumFlix's   ║
 * ║    Bootstrap 5 CDN, Google Fonts, and inline scripts/styles used by the    ║
 * ║    existing scrumflix.css / scrumflix.js assets.                            ║
 * ║    Adjust CDN hostnames if the view layer adds new external sources.        ║
 * ║                                                                              ║
 * ║  X-Frame-Options                                                            ║
 * ║    DENY — ScrumFlix has no legitimate iframe embedding use case.            ║
 * ║                                                                              ║
 * ║  X-Content-Type-Options                                                     ║
 * ║    nosniff — prevents MIME-type sniffing attacks.                           ║
 * ║                                                                              ║
 * ║  Referrer-Policy                                                            ║
 * ║    strict-origin-when-cross-origin — sends origin only on cross-site        ║
 * ║    requests; full URL on same-site.  Good balance of analytics vs. privacy. ║
 * ║                                                                              ║
 * ║  Permissions-Policy                                                         ║
 * ║    Explicitly disables browser features not needed by a cinema ticket app:  ║
 * ║    camera, microphone, geolocation, payment, USB, accelerometer, gyroscope. ║
 * ║    Allowlist pattern: any feature not listed here is allowed by default.    ║
 * ║                                                                              ║
 * ║  Cross-Origin-Opener-Policy (COOP)                                          ║
 * ║    same-origin — isolates the browsing context from cross-origin openers.  ║
 * ║                                                                              ║
 * ║  Cross-Origin-Embedder-Policy (COEP)                                        ║
 * ║    require-corp — required pair with COOP for cross-origin isolation.       ║
 * ║    NOTE: If you embed third-party iframes or images without CORP headers,   ║
 * ║    downgrade to unsafe-none until those resources are fixed.                ║
 * ║                                                                              ║
 * ║  Cross-Origin-Resource-Policy (CORP)                                        ║
 * ║    same-origin — prevents cross-origin reads of ScrumFlix resources.       ║
 * ║                                                                              ║
 * ║  Server header removal                                                      ║
 * ║    Removes the "Server: Kestrel" header to reduce fingerprinting surface.   ║
 * ║                                                                              ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 *
 * TUNING THE CSP:
 *   The CSP is the most environment-sensitive header.  Run the app with the
 *   browser DevTools console open — any blocked resource will appear as a
 *   "Content Security Policy" violation.  Common adjustments needed:
 *
 *     - Adding a new CDN JS library  →  add its host to ScriptSrc
 *     - Adding Google Analytics      →  add 'www.google-analytics.com' to ScriptSrc
 *                                       and ConnectSrc
 *     - Adding a font provider       →  add host to FontSrc and StyleSrc
 *     - Inline event handlers        →  prefer moving to .js files; avoid 'unsafe-inline'
 *
 * COEP / COOP CAUTION:
 *   COEP require-corp will block loading any cross-origin resource that does
 *   not set a Cross-Origin-Resource-Policy response header.  Bootstrap CDN and
 *   Google Fonts do set these headers, but if you encounter blocked images or
 *   fonts, temporarily set COEP to UnsafeNone() while you investigate.
 *
 * Author:  ScrumFlix Rebuild Team
 * Phase:   1E
 * Updated: 2026-05-04
 */

namespace ScrumFlix.Infrastructure;

/// <summary>
/// Builds the <see cref="HeaderPolicyCollection"/> used by the
/// NetEscapades security headers middleware.
/// </summary>
public static class SecurityHeadersConfiguration
{
    /// <summary>
    /// Returns the production-grade header policy collection for ScrumFlix.
    /// Call <c>app.UseSecurityHeaders(SecurityHeadersConfiguration.BuildPolicy())</c>
    /// in Program.cs before <c>UseStaticFiles</c>.
    /// </summary>
    public static HeaderPolicyCollection BuildPolicy() =>
      new HeaderPolicyCollection()

      // ── Strict-Transport-Security ──────────────────────────────────────
      // 1-year max-age, include subdomains.
      // Only sent on HTTPS responses — safe to register unconditionally.
      .AddStrictTransportSecurityMaxAgeIncludeSubDomains(maxAgeInSeconds: 60 * 60 * 24 * 365)

      // ── X-Frame-Options ───────────────────────────────────────────────
      // ScrumFlix never embeds itself in an iframe.
      .AddFrameOptionsDeny()

      // ── X-Content-Type-Options ────────────────────────────────────────
      .AddContentTypeOptionsNoSniff()

      // ── Referrer-Policy ───────────────────────────────────────────────
      .AddReferrerPolicyStrictOriginWhenCrossOrigin()

      // ── Permissions-Policy ────────────────────────────────────────────
      // Disable every browser capability a cinema ticket app doesn't need.
      .AddPermissionsPolicy(builder => {
          builder.AddCamera().None();
          builder.AddMicrophone().None();
          builder.AddGeolocation().None();
          builder.AddPayment().None();
          builder.AddUsb().None();
          builder.AddAccelerometer().None();
          builder.AddGyroscope().None();
          builder.AddMagnetometer().None();
          builder.AddDisplayCapture().None();
          builder.AddPictureInPicture().None();
          builder.AddScreenWakeLock().None();
      })

      // ── Cross-Origin-Opener-Policy ────────────────────────────────────
      .AddCrossOriginOpenerPolicy(builder => builder.SameOrigin())

      // ── Cross-Origin-Embedder-Policy ──────────────────────────────────
      // Switch to RequireCorp() once all CDN assets are verified CORP-compliant.
      // Use UnsafeNone() if cross-origin resources block loading during dev.
      .AddCrossOriginEmbedderPolicy(builder => builder.UnsafeNone())

      // ── Cross-Origin-Resource-Policy ──────────────────────────────────
      .AddCrossOriginResourcePolicy(builder => builder.SameOrigin())

      // ── Remove "Server" header ────────────────────────────────────────
      // Kestrel adds "Server: Kestrel" by default — remove it to reduce
      // fingerprinting surface area.
      .RemoveServerHeader()

      // ── Content-Security-Policy ───────────────────────────────────────
      //
      // Directive-by-directive breakdown:
      //
      //   default-src 'self'
      //     Fallback for any directive not explicitly listed below.
      //
      //   script-src 'self' cdn.jsdelivr.net
      //     Bootstrap 5 JS is loaded from jsDelivr.  Add any other CDN
      //     script hosts here.  'unsafe-inline' is intentionally omitted —
      //     move any inline <script> blocks to scrumflix.js.
      //
      //   style-src 'self' cdn.jsdelivr.net fonts.googleapis.com 'unsafe-inline'
      //     Bootstrap CSS (jsDelivr) + Google Fonts CSS require this.
      //     'unsafe-inline' is needed for Bootstrap's style="" attribute
      //     patterns on certain components.  If you can audit and remove all
      //     inline styles, remove 'unsafe-inline' here.
      //
      //   font-src 'self' fonts.gstatic.com
      //     Google Fonts serves font files from fonts.gstatic.com.
      //
      //   img-src 'self' data:
      //     data: allows base64-encoded images (used by some Bootstrap icons
      //     and any inline SVG data URIs in views).
      //
      //   connect-src 'self'
      //     AJAX calls (cart badge refresh, concession add-to-cart) stay
      //     on the same origin.  Add third-party API hosts if needed.
      //
      //   frame-ancestors 'none'
      //     Belt-and-suspenders with X-Frame-Options DENY.
      //
      //   form-action 'self'
      //     All HTML form posts must target the same origin.
      //
      //   object-src 'none'
      //     Disables Flash and other browser plugins.
      //
      //   base-uri 'self'
      //     Prevents <base href> injection attacks.
      //
      //   upgrade-insecure-requests
      //     Browser upgrades any http:// sub-resource requests to https://
      //     automatically.
      //
      .AddContentSecurityPolicy(builder => {
          builder.AddDefaultSrc().Self();

          builder.AddScriptSrc()
          .Self()
          //.UnsafeInline() // Allows the refreshCartBadge() script in _Layout
          .From("cdn.jsdelivr.net")
          .From("cdnjs.cloudflare.com") // Added for FontAwesome CSS
          .From("https://www.youtube.com"); // Added for FontAwesome CSS

          //builder.AddScriptSrc()
          //    .Self()
          //    .From("cdn.jsdelivr.net");

          // Allow styles from self, jsdelivr (Bootstrap), googleapis (Fonts), and cdnjs (FontAwesome)
          builder.AddStyleSrc()
          .Self()
          .UnsafeInline()
          .From("cdn.jsdelivr.net")
          .From("fonts.googleapis.com")
          .From("cdnjs.cloudflare.com"); // Added for FontAwesome CSS
                                         //builder.AddStyleSrc()
                                         //    .Self()
                                         //    .From("cdn.jsdelivr.net")
                                         //    .From("fonts.googleapis.com")
                                         //    .UnsafeInline();   // required by Bootstrap component styles

          // Allow font files from gstatic (Google) and cdnjs (FontAwesome)
          builder.AddFontSrc()
          .Self()
          .From("fonts.gstatic.com")
          .From("cdn.jsdelivr.net") //  Required for the actual Icon files (.woff2)
          .From("cdnjs.cloudflare.com"); // Added for FontAwesome Font files
                                         //builder.AddFontSrc()
                                         //    .Self()
                                         //    .From("fonts.gstatic.com");

          builder.AddImgSrc()
          .Self()
          .Data() // base64 data URIs for Bootstrap icons / SVGs
                  // image.tmdb.org kept here for fallback/admin direct access.
                  // Consumer views now use the /tmdb/poster|backdrop/* proxy routes
                  // (TmdbImageController) so browser requests go to 'self' only.
                  // This entry can be removed once all direct PosterUrl usages are confirmed gone.
          .From("image.tmdb.org")
          .From("www.themoviedb.org") //
          .From("https://*.ytimg.com");
          //builder.AddImgSrc()
          //    .Self()
          //    .Data();           // base64 data URIs for Bootstrap icons / SVGs

          // Allow connections to self and Google Fonts (for preconnect/preflight)
          builder.AddConnectSrc()
          .Self()
          .From("fonts.googleapis.com") // Added to support font preconnect links
          .From("api.themoviedb.org") // Only if calling TMDb via JavaScript/Fetch
                                      // --- ADD THESE FOR DEVELOPMENT ---
          .From("http://localhost:*") // Allows Browser Link
          .From("https://www.youtube.com") // Allows Browser Link (SSL)
          .From("ws://localhost:*") // Allows Hot Reload WebSockets
          .From("wss://localhost:*"); // Allows Hot Reload WebSockets (SSL)
                                      //builder.AddConnectSrc()
                                      //    .Self();
          builder.AddFrameSrc()
          .From("https://www.youtube.com");

          builder.AddFrameAncestors()
          .None();

          builder.AddFormAction()
          .Self();

          builder.AddObjectSrc()
          .None();

          builder.AddBaseUri()
          .Self();

          builder.AddUpgradeInsecureRequests();
      });

    // ── Content-Security-Policy ───────────────────────────────────────
    //
    // Directive-by-directive breakdown:
    //
    //   default-src 'self'
    //     Fallback for any directive not explicitly listed below.
    //
    //   script-src 'self' cdn.jsdelivr.net
    //     Bootstrap 5 JS is loaded from jsDelivr.  Add any other CDN
    //     script hosts here.  'unsafe-inline' is intentionally omitted —
    //     move any inline <script> blocks to scrumflix.js.
    //
    //   style-src 'self' cdn.jsdelivr.net fonts.googleapis.com 'unsafe-inline'
    //     Bootstrap CSS (jsDelivr) + Google Fonts CSS require this.
    //     'unsafe-inline' is needed for Bootstrap's style="" attribute
    //     patterns on certain components.  If you can audit and remove all
    //     inline styles, remove 'unsafe-inline' here.
    //
    //   font-src 'self' fonts.gstatic.com
    //     Google Fonts serves font files from fonts.gstatic.com.
    //
    //   img-src 'self' data:
    //     data: allows base64-encoded images (used by some Bootstrap icons
    //     and any inline SVG data URIs in views).
    //
    //   connect-src 'self'
    //     AJAX calls (cart badge refresh, concession add-to-cart) stay
    //     on the same origin.  Add third-party API hosts if needed.
    //
    //   frame-ancestors 'none'
    //     Belt-and-suspenders with X-Frame-Options DENY.
    //
    //   form-action 'self'
    //     All HTML form posts must target the same origin.
    //
    //   object-src 'none'
    //     Disables Flash and other browser plugins.
    //
    //   base-uri 'self'
    //     Prevents <base href> injection attacks.
    //
    //   upgrade-insecure-requests
    //     Browser upgrades any http:// sub-resource requests to https://
    //     automatically.
    //
    public static HeaderPolicyCollection BuildDevPolicy() =>
      new HeaderPolicyCollection()

      // ── Strict-Transport-Security ──────────────────────────────────────
      // 1-year max-age, include subdomains.
      // Only sent on HTTPS responses — safe to register unconditionally.
      .AddStrictTransportSecurityMaxAgeIncludeSubDomains(maxAgeInSeconds: 60 * 60 * 24 * 365)

      // ── X-Frame-Options ───────────────────────────────────────────────
      // ScrumFlix never embeds itself in an iframe.
      .AddFrameOptionsDeny()

      // ── X-Content-Type-Options ────────────────────────────────────────
      .AddContentTypeOptionsNoSniff()

      // ── Referrer-Policy ───────────────────────────────────────────────
      .AddReferrerPolicyStrictOriginWhenCrossOrigin()

      // ── Permissions-Policy ────────────────────────────────────────────
      // Disable every browser capability a cinema ticket app doesn't need.
      .AddPermissionsPolicy(builder => {
          builder.AddCamera().None();
          builder.AddMicrophone().None();
          builder.AddGeolocation().None();
          builder.AddPayment().None();
          builder.AddUsb().None();
          builder.AddAccelerometer().None();
          builder.AddGyroscope().None();
          builder.AddMagnetometer().None();
          builder.AddDisplayCapture().None();
          builder.AddPictureInPicture().None();
          builder.AddScreenWakeLock().None();
      })

      // ── Cross-Origin-Opener-Policy ────────────────────────────────────
      .AddCrossOriginOpenerPolicy(builder => builder.SameOrigin())

      // ── Cross-Origin-Embedder-Policy ──────────────────────────────────
      // Switch to RequireCorp() once all CDN assets are verified CORP-compliant.
      // Use UnsafeNone() if cross-origin resources block loading during dev.
      .AddCrossOriginEmbedderPolicy(builder => builder.UnsafeNone())

      // ── Cross-Origin-Resource-Policy ──────────────────────────────────
      .AddCrossOriginResourcePolicy(builder => builder.SameOrigin())

      // ── Remove "Server" header ────────────────────────────────────────
      // Kestrel adds "Server: Kestrel" by default — remove it to reduce
      // fingerprinting surface area.
      .RemoveServerHeader()

      .AddContentSecurityPolicy(builder => {
          builder.AddScriptSrc()
          .Self()
          .UnsafeInline() // ← allows Browser Link's injected script
          .From("cdn.jsdelivr.net")
          .From("cdnjs.cloudflare.com")
          .From("https://www.youtube.com");

          // all other directives identical to BuildPolicy()
          // Allow styles from self, jsdelivr (Bootstrap), googleapis (Fonts), and cdnjs (FontAwesome)
          builder.AddStyleSrc()
          .Self()
          .UnsafeInline()
          .From("cdn.jsdelivr.net")
          .From("fonts.googleapis.com")
          .From("cdnjs.cloudflare.com"); // Added for FontAwesome CSS
                                         //builder.AddStyleSrc()
                                         //    .Self()
                                         //    .From("cdn.jsdelivr.net")
                                         //    .From("fonts.googleapis.com")
                                         //    .UnsafeInline();   // required by Bootstrap component styles

          // Allow font files from gstatic (Google) and cdnjs (FontAwesome)
          builder.AddFontSrc()
          .Self()
          .From("fonts.gstatic.com")
          .From("cdn.jsdelivr.net") //  Required for the actual Icon files (.woff2)
          .From("cdnjs.cloudflare.com"); // Added for FontAwesome Font files
                                         //builder.AddFontSrc()
                                         //    .Self()
                                         //    .From("fonts.gstatic.com");

          builder.AddImgSrc()
          .Self()
          .Data() // base64 data URIs for Bootstrap icons / SVGs
                  // image.tmdb.org kept here for fallback/admin direct access.
                  // Consumer views now use the /tmdb/poster|backdrop/* proxy routes
                  // (TmdbImageController) so browser requests go to 'self' only.
                  // This entry can be removed once all direct PosterUrl usages are confirmed gone.
          .From("image.tmdb.org")
          .From("www.themoviedb.org") //
          .From("https://*.ytimg.com");
          //builder.AddImgSrc()
          //    .Self()
          //    .Data();           // base64 data URIs for Bootstrap icons / SVGs

          // Allow connections to self and Google Fonts (for preconnect/preflight)
          builder.AddConnectSrc()
          .Self()
          .From("fonts.googleapis.com") // Added to support font preconnect links
          .From("api.themoviedb.org") // Only if calling TMDb via JavaScript/Fetch
                                      // --- ADD THESE FOR DEVELOPMENT ---
          .From("http://localhost:*") // Allows Browser Link
          .From("https://www.youtube.com") // Allows Browser Link (SSL)
          .From("ws://localhost:*") // Allows Hot Reload WebSockets
          .From("wss://localhost:*"); // Allows Hot Reload WebSockets (SSL)
                                      //builder.AddConnectSrc()
                                      //    .Self();
          builder.AddFrameSrc()
          .From("https://www.youtube.com");

          builder.AddFrameAncestors()
          .None();

          builder.AddFormAction()
          .Self();

          builder.AddObjectSrc()
          .None();

          builder.AddBaseUri()
          .Self();

          builder.AddUpgradeInsecureRequests();
      });
}