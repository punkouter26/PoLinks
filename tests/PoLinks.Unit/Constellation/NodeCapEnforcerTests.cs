// T022: Unit tests for node cap eviction ordering (FR-003).
// 100-node hard cap; lowest HypeScore node evicted first on overflow.
using PoLinks.Web.Features.Constellation;
using PoLinks.Web.Features.Shared.Entities;

namespace PoLinks.Unit.Constellation;

public sealed class NodeCapEnforcerTests
{
    private const int HardCap = 100;

    [Fact]
    public void Enforce_BelowCap_DoesNotEvictAnyNodes()
    {
        var nodes = Enumerable.Range(1, 50)
            .Select(i => MakeNode($"t{i}", hypeScore: i))
            .ToList();

        var result = NodeCapEnforcer.Enforce(nodes, HardCap);

        result.Should().HaveCount(50);
    }

    [Fact]
    public void Enforce_ExactlyCap_DoesNotEvict()
    {
        var nodes = Enumerable.Range(1, HardCap)
            .Select(i => MakeNode($"t{i}", hypeScore: i))
            .ToList();

        var result = NodeCapEnforcer.Enforce(nodes, HardCap);

        result.Should().HaveCount(HardCap);
    }

    [Fact]
    public void Enforce_OverCap_EvictsLowestHypeScoreNodes()
    {
        // 101 nodes; the one with hypeScore=0 should be evicted
        var nodes = Enumerable.Range(0, HardCap + 1)
            .Select(i => MakeNode($"t{i}", hypeScore: (double)i))
            .ToList();

        var result = NodeCapEnforcer.Enforce(nodes, HardCap);

        result.Should().HaveCount(HardCap);
        result.Should().NotContain(n => n.HypeScore == 0.0);
    }

    [Fact]
    public void Enforce_OverCap_PreservesHighestHypeScoreNodes()
    {
        var nodes = Enumerable.Range(0, HardCap + 5)
            .Select(i => MakeNode($"t{i}", hypeScore: (double)i))
            .ToList();

        var result = NodeCapEnforcer.Enforce(nodes, HardCap);

        // The top 100 by HypeScore should all survive
        result.Select(n => n.HypeScore).Min().Should().BeGreaterThan(4.0);
    }

    [Fact]
    public void Enforce_AnchorNodes_NeverEvicted()
    {
        // Anchors always survive even with hypeScore=0
        var anchors = Enumerable.Range(1, 5)
            .Select(i => MakeNode($"anchor{i}", hypeScore: 0.0, type: NodeType.Anchor))
            .ToList();
        var topics = Enumerable.Range(1, HardCap)
            .Select(i => MakeNode($"topic{i}", hypeScore: (double)i))
            .ToList();

        var result = NodeCapEnforcer.Enforce(anchors.Concat(topics).ToList(), HardCap);

        result.Where(n => n.Type == NodeType.Anchor).Should().HaveCount(5);
    }

    private static NexusNode MakeNode(string id, double hypeScore, NodeType type = NodeType.Topic) =>
        new()
        {
            Id = id,
            Label = id,
            Type = type,
            HypeScore = hypeScore,
            Elasticity = 0.5,
            AnchorId = "anchor1",
            FirstSeen = DateTimeOffset.UtcNow,
            LastSeen = DateTimeOffset.UtcNow,
        };
}
