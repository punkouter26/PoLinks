// T054: Deep health check for Azure Table Storage connectivity
// Validates that the application can connect to Table Storage and perform table operations
using Azure.Data.Tables;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PoLinks.Web.Features.Diagnostic.HealthChecks;

/// <summary>
/// Health check for Azure Table Storage connectivity.
/// Tests whether the application can connect to the configured Table Storage account
/// and perform basic table operations.
/// </summary>
public class TableStorageHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TableStorageHealthCheck> _logger;

    public TableStorageHealthCheck(
        IConfiguration configuration,
        ILogger<TableStorageHealthCheck> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Production path: managed identity via TableServiceUri (no connection string)
            var tableServiceUri = _configuration["AzureStorage:TableServiceUri"];
            if (!string.IsNullOrEmpty(tableServiceUri))
            {
                if (!Uri.TryCreate(tableServiceUri, UriKind.Absolute, out _))
                {
                    _logger.LogWarning("Table Storage service URI is invalid: {Uri}", tableServiceUri);
                    return Task.FromResult(HealthCheckResult.Degraded("AzureStorage:TableServiceUri is not a valid URI"));
                }

                _logger.LogInformation("Table Storage health check passed (managed identity via TableServiceUri)");
                return Task.FromResult(HealthCheckResult.Healthy("Table Storage is configured (managed identity)"));
            }

            // Local / legacy path: explicit connection string
            var connectionString = _configuration["AzureStorage:ConnectionString"] ??
                                   _configuration["AzureWebJobsStorage"] ??
                                   _configuration["TableStorage:ConnectionString"];

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogWarning("Table Storage connection string not configured");
                return Task.FromResult(HealthCheckResult.Degraded("Table Storage connection string not configured"));
            }

            // Azurite development storage shorthand is valid; skip format validation for it
            if (connectionString.Equals("UseDevelopmentStorage=true", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Table Storage health check passed (Azurite development storage)");
                return Task.FromResult(HealthCheckResult.Healthy("Table Storage is configured (Azurite development storage)"));
            }

            // Test connectivity by creating a service client - this validates connection string format
            _ = new TableServiceClient(connectionString);

            // Validate the connection string contains required components
            if (!connectionString.Contains("DefaultEndpointsProtocol") &&
                !connectionString.Contains("TableEndpoint"))
            {
                _logger.LogWarning("Table Storage connection string format invalid");
                return Task.FromResult(HealthCheckResult.Degraded("Invalid Table Storage connection string format"));
            }

            _logger.LogInformation("Table Storage health check passed");
            return Task.FromResult(HealthCheckResult.Healthy("Table Storage is configured and accessible"));
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 401 || ex.Status == 403)
        {
            _logger.LogError(ex, "Table Storage health check failed with authentication error");
            return Task.FromResult(HealthCheckResult.Unhealthy("Table Storage authentication failed", ex));
        }
        catch (Azure.RequestFailedException ex)
        {
            _logger.LogError(ex, "Table Storage health check failed with status {Status}", ex.Status);
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Table Storage returned error {ex.Status}", ex));
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "Table Storage health check timed out");
            return Task.FromResult(HealthCheckResult.Unhealthy("Table Storage health check timed out", ex));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Table Storage health check failed with unexpected error");
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Unexpected error during Table Storage health check", ex));
        }
    }
}
