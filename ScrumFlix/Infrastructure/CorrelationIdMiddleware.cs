/*
 * File:        /ScrumFlix/Infrastructure/CorrelationIdMiddleware.cs
 * Namespace:   ScrumFlix.Infrastructure
 * Purpose:     Attaches a per-request correlation ID to every Serilog log event
 *              produced during that request's lifetime.
 *
 * How it works:
 *   1. Reads "X-Correlation-Id" from the incoming request header if the caller
 *      (another service, a test harness, or the browser dev tools) supplies one.
 *   2. Otherwise generates a new short GUID (no hyphens, 32 chars) for this request.
 *   3. Echoes the ID back on the response header so callers can match their
 *      outbound trace to ScrumFlix's inbound log events.
 *   4. Pushes the ID into Serilog's LogContext for the duration of the request.
 *      Because LogContext is async-local, every log event emitted on this request's
 *      thread (or any awaited continuation) automatically carries {CorrelationId}.
 *
 * Placement in the middleware pipeline (Program.cs):
 *   Must be registered BEFORE UseSerilogRequestLogging() so that the request
 *   log event itself also carries the CorrelationId property.
 *
 * MySQL query to find all events for a single request:
 *   SELECT * FROM Logs WHERE Properties LIKE '%"CorrelationId":"<id>"%' ORDER BY TimeStamp;
 *
 * Email alert body includes CorrelationId so production errors can be immediately
 * traced to the specific request that triggered them.
 *
 * Author:  ScrumFlix Rebuild Team
 * Phase:   5
 * Added:   2026-05-28
 */

using Serilog.Context;

namespace ScrumFlix.Infrastructure;

/// <summary>
/// Middleware that ensures every HTTP request carries a unique correlation ID
/// that is propagated through all Serilog log events for that request.
/// </summary>
public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        // Honour an ID supplied by the caller (upstream proxy, test, API client).
        // Fall back to a new compact GUID if none is present.
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        // Echo it back so callers can correlate their trace with server logs.
        context.Response.Headers[HeaderName] = correlationId;

        // Push into Serilog's async-local LogContext.
        // The using block ensures the property is popped when the request completes,
        // preventing any possible leak into the next request on the same thread.
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
