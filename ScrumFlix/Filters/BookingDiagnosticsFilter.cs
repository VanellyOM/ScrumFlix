using Microsoft.AspNetCore.Mvc.Filters;

namespace ScrumFlix.Filters;

// File: /ScrumFlix/Filters/BookingDiagnosticsFilter.cs
public class BookingDiagnosticsFilter : IActionFilter
{
    private readonly ILogger<BookingDiagnosticsFilter> _logger;

    public BookingDiagnosticsFilter(ILogger<BookingDiagnosticsFilter> logger)
        => _logger = logger;

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var req = context.HttpContext.Request;
        if (!req.HasFormContentType) return;

        // Log every form field
        foreach (var key in req.Form.Keys)
            _logger.LogDebug("FORM  {Key} = {Value}", key, req.Form[key]);

        // Log every action argument (bound model values)
        foreach (var arg in context.ActionArguments)
            _logger.LogDebug("ARG   {Key} = {Value}",
                arg.Key, System.Text.Json.JsonSerializer.Serialize(arg.Value));
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // Log ModelState errors after binding
        var ms = context.Controller is Controller c ? c.ModelState : null;
        if (ms == null || ms.IsValid) return;

        foreach (var kvp in ms)
            foreach (var err in kvp.Value.Errors)
                _logger.LogWarning("MODELSTATE  {Field}: {Error}", kvp.Key, err.ErrorMessage);
    }
}