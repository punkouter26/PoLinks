// T050: Integration tests for correlationId propagation
using FluentAssertions;
using Xunit;
using PoLinks.Integration.Fixtures;

namespace PoLinks.Integration.Diagnostic;

[Collection(IntegrationCollection.Name)]
public class CorrelationIdIntegrationTests
{
    private readonly HttpClient _client;

    public CorrelationIdIntegrationTests(PoLinksWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsCorrelationIdHeader()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.Headers.Should().Contain(h => h.Key.Equals("x-correlation-id", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task HealthEndpoint_GeneratesUniqueCorrelationIds()
    {
        // Act
        var response1 = await _client.GetAsync("/health");
        var response2 = await _client.GetAsync("/health");

        // Assert
        var header1 = response1.Headers.FirstOrDefault(h => h.Key.Equals("x-correlation-id", StringComparison.OrdinalIgnoreCase)).Value?.FirstOrDefault();
        var header2 = response2.Headers.FirstOrDefault(h => h.Key.Equals("x-correlation-id", StringComparison.OrdinalIgnoreCase)).Value?.FirstOrDefault();

        header1.Should().NotBeNullOrEmpty();
        header2.Should().NotBeNullOrEmpty();
        header1.Should().NotBe(header2);
    }

    [Fact]
    public async Task HealthEndpoint_CorrelationIdIsGuid()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        var headerValue = response.Headers.FirstOrDefault(h => h.Key.Equals("x-correlation-id", StringComparison.OrdinalIgnoreCase)).Value?.FirstOrDefault();
        headerValue.Should().NotBeNull();
        
        // Should be valid GUID format
        Guid.TryParse(headerValue, out var result).Should().BeTrue();
    }

    [Fact]
    public async Task RequestWithProvidedCorrelationId_PreservesId()
    {
        // Arrange
        var testCorrelationId = Guid.NewGuid().ToString();
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("x-correlation-id", testCorrelationId);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        var responseHeaderValue = response.Headers.FirstOrDefault(h => h.Key.Equals("x-correlation-id", StringComparison.OrdinalIgnoreCase)).Value?.FirstOrDefault();
        responseHeaderValue.Should().Be(testCorrelationId);
    }

    [Fact]
    public async Task ConstellationEndpoint_IncludesCorrelationId()
    {
        // Act
        var response = await _client.GetAsync("/api/constellation");

        // Assert
        response.Headers.Should().Contain(h => h.Key.Equals("x-correlation-id", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task MultipleConsecutiveRequests_HaveDifferentCorrelationIds()
    {
        // Arrange
        var correlationIds = new List<string>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            var response = await _client.GetAsync("/health");
            var headerId = response.Headers.FirstOrDefault(h => h.Key.Equals("x-correlation-id", StringComparison.OrdinalIgnoreCase)).Value?.FirstOrDefault();
            if (headerId != null)
            {
                correlationIds.Add(headerId);
            }
        }

        // Assert
        correlationIds.Should().HaveCount(5);
        correlationIds.Distinct().Should().HaveCount(5); // All should be unique
    }
}
