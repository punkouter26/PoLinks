// Persists ingested posts (with sentiment) to Azure Table Storage for trend reporting.
// Table schema:
//   PartitionKey = YYYYMMDD   (day-level bucketing for efficient time-range queries)
//   RowKey       = sanitised PostUri (at:// path chars replaced with underscores)
//   Columns: AnchorId, Keyword, AuthorDid, Text (truncated 1 000 chars),
//            Sentiment, ImpactScore, CreatedAt
using Azure.Data.Tables;
using PoLinks.Web.Features.Shared.Entities;

namespace PoLinks.Web.Infrastructure.TableStorage;

/// <summary>Writes and queries <see cref="IngestedPost"/> records in Table Storage.</summary>
public interface IPostStorageRepository
{
    Task WriteAsync(IngestedPost post, CancellationToken ct = default);

    /// <summary>
    /// Returns ingestion counts by AnchorId for the given UTC date (partition = YYYYMMDD).
    /// Returns an empty dictionary when Table Storage is unavailable.
    /// </summary>
    Task<IReadOnlyDictionary<string, int>> GetDailyCountsAsync(DateOnly date, CancellationToken ct = default);
}

/// <summary>
/// Azure Table Storage implementation of <see cref="IPostStorageRepository"/>.
/// All writes use Upsert (Replace mode) so re-ingested duplicates are idempotent.
/// Errors are swallowed so a storage outage never blocks the constellation pipeline.
/// </summary>
public sealed class PostStorageRepository : IPostStorageRepository
{
    private readonly TableClient _table;
    private readonly ILogger<PostStorageRepository> _logger;

    public PostStorageRepository(TableServiceClient tableService, ILogger<PostStorageRepository> logger)
    {
        _table  = tableService.GetTableClient(TableStorageExtensions.PostsTable);
        _logger = logger;
    }

    public async Task WriteAsync(IngestedPost post, CancellationToken ct = default)
    {
        var entity = new TableEntity(
            partitionKey: post.CreatedAt.ToString("yyyyMMdd"),
            rowKey:       SanitizeRowKey(post.PostUri))
        {
            ["AnchorId"]    = post.MatchedAnchorId,
            ["Keyword"]     = post.MatchedKeyword,
            ["AuthorDid"]   = post.AuthorDid,
            ["Text"]        = post.Text.Length > 1_000 ? post.Text[..1_000] : post.Text,
            ["Sentiment"]   = post.Sentiment.ToString(),
            ["ImpactScore"] = post.ImpactScore,
            ["CreatedAt"]   = post.CreatedAt,
        };

        try
        {
            await _table.UpsertEntityAsync(entity, TableUpdateMode.Replace, ct);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist post {PostUri} to Table Storage", post.PostUri);
        }
    }

    public async Task<IReadOnlyDictionary<string, int>> GetDailyCountsAsync(DateOnly date, CancellationToken ct = default)
    {
        var partitionKey = date.ToString("yyyyMMdd");
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var filter = $"PartitionKey eq '{partitionKey}'";
            await foreach (var entity in _table.QueryAsync<TableEntity>(filter, select: ["AnchorId"], cancellationToken: ct))
            {
                var anchorId = entity.GetString("AnchorId") ?? "unknown";
                counts.TryGetValue(anchorId, out var existing);
                counts[anchorId] = existing + 1;
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query Table Storage daily counts for {Date}", partitionKey);
        }
        return counts;
    }

    /// <summary>
    /// Table Storage Row Keys cannot contain /  \  #  ?  control chars.
    /// PostUri format: at://did/app.bsky.feed.post/rkey — replace / with _
    /// and cap at 256 chars.
    /// </summary>
    private static string SanitizeRowKey(string key)
    {
        var sanitized = key
            .Replace('/', '_')
            .Replace('\\', '_')
            .Replace('#',  '_')
            .Replace('?',  '_');
        return sanitized.Length <= 256 ? sanitized : sanitized[..256];
    }
}
