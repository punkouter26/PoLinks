// T029: Ingestion DTO mapping and keyword filtering (FR-004, FR-005).
// Maps raw Bluesky Jetstream JSON events to IngestedPost domain records.
namespace PoLinks.Web.Features.Ingestion;

/// <summary>Raw Jetstream commit event as received over WebSocket.</summary>
internal sealed record JetstreamEvent
{
    public string Did { get; init; } = string.Empty;
    [System.Text.Json.Serialization.JsonPropertyName("time_us")]
    public long TimeUs { get; init; }
    public string Kind { get; init; } = string.Empty;
    public JetstreamCommit? Commit { get; init; }
}

internal sealed record JetstreamCommit
{
    public string Collection { get; init; } = string.Empty;
    public string Operation { get; init; } = string.Empty;
    public JetstreamRecord? Record { get; init; }
    public string Rkey { get; init; } = string.Empty;
}

internal sealed record JetstreamRecord
{
    public string? Text { get; init; }
    public string? CreatedAt { get; init; }
}

/// <summary>Represents a configured anchor keyword for ingestion filtering.</summary>
public sealed record AnchorConfig
{
    public required string AnchorId { get; init; }
    public required IReadOnlyList<string> Keywords { get; init; }
}
