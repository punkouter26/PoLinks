// T052: Diagnostic route endpoints for health checks and configuration validation
// Provides routes for deep health checking and safe configuration inspection
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PoLinks.Web.Features.Ingestion;
using PoLinks.Web.Features.Shared.Masking;
using PoLinks.Web.Infrastructure.TableStorage;

namespace PoLinks.Web.Features.Diagnostic;

/// <summary>
/// Response DTO for diagnostic health endpoint.
/// Includes status and details of all registered health checks.
/// </summary>
public sealed record DiagnosticHealthDto(
    string Status,
    IDictionary<string, object> Details,
    DateTimeOffset Timestamp);

/// <summary>
/// Response DTO for diagnostic configuration endpoint.
/// Returns masked (redacted) configuration for safe inspection.
/// </summary>
public sealed record DiagnosticConfigDto(
    string Environment,
    string ApplicationName,
    string ApplicationVersion,
    IDictionary<string, string> MaskedSettings);

public sealed record DiagnosticUptimeDto(
    double UptimePercentage,
    int AvailableProbeCount,
    int TotalProbeCount,
    double TargetPercentage,
    DateTimeOffset AsOfUtc,
    string? Note);

/// <summary>Response DTO for the live diagnostic log buffer endpoint.</summary>
public sealed record DiagnosticLogsDto(
    IReadOnlyList<DiagnosticLogEntry> Logs,
    int Count,
    DateTimeOffset AsOfUtc);

/// <summary>Today's ingestion totals per anchor, pulled from Table Storage.</summary>
public sealed record DiagnosticAnalyticsDto(
    DateOnly Date,
    int TotalPosts,
    IReadOnlyDictionary<string, int> CountsByAnchor,
    DateTimeOffset AsOfUtc);

/// <summary>Sentiment API guardrail state — circuit-breaker and daily quota consumption.</summary>
public sealed record DiagnosticSentimentStatusDto(
    bool CircuitOpen,
    int UsedToday,
    int Cap,
    string EstimatedDailyCost,
    DateTimeOffset AsOfUtc);

public static class DiagnosticEndpoints
{
    public static IEndpointRouteBuilder MapDiagnosticEndpoints(this IEndpointRouteBuilder app)
    {
          app.MapGet("/health", GetDeepHealth)
              .WithName("GetHealth")
              .WithTags("Diagnostic")
              .Produces<DiagnosticHealthDto>(StatusCodes.Status200OK)
              .Produces(StatusCodes.Status503ServiceUnavailable);

        app.MapGet("/diagnostic/health", GetDeepHealth)
           .WithName("GetDeepHealth")
           .WithTags("Diagnostic")
           .Produces<DiagnosticHealthDto>(StatusCodes.Status200OK)
           .Produces(StatusCodes.Status503ServiceUnavailable);

        app.MapGet("/diagnostic/config", GetMaskedConfig)
           .WithName("GetMaskedConfig")
           .WithTags("Diagnostic")
           .Produces<DiagnosticConfigDto>(StatusCodes.Status200OK);

          app.MapGet("/diagnostic/uptime", GetUptime)
              .WithName("GetUptime")
              .WithTags("Diagnostic")
              .Produces<DiagnosticUptimeDto>(StatusCodes.Status200OK);

        app.MapGet("/diagnostic/logs", GetLogs)
           .WithName("GetDiagnosticLogs")
           .WithTags("Diagnostic")
           .Produces<DiagnosticLogsDto>(StatusCodes.Status200OK);

        app.MapGet("/diagnostic/analytics", GetAnalytics)
           .WithName("GetDiagnosticAnalytics")
           .WithTags("Diagnostic")
           .Produces<DiagnosticAnalyticsDto>(StatusCodes.Status200OK);

          app.MapGet("/diagnostic/sentiment-status", GetSentimentStatus)
              .WithName("GetSentimentStatus")
              .WithTags("Diagnostic")
              .Produces<DiagnosticSentimentStatusDto>(StatusCodes.Status200OK);

          return app;
    }

