// T019: Shared integration test fixtures.
// Provides a WebApplicationFactory and a live Azurite container for integration tests.
using Azure.Data.Tables;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.Azurite;
using Testcontainers.MsSql;

namespace PoLinks.Integration.Fixtures;

/// <summary>
/// Collection marker that tells xUnit all tests in this collection share one
/// <see cref="PoLinksWebAppFactory"/> instance (avoids redundant container starts).
/// </summary>
[CollectionDefinition(Name)]
public sealed class IntegrationCollection : ICollectionFixture<PoLinksWebAppFactory>
{
    public const string Name = "Integration";
}

/// <summary>
/// Spins up an Azurite container and an in-process ASP.NET Core test server.
/// Tests should inject <see cref="HttpClient"/> via the factory.
/// </summary>
public sealed class PoLinksWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly AzuriteContainer _azurite = new AzuriteBuilder()
        .WithImage("mcr.microsoft.com/azure-storage/azurite:latest")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(10002))
        .Build();

    private readonly MsSqlContainer _sql = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public TableServiceClient TableServiceClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _azurite.StartAsync();
        await _sql.StartAsync();
        TableServiceClient = new TableServiceClient(_azurite.GetConnectionString());
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _azurite.DisposeAsync();
        await _sql.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("AzureStorage:ConnectionString", _azurite.GetConnectionString());
        builder.UseSetting("ConnectionStrings:Sql", _sql.GetConnectionString());
        // Override AI keys to prevent real calls during integration tests
        builder.UseSetting("AzureAI:Endpoint", "https://example.invalid");
        builder.UseSetting("AzureAI:ApiKey", "test-key");
        builder.UseSetting("AzureAI:ModelId", "phi-4");
        // Point Jetstream at an unreachable endpoint so the worker falls back to
        // Simulation Mode — makes pulse broadcast tests deterministic (no live wait).
        builder.UseSetting("Jetstream:Endpoint", "wss://invalid.example");
    }
}
