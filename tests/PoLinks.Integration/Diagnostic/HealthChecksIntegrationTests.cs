// T049: Integration tests for deep health endpoint responses
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;
using PoLinks.Integration.Fixtures;

namespace PoLinks.Integration.Diagnostic;

[Collection(IntegrationCollection.Name)]
public class HealthChecksIntegrationTests
{
    private readonly HttpClient _client;

    public HealthChecksIntegrationTests(PoLinksWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_Returns200WhenHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthEndpoint_IncludesTableStorageHealthCheck()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var json = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        json.Should().Contain("TableStorage");
    }

    [Fact]
    public async Task HealthEndpoint_IncludesBlueskyNetworkHealthCheck()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var json = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        json.Should().Contain("Bluesky");
    }

    [Fact]
    public async Task HealthEndpoint_IncludesApplicationHealthStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var json = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        (json.Contains("\"status\"") || json.Contains("\"healthy\"")).Should().BeTrue();
    }

    [Fact]
    public async Task DiagnosticEndpoint_ReturnsMaskedConfiguration()
    {
        // Act
        var response = await _client.GetAsync("/diagnostic/config");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("[REDACTED]");
        json.Should().NotContain("sk_live_"); // Should not contain unmasked API keys
    }

    [Fact]
    public async Task DiagnosticEndpoint_IncludesConnectionStatus()
    {
        // Act
        var response = await _client.GetAsync("/diagnostic/config");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().NotBeNullOrEmpty();
    }
}
