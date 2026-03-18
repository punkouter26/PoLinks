// Unit tests for TableStorageHealthCheck — covers the managed-identity (TableServiceUri)
// and connection-string paths added/modified to fix the production Degraded status.
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using PoLinks.Web.Features.Diagnostic.HealthChecks;

namespace PoLinks.Unit.Diagnostic;

public class TableStorageHealthCheckTests
{
    private static TableStorageHealthCheck CreateSut(Dictionary<string, string?> config)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(config)
            .Build();

        return new TableStorageHealthCheck(configuration, NullLogger<TableStorageHealthCheck>.Instance);
    }

    private static HealthCheckContext MakeContext() =>
        new() { Registration = new HealthCheckRegistration("TableStorage", _ => null!, null, null) };

    // ----- AzureStorage:TableServiceUri (production / managed identity) path -----

    [Fact]
    public async Task CheckHealthAsync_WithValidTableServiceUri_ReturnsHealthy()
    {
        var sut = CreateSut(new() { ["AzureStorage:TableServiceUri"] = "https://mystorageaccount.table.core.windows.net/" });

        var result = await sut.CheckHealthAsync(MakeContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("managed identity");
    }

    [Fact]
    public async Task CheckHealthAsync_WithInvalidTableServiceUri_ReturnsDegraded()
    {
        var sut = CreateSut(new() { ["AzureStorage:TableServiceUri"] = "not-a-valid-uri" });

        var result = await sut.CheckHealthAsync(MakeContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("not a valid URI");
    }

    // ----- AzureStorage:ConnectionString (local / Azurite) path -----

    [Fact]
    public async Task CheckHealthAsync_WithAzuriteConnectionString_ReturnsHealthy()
    {
        var sut = CreateSut(new() { ["AzureStorage:ConnectionString"] = "UseDevelopmentStorage=true" });

        var result = await sut.CheckHealthAsync(MakeContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("Azurite");
    }

    [Fact]
    public async Task CheckHealthAsync_WithNoConfiguration_ReturnsDegraded()
    {
        var sut = CreateSut(new());

        var result = await sut.CheckHealthAsync(MakeContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("not configured");
    }

    // ----- TableServiceUri takes priority over ConnectionString -----

    [Fact]
    public async Task CheckHealthAsync_WhenBothConfigured_TableServiceUriTakesPriority()
    {
        var sut = CreateSut(new()
        {
            ["AzureStorage:TableServiceUri"] = "https://mystorageaccount.table.core.windows.net/",
            ["AzureStorage:ConnectionString"] = "UseDevelopmentStorage=true"
        });

        var result = await sut.CheckHealthAsync(MakeContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("managed identity");
    }
}
