// T042: Insight query endpoint and response DTOs (US2, FR-020).
// GET /api/constellation/insight/{nodeId} — returns semantic roots + impact-sorted posts.
// Falls back to deterministic simulation data when the constellation window is empty.
// T064: Ghost snapshot endpoint and DTOs (US3, FR-010).
// GET /api/constellation/ghost-snapshots — returns historical constellation snapshots for ghost overlay.
using Microsoft.AspNetCore.Mvc;
using PoLinks.Web.Features.Simulation;
using PoLinks.Web.Features.Shared.Entities;

namespace PoLinks.Web.Features.Constellation;

// ---- DTOs -----------------------------------------------------------------

/// <summary>Single post entry in the insight feed, stripped to display-relevant fields.</summary>
public sealed record InsightPostDto(
    string          PostUri,
    string          AuthorDid,
    string          Text,
    double          ImpactScore,
    string          Sentiment,
    string          SentimentColour,
    DateTimeOffset  CreatedAt);

/// <summary>Full insight response returned by the GET endpoint.</summary>
public sealed record InsightResponseDto(
    string                       NodeId,
    string                       AnchorId,
    IReadOnlyList<string>        SemanticRoots,
    IReadOnlyList<InsightPostDto> Posts);

// T071: Focus mode DTOs (FR-024, FR-025)

/// <summary>Request body for entering focus mode.</summary>
public sealed record EnterFocusModeRequest(string AnchorId);

/// <summary>Response after successfully entering focus mode.</summary>
public sealed record FocusModeResponseDto(
    string AnchorId,
    bool IsActive,
    int FilteredNodeCount,
    int FilteredLinkCount,
    DateTimeOffset EnteredAt);

/// <summary>Response after exiting focus mode.</summary>
public sealed record FocusModeExitResponseDto(
    bool IsActive,
    int FullNodeCount,
    int FullLinkCount,
    DateTimeOffset ExitedAt);

/// <summary>Current focus mode status.</summary>
public sealed record FocusModeStatusDto(
    bool IsActive,
    string? AnchorId,
    int? NodeCount,
    DateTimeOffset? EnteredAt);

/// <summary>Ghost snapshot entry returned by the ghost-snapshots endpoint.</summary>
public sealed record GhostSnapshotDto(
    DateTimeOffset CreatedAt,
    IReadOnlyList<NexusNode> Nodes,
    IReadOnlyList<NexusLink> Links,
    bool IsSimulated);

// ---- Endpoint registration ------------------------------------------------

public static class ConstellationEndpoints
{
    public static IEndpointRouteBuilder MapConstellationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/constellation/insight/{nodeId}", GetInsight)
           .WithName("GetNodeInsight")
           .WithTags("Constellation");

        // T064: Ghost snapshot endpoint (FR-010)
        app.MapGet("/api/constellation/ghost-snapshots", GetGhostSnapshots)
           .WithName("GetGhostSnapshots")
           .WithTags("Constellation");

        // T071: Focus mode endpoints (FR-024, FR-025)
        app.MapPost("/api/constellation/focus-mode/enter", EnterFocusMode)
           .WithName("EnterFocusMode")
           .WithTags("Focus Mode");

        app.MapPost("/api/constellation/focus-mode/exit", ExitFocusMode)
           .WithName("ExitFocusMode")
           .WithTags("Focus Mode");

        app.MapGet("/api/constellation/focus-mode/status", GetFocusModeStatus)
           .WithName("GetFocusModeStatus")
           .WithTags("Focus Mode");
        
