// T010: Correlation ID middleware (FR-017).
// Propagates X-Correlation-ID header through request/response pipeline
// so distributed traces and log entries can be joined.
using Serilog.Context;

namespace PoLinks.Web.Features.Shared.Correlation;

/// <summary>
/// Reads or generates a correlation ID per request.
/// Adds it to the Serilog LogContext and echoes it back in the response header.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-ID";
    private const string SessionHeaderName = "X-Session-ID";
    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _environment;

    public CorrelationIdMiddleware(RequestDelegate next, IHostEnvironment environment)
    {
        _next = next;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreate(context.Request.Headers);
        var sessionId = GetOrCreateSessionId(context.Request.Headers);
        var userId = context.User?.Identity?.IsAuthenticated == true
            ? (context.User.FindFirst("sub")?.Value
                ?? context.User.FindFirst("nameid")?.Value
                ?? context.User.Identity?.Name
                ?? "authenticated")
            : "anonymous";

        context.Response.Headers[HeaderName] = correlationId;
        context.Response.Headers[SessionHeaderName] = sessionId;
        context.TraceIdentifier = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("SessionId", sessionId))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("Environment", _environment.EnvironmentName))
        {
            await _next(context);
        }
    }

    private static string GetOrCreate(IHeaderDictionary headers)
    {
        if (headers.TryGetValue(HeaderName, out var existing)
            && !string.IsNullOrWhiteSpace(existing))
        {
            return existing.ToString();
        }

        return Guid.NewGuid().ToString("N");
    }

    private static string GetOrCreateSessionId(IHeaderDictionary headers)
    {
        if (headers.TryGetValue(SessionHeaderName, out var existing)
            && !string.IsNullOrWhiteSpace(existing))
        {
            return existing.ToString();
        }

        return Guid.NewGuid().ToString("N");
    }
}
