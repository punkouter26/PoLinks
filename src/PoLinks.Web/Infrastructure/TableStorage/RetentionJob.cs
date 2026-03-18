// T015: 90-day data retention cleanup job (FR-013).
// Runs hourly and deletes PulseBatch rows older than 90 days from Table Storage.
using Azure.Data.Tables;

namespace PoLinks.Web.Infrastructure.TableStorage;

/// <summary>
/// Background service that enforces the 90-day data retention policy (FR-013).
/// Runs once per hour and deletes all rows whose PartitionKey date is older than 90 days.
/// The PartitionKey format is YYYYMMDD so lexicographic comparison is sufficient.
/// </summary>
public sealed class RetentionJob : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);
    private const int RetentionDays = 90;

    private readonly TableServiceClient _tableService;
    private readonly ILogger<RetentionJob> _logger;

    public RetentionJob(
        TableServiceClient tableService,
        ILogger<RetentionJob> logger)
    {
        _tableService = tableService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PurgeOldRowsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Retention job failed; will retry in {Interval}", Interval);
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task PurgeOldRowsAsync(CancellationToken ct)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-RetentionDays);
        // PartitionKey is YYYYMMDD; string comparison works because format is zero-padded ISO
        var cutoffKey = cutoff.ToString("yyyyMMdd");

        var filter  = $"PartitionKey lt '{cutoffKey}'";
        var deleted = 0;

        foreach (var tableName in new[] { TableStorageExtensions.PulseTable, TableStorageExtensions.PostsTable })
        {
            var table = _tableService.GetTableClient(tableName);
            await foreach (var entity in table.QueryAsync<TableEntity>(filter, cancellationToken: ct))
            {
                await table.DeleteEntityAsync(entity.PartitionKey, entity.RowKey, cancellationToken: ct);
                deleted++;
            }
        }

        if (deleted > 0)
        {
            _logger.LogInformation(
                "Retention job: deleted {Count} rows older than {Cutoff:yyyy-MM-dd}",
                deleted, cutoff);
        }
    }
}