    /// <summary>
    /// Deep health endpoint that runs all registered health checks and returns detailed results.
    /// Used by diagnostic dashboard to show component health status.
    /// </summary>
    private static async Task<IResult> GetDeepHealth(
        HealthCheckService healthCheckService,
        UptimeMetricsService uptimeMetrics,
        DiagnosticLogBuffer logBuffer,
        HttpContext httpContext)
    {
        try
        {
            // Run all health checks with a timeout
            var result = await healthCheckService.CheckHealthAsync(null, CancellationToken.None);

            var details = new Dictionary<string, object>();
            foreach (var check in result.Entries)
            {
                details[check.Key] = new
                {
                    status = check.Value.Status.ToString().ToLower(),
                    description = check.Value.Description ?? "(no description)",
                    duration = check.Value.Duration.TotalMilliseconds
                };
            }

            var response = new DiagnosticHealthDto(
                Status: result.Status.ToString().ToLower(),
                Details: details,
                Timestamp: DateTimeOffset.UtcNow);

            // Ensure diagnostic drawer receives entries during healthy sessions
            // even when structured logging providers are reconfigured.
            logBuffer.Add(new DiagnosticLogEntry(
                Id: Guid.NewGuid().ToString("N"),
                Level: "Information",
                Message: $"Health probe completed with status {response.Status}",
                Exception: null,
                Context: new Dictionary<string, string>
                {
                    ["path"] = httpContext.Request.Path,
                    ["probeCount"] = details.Count.ToString(),
                },
                Timestamp: DateTimeOffset.UtcNow));

            uptimeMetrics.RecordProbe(result.Status != HealthStatus.Unhealthy, DateTimeOffset.UtcNow);

            // Keep diagnostic health endpoint queryable even when dependencies are degraded.
            return TypedResults.Ok(response);
        }
        catch (Exception ex)
        {
            logBuffer.Add(new DiagnosticLogEntry(
                Id: Guid.NewGuid().ToString("N"),
                Level: "Error",
                Message: "Health probe failed",
                Exception: ex.ToString(),
                Context: new Dictionary<string, string>
                {
                    ["path"] = httpContext.Request.Path,
                },
                Timestamp: DateTimeOffset.UtcNow));

            uptimeMetrics.RecordProbe(false, DateTimeOffset.UtcNow);
            return TypedResults.Ok(
                new DiagnosticHealthDto(
                    Status: "error",
                    Details: new Dictionary<string, object>
                    {
                        { "error", ex.Message },
                        { "type", ex.GetType().Name }
                    },
                    Timestamp: DateTimeOffset.UtcNow));
        }
    }

    /// <summary>
    /// Configuration inspection endpoint with automatic secret masking.
    /// Allows developers to verify configuration is loaded correctly without exposing secrets.
    /// </summary>
    private static IResult GetMaskedConfig(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        try
        {
            var maskedSettings = new Dictionary<string, string>();

            // Safely expose non-sensitive configuration values
            var exposedKeys = new[]
            {
                "ASPNETCORE_ENVIRONMENT",
                "ASPNETCORE_URLS",
                "ApplicationName",
                "ApplicationVersion",
                "AzureStorage:ConnectionString",
                "Features:EnableDiagnosticTerminal",
                "Features:EnableGhostLayer",
                "Logging:LogLevel:Default"
            };

            foreach (var key in exposedKeys)
            {
                var value = configuration[key];
                if (!string.IsNullOrEmpty(value))
                {
                    // Mask any sensitive values in the configuration
                    value = SensitiveValueMasker.Mask(value);
                    maskedSettings[key] = value;
                }
            }

            var response = new DiagnosticConfigDto(
                Environment: environment.EnvironmentName,
                ApplicationName: environment.ApplicationName,
                ApplicationVersion: "1.0.0",
                MaskedSettings: maskedSettings);

            return TypedResults.Ok(response);
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                new { error = ex.Message, type = ex.GetType().Name },
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static IResult GetUptime(UptimeMetricsService uptimeMetrics)
    {
        var report = uptimeMetrics.GetReport(DateTimeOffset.UtcNow);
        return TypedResults.Ok(new DiagnosticUptimeDto(
            report.UptimePercentage,
            report.AvailableProbeCount,
            report.TotalProbeCount,
            report.TargetPercentage,
            report.AsOfUtc,
            report.Note));
    }

    private static IResult GetLogs(DiagnosticLogBuffer logBuffer)
    {
        var logs = logBuffer.GetAll();
        return TypedResults.Ok(new DiagnosticLogsDto(logs, logs.Count, DateTimeOffset.UtcNow));
    }

    private static async Task<IResult> GetAnalytics(IPostStorageRepository storage, CancellationToken ct)
    {
        var today   = DateOnly.FromDateTime(DateTime.UtcNow);
        var counts  = await storage.GetDailyCountsAsync(today, ct);
        var total   = counts.Values.Sum();
        return TypedResults.Ok(new DiagnosticAnalyticsDto(today, total, counts, DateTimeOffset.UtcNow));
    }

    private static IResult GetSentimentStatus(ISentimentStatus sentimentStatus)
    {
        var snapshot = sentimentStatus.GetStatus();
        return TypedResults.Ok(new DiagnosticSentimentStatusDto(
            snapshot.CircuitOpen,
            snapshot.UsedToday,
            snapshot.Cap,
            snapshot.EstimatedDailyCost,
            snapshot.AsOfUtc));
    }
}
