// T030: 60-minute rolling constellation state service (FR-001, FR-002, FR-003).
// Maintains an in-memory sliding window of posts and produces NexusNode/NexusLink snapshots.
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using PoLinks.Web.Features.Shared.Entities;

namespace PoLinks.Web.Features.Constellation;

/// <summary>Configurable options for constellation behaviour (bound from appsettings "Constellation" section).</summary>
public sealed class ConstellationOptions
{
    public const string Section = "Constellation";

    /// <summary>Hard cap on Topic nodes per anchor. Lowest-HypeScore nodes are evicted first. Default: 100.</summary>
    public int MaxNodeCount { get; set; } = 100;

    /// <summary>How many minutes a post remains in the rolling window before eviction. Default: 60.</summary>
    public int NodeFadeWindowMinutes { get; set; } = 60;
}

/// <summary>
/// Maintains a thread-safe rolling window of ingested posts per anchor
/// and produces constellation graph snapshots (nodes + links) on demand.
/// </summary>
public sealed class ConstellationService(IOptions<ConstellationOptions> options)
{
    private readonly TimeSpan _windowDuration = TimeSpan.FromMinutes(options.Value.NodeFadeWindowMinutes);
    private readonly int _perAnchorNodeCap = options.Value.MaxNodeCount;

    private readonly ConcurrentDictionary<string, ConcurrentBag<IngestedPost>> _posts = new();

    /// <summary>Add a newly ingested post to the rolling window.</summary>
    public void AddPost(IngestedPost post)
    {
        var bucket = _posts.GetOrAdd(post.MatchedAnchorId, _ => new ConcurrentBag<IngestedPost>());
        bucket.Add(post);
    }

    /// <summary>
    /// Build the full constellation snapshot for all anchors.
    /// Evicts posts older than 60 minutes before computing scores.
    /// </summary>
    public ConstellationSnapshot BuildSnapshot()
    {
        var now = DateTimeOffset.UtcNow;
        var windowCutoff = now - _windowDuration;

        var allNodes = new List<NexusNode>();
        var allLinks = new List<NexusLink>();

        foreach (var (anchorId, bag) in _posts)
        {
            // Evict expired posts
            var live = bag.Where(p => p.CreatedAt >= windowCutoff).ToList();
            // Atomic swap: only replace if the bag reference hasn't changed since we read it.
            // Prevents racing with AddPost which may hold a reference to the old bag.
            _posts.TryUpdate(anchorId, new ConcurrentBag<IngestedPost>(live), bag);

            if (live.Count == 0) continue;

            var anchorNode = new NexusNode
            {
                Id        = anchorId,
                Label     = anchorId,
                Type      = NodeType.Anchor,
                HypeScore = 0,
                Elasticity = 1.0,
                AnchorId  = anchorId,
                FirstSeen = now,
                LastSeen  = now,
            };

            // Group by author to derive topic nodes
            var byAuthor = live.GroupBy(p => p.AuthorDid).ToList();
            var topicNodes = byAuthor.Select(g =>
            {
                var posts = g.ToList();
                var score = HypeScoreCalculator.Calculate(posts);
                var topKeyword = posts
                    .GroupBy(p => p.MatchedKeyword)
                    .MaxBy(grp => grp.Count())?.Key;
                return new NexusNode
                {
                    Id        = g.Key,
                    Label     = topKeyword ?? g.Key[..Math.Min(12, g.Key.Length)],
                    Type      = NodeType.Topic,
                    HypeScore = score,
                    Elasticity = 1.0, // recomputed after ranking below
                    AnchorId  = anchorId,
                    FirstSeen = posts.Min(p => p.CreatedAt),
                    LastSeen  = posts.Max(p => p.CreatedAt),
                    AuthorDid = g.Key,
                    TopKeyword = topKeyword,
                };
            }).ToList();

            // Cap + rank
            var combined = NodeCapEnforcer.Enforce(
                [anchorNode, .. topicNodes], _perAnchorNodeCap);
            var ranked    = combined.Where(n => n.Type == NodeType.Topic)
                                    .OrderByDescending(n => n.HypeScore)
                                    .Select((n, i) => n with
                                    {
                                        Elasticity = ElasticityCalculator.Calculate(i + 1, combined.Count - 1),
                                    })
                                    .ToList();

            allNodes.Add(anchorNode);
            allNodes.AddRange(ranked);

            // Links: each topic → its anchor
            allLinks.AddRange(ranked.Select(n => new NexusLink
            {
                SourceId = n.Id,
                TargetId = anchorId,
                Weight   = n.HypeScore,
            }));
        }

        return new ConstellationSnapshot
        {
            Nodes     = allNodes,
            Links     = allLinks,
            CreatedAt = now,
        };
    }

