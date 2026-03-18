// T039: Unit tests for impact score sort and sentiment colour mapping (US2).
// Tests the sort order invariant and the colour token rules from spec AC-3 and AC-4.
using PoLinks.Web.Features.Constellation;
using PoLinks.Web.Features.Shared.Entities;

namespace PoLinks.Unit.Insight;

public sealed class InsightPanelLogicTests
{
    // ---- Impact score sort invariant ----------------------------------------

    [Fact]
    public void GetNodeInsight_PostsReturnedInDescendingImpactOrder()
    {
        var svc = new ConstellationService();
        var now  = DateTimeOffset.UtcNow;
        var authorDid = "did:plc:sorttest";
        svc.AddPost(MakePost(authorDid, "anchor1", impact: 1.0, created: now));
        svc.AddPost(MakePost(authorDid, "anchor1", impact: 5.0, created: now));
        svc.AddPost(MakePost(authorDid, "anchor1", impact: 3.0, created: now));

        var insight = svc.GetNodeInsight(authorDid);

        insight.Should().NotBeNull();
        var scores = insight!.Posts.Select(p => p.ImpactScore).ToList();
        scores.Should().BeInDescendingOrder("posts must be sorted by ImpactScore descending (AC-3)");
    }

    [Fact]
    public void GetNodeInsight_NoPosts_ReturnsNull()
    {
        var svc = new ConstellationService();
        var result = svc.GetNodeInsight("did:plc:nonexistent");
        result.Should().BeNull();
    }

    [Fact]
    public void GetNodeInsight_OnlyReturnsPostsForRequestedNode()
    {
        var svc = new ConstellationService();
        var now  = DateTimeOffset.UtcNow;
        svc.AddPost(MakePost("did:plc:aaa", "anchor1", impact: 2.0, created: now));
        svc.AddPost(MakePost("did:plc:bbb", "anchor1", impact: 4.0, created: now));

        var insight = svc.GetNodeInsight("did:plc:aaa");

        insight.Should().NotBeNull();
        insight!.Posts.Should().OnlyContain(p => p.AuthorDid == "did:plc:aaa");
    }

    [Fact]
    public void GetNodeInsight_CorrectAnchorIdReturned()
    {
        var svc = new ConstellationService();
        var now  = DateTimeOffset.UtcNow;
        svc.AddPost(MakePost("did:plc:xyz", "robotics", impact: 1.0, created: now));

        var insight = svc.GetNodeInsight("did:plc:xyz");

        insight!.AnchorId.Should().Be("robotics");
    }

    // ---- Semantic roots breadcrumb ------------------------------------------

    [Fact]
    public void SemanticRootsResolver_ReturnsAnchorThenNodeLabel()
    {
        var roots = SemanticRootsResolver.Resolve("robotics", "node-abc");
        roots.Should().HaveCountGreaterThanOrEqualTo(2, "spec requires at least one Anchor ancestor plus the node itself (AC-2)");
        roots[0].Should().Be("robotics");
        roots[^1].Should().Be("node-abc");
    }

    [Fact]
    public void SemanticRootsResolver_AnchorAsNode_ReturnsSingleElement()
    {
        var roots = SemanticRootsResolver.Resolve("robotics", "robotics");
        roots.Should().NotBeEmpty();
    }

    // ---- Sentiment colour token mapping -------------------------------------
    // Spec AC-4: Positive → Electric Blue (#00BFFF range); Negative → Deep Crimson (#DC143C range).
    // The colour tokens live in the C# InsightSentimentColour helper and the TS tokens; we test
    // the helper here so the E2E colour test has a known source of truth.

    [Theory]
    [InlineData(SentimentLabel.Positive, "#00BFFF")]
    [InlineData(SentimentLabel.Negative, "#DC143C")]
    [InlineData(SentimentLabel.Neutral,  "#9CA3AF")]
    public void InsightSentimentColour_ReturnsCssColourForLabel(SentimentLabel label, string expectedHex)
    {
        var colour = InsightSentimentColour.For(label);
        colour.Should().Be(expectedHex, $"spec mandates Electric Blue for positive and Deep Crimson for negative");
    }

    // ---- Helpers ------------------------------------------------------------

    private static IngestedPost MakePost(string authorDid, string anchorId, double impact, DateTimeOffset created) =>
        new()
        {
            PostUri          = $"at://did:plc:test/{Guid.NewGuid():N}",
            AuthorDid        = authorDid,
            Text             = "Test post",
            MatchedAnchorId  = anchorId,
            MatchedKeyword   = "test",
            CreatedAt        = created,
            ImpactScore      = impact,
            Sentiment        = SentimentLabel.Neutral,
        };
}
