// T014: Azure Table Storage DI extensions (FR-012).
// Registers the TableServiceClient and creates the required tables at startup.
using Azure.Data.Tables;

namespace PoLinks.Web.Infrastructure.TableStorage;

/// <summary>
/// Extension methods to register Azure Table Storage and ensure required tables exist.
/// </summary>
public static class TableStorageExtensions
{
    public const string PulseTable  = "PulseBatches";
    public const string AnchorTable = "AnchorNodes";
    public const string PostsTable  = "IngestedPosts";

    /// <summary>
    /// Adds a <see cref="TableServiceClient"/> from <c>AzureStorage:ConnectionString</c>
    /// and registers a startup task that creates the required tables if they don't exist.
    /// </summary>
    public static IServiceCollection AddTableStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration["AzureStorage:ConnectionString"]
            ?? throw new InvalidOperationException(
                "AzureStorage:ConnectionString is required but was not configured.");

        services.AddSingleton(new TableServiceClient(connectionString));
        services.AddHostedService<TableStorageInitializer>();

        return services;
    }
}

/// <summary>
/// Ensures required tables exist in Azure Table Storage on startup.
/// Runs once during application start and then exits (not a continuous background service).
/// </summary>
internal sealed class TableStorageInitializer : IHostedService
{
    private readonly TableServiceClient _client;
    private readonly ILogger<TableStorageInitializer> _logger;

    public TableStorageInitializer(
        TableServiceClient client,
        ILogger<TableStorageInitializer> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var table in new[] { TableStorageExtensions.PulseTable, TableStorageExtensions.AnchorTable, TableStorageExtensions.PostsTable })
        {
            await _client.CreateTableIfNotExistsAsync(table, cancellationToken);
            _logger.LogInformation("Ensured table {TableName} exists", table);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