    /// <summary>
    /// Returns the posts for a specific node sorted by descending ImpactScore.
    /// Handles both anchor nodes (nodeId == anchorId keyword) and topic nodes
    /// (nodeId == AuthorDid). Returns null when the node has no posts in the
    /// current window.
    /// </summary>
    public NodeInsightData? GetNodeInsight(string nodeId)
    {
        var cutoff = DateTimeOffset.UtcNow - _windowDuration;

        // Anchor node: nodeId is the keyword (e.g. "robotics")
        if (_posts.TryGetValue(nodeId, out var anchorBag))
        {
            var anchorPosts = anchorBag
                .Where(p => p.CreatedAt >= cutoff)
                .OrderByDescending(p => p.ImpactScore)
                .ToList();
            if (anchorPosts.Count > 0)
                return new NodeInsightData(nodeId, anchorPosts);
        }

        // Topic node: nodeId is an AuthorDid
        foreach (var (anchorId, bag) in _posts)
        {
            var posts = bag
                .Where(p => p.CreatedAt >= cutoff && p.AuthorDid == nodeId)
                .OrderByDescending(p => p.ImpactScore)
                .ToList();
            if (posts.Count > 0)
                return new NodeInsightData(anchorId, posts);
        }
        return null;
    }

    /// <summary>Returns true if there are no live posts in the current window.</summary>
    public bool IsEmpty => _posts.Values.All(b => b.IsEmpty);

    /// <summary>Number of anchor buckets with any posts (for diagnostics).</summary>
    public int PostBucketCount => _posts.Count;

    // -----------------------------------------------------------------------
    // T071: Focus Mode State Management (FR-024, FR-025)
    // -----------------------------------------------------------------------

    private readonly object _focusLock = new();
    private FocusMode? _focusState;

    /// <summary>Get the current focus mode state (null if inactive).</summary>
    public FocusMode? GetFocusState() { lock (_focusLock) return _focusState; }

    /// <summary>Enter focus mode for a specific anchor. Returns the filtered subgraph.</summary>
    public FocusMode EnterFocusMode(string anchorId, ConstellationSnapshot currentSnapshot)
    {
        var subgraph = ResolveSubgraph(currentSnapshot, anchorId);
        var focusState = new FocusMode(
            AnchorId: anchorId,
            NodeIds: subgraph.Nodes.Select(n => n.Id),
            EnteredAt: DateTimeOffset.UtcNow);
        lock (_focusLock) { _focusState = focusState; }
        return focusState;
    }

    /// <summary>Exit focus mode and return to full constellation view.</summary>
    public void ExitFocusMode()
    {
        lock (_focusLock) { _focusState = null; }
    }

    /// <summary>
    /// Apply focus mode filtering to a constellation snapshot.
    /// Returns the full snapshot if focus mode is inactive.
    /// </summary>
    public ConstellationSnapshot ApplyFocusFilter(ConstellationSnapshot snapshot)
    {
        FocusMode? focusState;
        lock (_focusLock) { focusState = _focusState; }
        if (focusState == null) return snapshot;

        var focusedNodeIds = new HashSet<string>(focusState.NodeIds);
        var filteredNodes = snapshot.Nodes.Where(n => focusedNodeIds.Contains(n.Id)).ToList();
        var filteredLinks = snapshot.Links
            .Where(l => focusedNodeIds.Contains(l.SourceId) && focusedNodeIds.Contains(l.TargetId))
            .ToList();

        return new ConstellationSnapshot
        {
            Nodes = filteredNodes,
            Links = filteredLinks,
            CreatedAt = snapshot.CreatedAt,
        };
    }

    // -----------------------------------------------------------------------
    // BFS subgraph resolver (inlined from FocusSubgraphResolver — S2)
    // -----------------------------------------------------------------------

    private static ConstellationSnapshot ResolveSubgraph(ConstellationSnapshot constellation, string anchorId)
    {
        var anchor = constellation.Nodes.FirstOrDefault(n => n.Id == anchorId && n.Type == NodeType.Anchor);
        if (anchor == null)
            throw new ArgumentException($"Anchor '{anchorId}' not found in constellation", nameof(anchorId));

        var connectedIds = BfsConnectedNodes(constellation, anchorId);

        return new ConstellationSnapshot
        {
            Nodes  = constellation.Nodes.Where(n => connectedIds.Contains(n.Id)).ToList(),
            Links  = constellation.Links.Where(l => connectedIds.Contains(l.SourceId) && connectedIds.Contains(l.TargetId)).ToList(),
            CreatedAt = constellation.CreatedAt,
        };
    }

    private static HashSet<string> BfsConnectedNodes(ConstellationSnapshot constellation, string startId)
    {
        var visited = new HashSet<string> { startId };
        var queue   = new Queue<string>();
        queue.Enqueue(startId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var neighbours = constellation.Links
                .Where(l => l.SourceId == current).Select(l => l.TargetId)
                .Union(constellation.Links.Where(l => l.TargetId == current).Select(l => l.SourceId));

            foreach (var id in neighbours.Where(id => visited.Add(id)))
                queue.Enqueue(id);
        }

        return visited;
    }
}

/// <summary>Live insight data for a single node: which anchor it belongs to and its posts.</summary>
public sealed record NodeInsightData(string AnchorId, IReadOnlyList<IngestedPost> Posts);

/// <summary>Immutable snapshot of the constellation at a point in time.</summary>
public sealed record ConstellationSnapshot
{
    public required IReadOnlyList<NexusNode> Nodes { get; init; }
    public required IReadOnlyList<NexusLink> Links { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
