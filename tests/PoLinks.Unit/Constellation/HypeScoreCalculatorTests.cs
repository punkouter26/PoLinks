// T021: Unit tests for Hype Score calculation and elasticity rules (FR-002).
// Tests are written first (TDD) - implementation follows in T030/T031.
using PoLinks.Web.Features.Constellation;
using PoLinks.Web.Features.Shared.Entities;

namespace PoLinks.Unit.Constellation;

public sealed class HypeScoreCalculatorTests
{
    // --- Hype Score formula: Σ(impactScore × sentimentMultiplier) per post in 60-min window ---
    // Sentiment multipliers: Positive=1.5, Neutral=1.0, Negative=0.5 (FR-002)

    [Fact]
    public void HypeScore_SinglePositivePost_ReturnsImpactTimesPositiveMultiplier()
    {
        // The formula: hypeScore = Σ(impact × multiplier)
        // 1 positive post with impact=1.0 → 1.0 × 1.5 = 1.5
        var posts = new[] { MakePost(impact: 1.0, sentiment: SentimentLabel.Positive) };
        var score = HypeScoreCalculator.Calculate(posts);
        score.Should().BeApproximately(1.5, precision: 0.001);
    }

    [Fact]
    public void HypeScore_SingleNeutralPost_ReturnsImpactTimesNeutralMultiplier()
    {
        var posts = new[] { MakePost(impact: 2.0, sentiment: SentimentLabel.Neutral) };
        var score = HypeScoreCalculator.Calculate(posts);
        score.Should().BeApproximately(2.0, precision: 0.001);
    }

    [Fact]
    public void HypeScore_SingleNegativePost_ReturnsImpactTimesNegativeMultiplier()
    {
        var posts = new[] { MakePost(impact: 4.0, sentiment: SentimentLabel.Negative) };
        var score = HypeScoreCalculator.Calculate(posts);
        score.Should().BeApproximately(2.0, precision: 0.001);
    }

    [Fact]
    public void HypeScore_MixedPosts_ReturnsSumOfWeightedImpacts()
    {
        // 1 positive(2.0) + 1 negative(2.0) = (2.0*1.5) + (2.0*0.5) = 3.0 + 1.0 = 4.0
        var posts = new[]
        {
            MakePost(impact: 2.0, sentiment: SentimentLabel.Positive),
            MakePost(impact: 2.0, sentiment: SentimentLabel.Negative),
        };
        var score = HypeScoreCalculator.Calculate(posts);
        score.Should().BeApproximately(4.0, precision: 0.001);
    }

    [Fact]
    public void HypeScore_EmptyPostList_ReturnsZero()
    {
        var score = HypeScoreCalculator.Calculate(Enumerable.Empty<IngestedPost>());
        score.Should().Be(0.0);
    }

    // --- Elasticity rules (FR-002) ---
    // Elasticity is clamped to [0.1, 1.0] and derived from HypeScore rank among peers
    // rank 1 in 100 peers → elasticity = 1.0
    // rank 100 in 100 peers → elasticity = 0.1

    [Fact]
    public void Elasticity_HighestRankedNode_ReturnsOnePointZero()
    {
        var elasticity = ElasticityCalculator.Calculate(rank: 1, totalNodes: 100);
        elasticity.Should().BeApproximately(1.0, precision: 0.01);
    }

    [Fact]
    public void Elasticity_LowestRankedNode_ReturnsMinimumClamp()
    {
        var elasticity = ElasticityCalculator.Calculate(rank: 100, totalNodes: 100);
        elasticity.Should().BeGreaterThanOrEqualTo(0.1);
    }

    [Fact]
    public void Elasticity_MiddleRanked_IsBetweenMinAndMax()
    {
        var elasticity = ElasticityCalculator.Calculate(rank: 50, totalNodes: 100);
        elasticity.Should().BeInRange(0.1, 1.0);
    }

    [Fact]
    public void Elasticity_SingleNode_ReturnsMaxElasticity()
    {
        var elasticity = ElasticityCalculator.Calculate(rank: 1, totalNodes: 1);
        elasticity.Should().BeApproximately(1.0, precision: 0.01);
    }

    private static IngestedPost MakePost(double impact, SentimentLabel sentiment) =>
        new()
        {
            PostUri = Guid.NewGuid().ToString(),
            AuthorDid = "did:plc:test",
            Text = "test post",
            MatchedAnchorId = "anchor1",
            MatchedKeyword = "test",
            CreatedAt = DateTimeOffset.UtcNow,
            Sentiment = sentiment,
            ImpactScore = impact,
        };
}
