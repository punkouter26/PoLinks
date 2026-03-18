// T055: Configuration validity health check
// Validates that all required configuration settings are present and valid
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PoLinks.Web.Features.Diagnostic.HealthChecks;

/// <summary>
/// Health check for application configuration validity.
/// Ensures that all required settings are present and have valid values.
/// Does NOT expose actual configuration values (uses masking for sensitive keys).
/// </summary>
public class ConfigHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<ConfigHealthCheck> _logger;

    // Required configuration keys that must be set
    private static readonly string[] RequiredKeys = new[]
    {
        "Bluesky:ApiBaseUrl"
    };

    // Optional but recommended keys
    private static readonly string[] RecommendedKeys = new[]
    {
        "Logging:LogLevel:Default",
        "Features:EnableDiagnosticTerminal"
    };

    public ConfigHealthCheck(
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<ConfigHealthCheck> logger)
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var missingKeys = new List<string>();
            var invalidKeys = new List<string>();
            var missingRecommended = new List<string>();

            // Check required keys
            foreach (var key in RequiredKeys)
            {
                var value = _configuration[key];
                if (string.IsNullOrWhiteSpace(value))
                {
                    missingKeys.Add(key);
                }
                else if (!IsValidConfigValue(key, value))
                {
                    invalidKeys.Add($"{key} (invalid value)");
                }
            }

            // Check recommended keys
            foreach (var key in RecommendedKeys)
            {
                var value = _configuration[key];
                if (string.IsNullOrWhiteSpace(value))
                {
                    missingRecommended.Add(key);
                }
            }

            // Check environment
            if (string.IsNullOrWhiteSpace(_environment.EnvironmentName))
            {
                invalidKeys.Add("ASPNETCORE_ENVIRONMENT (not set)");
            }

            // Build result
            if (missingKeys.Count > 0 || invalidKeys.Count > 0)
            {
                var details = new List<string>();
                if (missingKeys.Count > 0)
                    details.Add($"Missing required: {string.Join(", ", missingKeys)}");
                if (invalidKeys.Count > 0)
                    details.Add($"Invalid: {string.Join(", ", invalidKeys)}");
                if (missingRecommended.Count > 0)
                    details.Add($"Missing recommended: {string.Join(", ", missingRecommended)}");

                var message = string.Join("; ", details);
                _logger.LogError("Configuration health check failed: {Message}", message);
                return Task.FromResult(HealthCheckResult.Unhealthy(message));
            }

            if (missingRecommended.Count > 0)
            {
                var message = $"Missing recommended settings: {string.Join(", ", missingRecommended)}";
                _logger.LogWarning("Configuration health check degraded: {Message}", message);
                return Task.FromResult(HealthCheckResult.Degraded(message));
            }

            _logger.LogInformation("Configuration health check passed");
            return Task.FromResult(HealthCheckResult.Healthy("All required configuration is valid"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Configuration health check failed with unexpected error");
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Unexpected error during configuration validation", ex));
        }
    }

    /// <summary>
    /// Validates that a configuration value is in the correct format for its key.
    /// </summary>
    private static bool IsValidConfigValue(string key, string value)
    {
        return key switch
        {
            "Bluesky:ApiBaseUrl" => Uri.TryCreate(value, UriKind.Absolute, out _),
            "ASPNETCORE_ENVIRONMENT" => new[] { "Development", "Staging", "Production" }
                .Contains(value, StringComparer.OrdinalIgnoreCase),
            _ => true // Accept other values as valid if present
        };
    }
}
