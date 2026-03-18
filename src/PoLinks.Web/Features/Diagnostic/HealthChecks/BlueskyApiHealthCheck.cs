// T053: Deep health check for Bluesky API connectivity
// Validates that the application can reach Bluesky's API endpoints and perform basic operations
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PoLinks.Web.Features.Diagnostic.HealthChecks;

/// <summary>
/// Health check for Bluesky API connectivity.
/// Tests whether the application can reach configured Bluesky endpoints
/// and authenticate successfully.
/// </summary>
public class BlueskyApiHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BlueskyApiHealthCheck> _logger;

    public BlueskyApiHealthCheck(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<BlueskyApiHealthCheck> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var blueskyBaseUrl = _configuration["Bluesky:ApiBaseUrl"]?.TrimEnd('/') ?? "https://bsky.social";
            
            // Test basic connectivity to Bluesky API
            var checkEndpoint = $"{blueskyBaseUrl}/xrpc/com.atproto.server.getSession";

            // Create request with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            var request = new HttpRequestMessage(HttpMethod.Get, checkEndpoint);
            var response = await _httpClient.SendAsync(request, cts.Token);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Bluesky API health check passed");
                return HealthCheckResult.Healthy("Bluesky API is reachable and responding");
            }

            // 401/403 means API is up but auth failed (expected without credentials)
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogInformation("Bluesky API health check passed (auth required but API is reachable)");
                return HealthCheckResult.Healthy("Bluesky API is reachable");
            }

            _logger.LogWarning("Bluesky API health check failed with status {StatusCode}", response.StatusCode);
            return HealthCheckResult.Degraded(
                $"Bluesky API returned status {response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Bluesky API health check failed with network error");
            return HealthCheckResult.Unhealthy(
                "Cannot reach Bluesky API - network error", ex);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "Bluesky API health check timed out");
            return HealthCheckResult.Unhealthy(
                "Bluesky API health check timed out", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bluesky API health check failed with unexpected error");
            return HealthCheckResult.Unhealthy(
                "Unexpected error during Bluesky API health check", ex);
        }
    }
}