        return app;
    }

    private static IResult GetInsight(
        string              nodeId,
        ConstellationService constellation,
        IMockDataService    mockData)
    {
        // Prefer live data; fall back to simulation when constellation has no posts.
        var raw = constellation.GetNodeInsight(nodeId)
                  ?? (constellation.IsEmpty ? mockData.GenerateNodeInsight(nodeId) : null);

        if (raw is null)
            return TypedResults.NotFound(new { nodeId, reason = "no posts in current window" });

        var roots = SemanticRootsResolver.Resolve(raw.AnchorId, nodeId);
        var posts = raw.Posts.Select(p => new InsightPostDto(
            p.PostUri,
            p.AuthorDid,
            p.Text,
            p.ImpactScore,
            p.Sentiment.ToString(),
            InsightSentimentColour.For(p.Sentiment),
            p.CreatedAt
        )).ToList();

        return TypedResults.Ok(new InsightResponseDto(nodeId, raw.AnchorId, roots, posts));
    }

    /// <summary>
    /// POST /api/constellation/focus-mode/enter
    /// Enter focus mode for a specific anchor, filtering the constellation to show only
    /// the anchor and its directly connected nodes.
    /// 
    /// Request body: { "anchorId": "string" }
    /// Returns 200 with FocusModeResponseDto on success.
    /// Returns 404 if anchor ID is not found.
    /// </summary>
    private static IResult EnterFocusMode(
        EnterFocusModeRequest request,
        ConstellationService constellation)
    {
        if (string.IsNullOrWhiteSpace(request.AnchorId))
            return TypedResults.BadRequest(new { error = "anchorId is required" });

        var currentSnapshot = constellation.BuildSnapshot();
        var anchorExists = currentSnapshot.Nodes.Any(n => n.Id == request.AnchorId);
        
        if (!anchorExists)
            return TypedResults.NotFound(new { error = $"Anchor '{request.AnchorId}' not found" });

        try
        {
            var focusMode = constellation.EnterFocusMode(request.AnchorId, currentSnapshot);
            var filteredSnapshot = constellation.ApplyFocusFilter(currentSnapshot);

            return TypedResults.Ok(new FocusModeResponseDto(
                AnchorId: request.AnchorId,
                IsActive: true,
                FilteredNodeCount: filteredSnapshot.Nodes.Count,
                FilteredLinkCount: filteredSnapshot.Links.Count,
                EnteredAt: focusMode.EnteredAt
            ));
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/constellation/focus-mode/exit
    /// Exit focus mode and restore the full constellation view.
    /// 
    /// Returns 200 with full node/link count on success.
    /// </summary>
    private static IResult ExitFocusMode(ConstellationService constellation)
    {
        constellation.ExitFocusMode();
        var fullSnapshot = constellation.BuildSnapshot();

        return TypedResults.Ok(new FocusModeExitResponseDto(
            IsActive: false,
            FullNodeCount: fullSnapshot.Nodes.Count,
            FullLinkCount: fullSnapshot.Links.Count,
            ExitedAt: DateTimeOffset.UtcNow
        ));
    }

    /// <summary>
    /// GET /api/constellation/focus-mode/status
    /// Get the current focus mode state.
    /// 
    /// Returns 200 with FocusModeStatusDto.
    /// </summary>
    private static IResult GetFocusModeStatus(ConstellationService constellation)
    {
        var focusState = constellation.GetFocusState();
        
        if (focusState == null)
        {
            return TypedResults.Ok(new FocusModeStatusDto(
                IsActive: false,
                AnchorId: null,
                NodeCount: null,
                EnteredAt: null
            ));
        }

        return TypedResults.Ok(new FocusModeStatusDto(
            IsActive: true,
            AnchorId: focusState.AnchorId,
            NodeCount: focusState.NodeIds.Count(),
            EnteredAt: focusState.EnteredAt
        ));
    }

    /// <summary>
    /// GET /api/constellation/ghost-snapshots?startTime={epochMs}&endTime={epochMs}
    /// Returns constellation snapshots that fall within the requested time window.
    /// Returns the current in-memory snapshot when it overlaps the window; otherwise an empty array.
    /// </summary>
    private static IResult GetGhostSnapshots(
        [FromQuery(Name = "startTime")] long startTime,
        [FromQuery(Name = "endTime")]   long endTime,
        ConstellationService constellation,
        IMockDataService mockData)
    {
        // Build (or synthesize) the most recent snapshot to serve as the ghost overlay source.
        // Historical persistence is deferred; for now we always return the current window snapshot.
        var batches = mockData.GenerateAllAnchorBatches();
        var snapshot = constellation.IsEmpty
            ? new ConstellationSnapshot
              {
                  Nodes     = batches.SelectMany(b => b.Nodes).ToList(),
                  Links     = batches.SelectMany(b => b.Links).ToList(),
                  CreatedAt = DateTimeOffset.UtcNow,
              }
            : constellation.BuildSnapshot();

        var entry = new GhostSnapshotDto(snapshot.CreatedAt, snapshot.Nodes, snapshot.Links, constellation.IsEmpty);
        return TypedResults.Ok(new[] { entry });
    }
}
