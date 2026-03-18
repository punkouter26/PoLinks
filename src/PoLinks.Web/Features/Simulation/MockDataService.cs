// T096 (backend): MockDataService — deterministic constellation mock for Simulation Mode (FR-013, FR-030).
using PoLinks.Web.Features.Constellation;
using PoLinks.Web.Features.Shared.Entities;

namespace PoLinks.Web.Features.Simulation;

/// <summary>Provides deterministic mock PulseBatch data when no live posts are available.</summary>
public interface IMockDataService
{
    /// <summary>Generate a single PulseBatch for the given anchor using the given seed.</summary>
    PulseBatch GeneratePulseBatch(string anchorId, int seed = 42);

    /// <summary>Generate one PulseBatch per configured anchor.</summary>
    IReadOnlyList<PulseBatch> GenerateAllAnchorBatches();

    /// <summary>Generate a list of NexusNodes for the given anchor (for unit-test parity).</summary>
    IReadOnlyList<NexusNode> GenerateNodes(string anchorId, int seed = 42, int count = 30);

    /// <summary>
    /// Generate deterministic mock insight data for a node in Simulation Mode.
    /// Returns null only when <paramref name="nodeId"/> cannot be parsed to derive an anchor.
    /// </summary>
    NodeInsightData? GenerateNodeInsight(string nodeId);
}

/// <summary>Adapter so the unit test MockDataService helper calls can be made statically.</summary>
public static class MockDataService
{
    public static IReadOnlyList<NexusNode> GenerateNodes(int seed, string anchorId, int count = 30)
    {
        var rng = new Random(seed);
        const int cap = 100;
        var actualCount = Math.Min(count, cap);

        var anchor = new NexusNode
        {
            Id         = anchorId,
            Label      = anchorId,
            Type       = NodeType.Anchor,
            HypeScore  = 0,
            Elasticity = 1.0,
            AnchorId   = anchorId,
            FirstSeen  = DateTimeOffset.UtcNow,
            LastSeen   = DateTimeOffset.UtcNow,
        };

        var topics = Enumerable.Range(0, actualCount - 1).Select(i =>
        {
            var id    = $"{anchorId}-{seed}-{i:D3}";
            var hype  = rng.NextDouble() * 10.0;
            return new NexusNode
            {
                Id         = id,
                Label      = id,
                Type       = NodeType.Topic,
                HypeScore  = hype,
                Elasticity = ElasticityCalculator.Calculate(i + 1, actualCount - 1),
                AnchorId   = anchorId,
                FirstSeen  = DateTimeOffset.UtcNow.AddMinutes(-rng.Next(0, 60)),
                LastSeen   = DateTimeOffset.UtcNow,
            };
        }).ToList();

        return [anchor, .. topics];
    }
}

/// <summary>Default IMockDataService implementation bound to DI.</summary>
public sealed class DefaultMockDataService : IMockDataService
{
    private static readonly string[] AnchorIds =
        ["robotics", "dronetech", "ai", "autonomy", "sensors"];

    public PulseBatch GeneratePulseBatch(string anchorId, int seed = 42)
    {
        var nodes = MockDataService.GenerateNodes(seed, anchorId);
        var links = nodes.Where(n => n.Type == NodeType.Topic)
                         .Select(n => new NexusLink
                         {
                             SourceId = n.Id,
                             TargetId = anchorId,
                             Weight   = n.HypeScore,
                         }).ToList();
        return new PulseBatch
        {
            AnchorId    = anchorId,
            GeneratedAt = DateTimeOffset.UtcNow,
            Nodes       = nodes.ToList(),
            Links       = links,
            IsSimulated = true,
        };
    }

    public IReadOnlyList<PulseBatch> GenerateAllAnchorBatches() =>
        AnchorIds.Select(id => GeneratePulseBatch(id)).ToArray();

    public IReadOnlyList<NexusNode> GenerateNodes(string anchorId, int seed = 42, int count = 30) =>
        MockDataService.GenerateNodes(seed, anchorId, count);

    /// <inheritdoc/>
    public NodeInsightData? GenerateNodeInsight(string nodeId)
    {
        // Derive anchor from the nodeId prefix (e.g. "robotics-42-000" → "robotics").
        // Fall back to first known anchor when no prefix match.
        var anchor = AnchorIds.FirstOrDefault(a => nodeId.StartsWith(a, StringComparison.OrdinalIgnoreCase))
                     ?? AnchorIds[0];

        var seed = Math.Abs(nodeId.GetHashCode()) % 1000;
        var rng  = new Random(seed);

        var sentiments = new[] { SentimentLabel.Positive, SentimentLabel.Neutral, SentimentLabel.Negative };
        var posts = Enumerable.Range(0, 5).Select(i =>
        {
            var impact    = Math.Round(rng.NextDouble() * 10.0, 2);
            var sentiment = sentiments[rng.Next(sentiments.Length)];
            return new IngestedPost
            {
                PostUri         = $"at://{nodeId}/app.bsky.feed.post/{i:D4}",
                AuthorDid       = nodeId,
                Text            = MockPostTexts[i % MockPostTexts.Length],
                MatchedAnchorId = anchor,
                MatchedKeyword  = anchor,
                CreatedAt       = DateTimeOffset.UtcNow.AddMinutes(-rng.Next(0, 55)),
                ImpactScore     = impact,
                Sentiment       = sentiment,
            };
        })
        .OrderByDescending(p => p.ImpactScore)
        .ToList();

        return new NodeInsightData(anchor, posts);
    }

    private static readonly string[] MockPostTexts =
    [
        "Exciting advances in robotics this week — simulation results look promising.",
        "Reinforcement learning agent achieved 98% pass rate on obstacle course.",
        "New NVIDIA Isaac SDK update just dropped, seamless ROS2 integration.",
        "ML-Agents 3.0 brings physics-accurate reward shaping, game changer.",
        "Autonomous drone fleet completed first fully unsupervised mission.",
    ];
}
