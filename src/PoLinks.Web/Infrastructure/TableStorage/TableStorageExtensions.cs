// T014: Azure Table Storage DI extensions (FR-012).
// Registers the TableServiceClient and creates the required tables at startup.
using Azure.Data.Tables;
using Azure.Identity;

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
    /// Adds a <see cref="TableServiceClient"/>.
    /// In Azure: uses <c>AzureStorage:TableServiceUri</c> with <see cref="DefaultAzureCredential"/>
    /// (managed identity, <c>allowSharedKeyAccess: false</c>).
    /// Locally: falls back to <c>AzureStorage:ConnectionString</c> (Azurite).
    /// </summary>
    public static IServiceCollection AddTableStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var tableServiceUri = configuration["AzureStorage:TableServiceUri"];

        TableServiceClient client;
        if (!string.IsNullOrEmpty(tableServiceUri))
        {
            // Azure deployment: keyless auth via managed identity / workload identity
            client = new TableServiceClient(new Uri(tableServiceUri), new DefaultAzureCredential());
        }
        else
        {
            // Local development: Azurite connection string
            var connectionString = configuration["AzureStorage:ConnectionString"]
                ?? throw new InvalidOperationException(
                    "Either AzureStorage:TableServiceUri (Azure) or AzureStorage:ConnectionString (local) must be configured.");

            client = new TableServiceClient(connectionString);
        }

        services.AddSingleton(client);
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
            try
            {
                await _client.CreateTableIfNotExistsAsync(table, cancellationToken);
                _logger.LogInformation("Ensured table {TableName} exists", table);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ensure table {TableName} exists — check storage credentials and connectivity", table);
                throw;
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
