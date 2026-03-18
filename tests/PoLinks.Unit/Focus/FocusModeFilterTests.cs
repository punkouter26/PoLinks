using PoLinks.Web.Features.Constellation;
using PoLinks.Web.Features.Shared.Entities;

namespace PoLinks.Unit.Focus;

public sealed class FocusModeFilterTests
{
    [Fact]
    public void ResolveSubgraph_ReturnsOnlyConnectedNodesAndLinks()
    {
        var resolver = new FocusSubgraphResolver();
        var snapshot = CreateSnapshot();

        var result = resolver.ResolveSubgraph(snapshot, "anchor-a");

        result.Nodes.Select(node => node.Id).Should().BeEquivalentTo(["anchor-a", "topic-a1", "topic-a2"]);
        result.Links.Should().HaveCount(2);
        result.Links.Should().OnlyContain(link => link.TargetId == "anchor-a" || link.SourceId == "anchor-a");
    }

    [Fact]
    public void ResolveSubgraph_WhenAnchorMissing_Throws()
    {
        var resolver = new FocusSubgraphResolver();

        var act = () => resolver.ResolveSubgraph(CreateSnapshot(), "missing-anchor");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetDistanceFromAnchor_ReturnsExpectedHopCount()
    {
        var resolver = new FocusSubgraphResolver();
        var snapshot = new ConstellationSnapshot
        {
            Nodes =
            [
                CreateNode("anchor-a", NodeType.Anchor, "anchor-a"),
                CreateNode("topic-a1", NodeType.Topic, "anchor-a"),
                CreateNode("topic-a2", NodeType.Topic, "anchor-a"),
            ],
            Links =
            [
                new NexusLink { SourceId = "topic-a1", TargetId = "anchor-a", Weight = 1 },
                new NexusLink { SourceId = "topic-a2", TargetId = "topic-a1", Weight = 1 },
            ],
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var distance = resolver.GetDistanceFromAnchor(snapshot, "anchor-a", "topic-a2");

        distance.Should().Be(2);
    }

    [Fact]
    public void CanExitFocusMode_WhenAnchorPresent_ReturnsTrue()
    {
        var resolver = new FocusSubgraphResolver();
        var focusMode = new FocusMode("anchor-a", ["anchor-a", "topic-a1"], DateTimeOffset.UtcNow);

        resolver.CanExitFocusMode(focusMode).Should().BeTrue();
    }

    private static ConstellationSnapshot CreateSnapshot() => new()
    {
        Nodes =
        [
            CreateNode("anchor-a", NodeType.Anchor, "anchor-a"),
            CreateNode("topic-a1", NodeType.Topic, "anchor-a"),
            CreateNode("topic-a2", NodeType.Topic, "anchor-a"),
            CreateNode("anchor-b", NodeType.Anchor, "anchor-b"),
            CreateNode("topic-b1", NodeType.Topic, "anchor-b"),
        ],
        Links =
        [
            new NexusLink { SourceId = "topic-a1", TargetId = "anchor-a", Weight = 1 },
            new NexusLink { SourceId = "topic-a2", TargetId = "anchor-a", Weight = 1 },
            new NexusLink { SourceId = "topic-b1", TargetId = "anchor-b", Weight = 1 },
        ],
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private static NexusNode CreateNode(string id, NodeType type, string anchorId) => new()
    {
        Id = id,
        Label = id,
        Type = type,
        HypeScore = 1,
        Elasticity = 1,
        AnchorId = anchorId,
        FirstSeen = DateTimeOffset.UtcNow,
        LastSeen = DateTimeOffset.UtcNow,
        AuthorDid = type == NodeType.Topic ? $"did:plc:{id}" : null,
    };
}
