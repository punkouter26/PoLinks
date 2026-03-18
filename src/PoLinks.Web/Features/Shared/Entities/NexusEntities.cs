// T009: Shared entity contracts used across all features.
// These are the core domain types; no business logic here — pure data contracts.
namespace PoLinks.Web.Features.Shared.Entities;

/// <summary>
/// Identifies the type of node in the constellation graph.
/// </summary>
public enum NodeType
{
    /// <summary>
    /// Anchor nodes represent the 5 keyword topics (e.g. "robotics").
    /// They are fixed gravity wells in the physics simulation.
    /// </summary>
    Anchor,

    /// <summary>
    /// Topic nodes orbit Anchors; created from individual ingested posts.
    /// Subject to the 100-node hard cap and Hype Score eviction (FR-003).
    /// </summary>
    Topic,
}

/// <summary>
/// A single node in the constellation. Immutable after creation; state is rebuilt
/// from the rolling 60-minute window on each pulse cycle.
/// </summary>
public sealed record NexusNode
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public required NodeType Type { get; init; }
    public required double HypeScore { get; init; }
    public required double Elasticity { get; init; }
    public required string AnchorId { get; init; }
    public required DateTimeOffset FirstSeen { get; init; }
    public required DateTimeOffset LastSeen { get; init; }
    public string? AuthorDid { get; init; }
}

/// <summary>
/// Directional edge between two nodes. Weight reflects sentiment co-occurrence strength.
/// </summary>
public sealed record NexusLink
{
    public required string SourceId { get; init; }
    public required string TargetId { get; init; }
    public required double Weight { get; init; }
}

/// <summary>
/// A single real-time pulse batch broadcast over SignalR (JSON-serialised).
/// Consumers should treat this as a full graph snapshot for the current window.
/// </summary>
public sealed record PulseBatch
{
    public string AnchorId { get; set; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; set; }
    public List<NexusNode> Nodes { get; set; } = [];
    public List<NexusLink> Links { get; set; } = [];
    public bool IsSimulated { get; set; }
}

/// <summary>
/// Sentiment classification for an ingested post.
/// </summary>
public enum SentimentLabel
{
    Positive,
    Neutral,
    Negative,
}

/// <summary>
/// Raw ingested post after keyword filtering and before constellation state update.
/// </summary>
public sealed record IngestedPost
{
    public required string PostUri { get; init; }
    public required string AuthorDid { get; init; }
    public required string Text { get; init; }
    public required string MatchedAnchorId { get; init; }
    public required string MatchedKeyword { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public SentimentLabel Sentiment { get; init; } = SentimentLabel.Neutral;
    public double ImpactScore { get; init; }
}

/// <summary>
/// Persisted snapshot row in Azure Table Storage.
/// PartitionKey = YYYYMMDD, RowKey = PostUri (sanitised).
/// </summary>
public sealed record PulseStorageEntity
{
    public required string PartitionKey { get; init; }
    public required string RowKey { get; init; }
    public required string PostUri { get; init; }
    public required string AnchorId { get; init; }
    public required string Keyword { get; init; }
    public required double HypeScore { get; init; }
    public required string SentimentLabel { get; init; }
    public required double ImpactScore { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}
