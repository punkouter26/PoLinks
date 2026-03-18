// T030 (partial): Hype Score formula implementation (FR-002).
// HypeScore = Σ(impactScore × sentimentMultiplier) over posts in the 60-min window.
using PoLinks.Web.Features.Shared.Entities;

namespace PoLinks.Web.Features.Constellation;

/// <summary>Computes the aggregate Hype Score for a node given its contributing posts.</summary>
public static class HypeScoreCalculator
{
    private const double PositiveMultiplier = 1.5;
    private const double NeutralMultiplier  = 1.0;
    private const double NegativeMultiplier = 0.5;

    public static double Calculate(IEnumerable<IngestedPost> posts)
    {
        double total = 0.0;
        foreach (var post in posts)
        {
            total += post.ImpactScore * MultiplierFor(post.Sentiment);
        }
        return total;
    }

    private static double MultiplierFor(SentimentLabel sentiment) => sentiment switch
    {
        SentimentLabel.Positive => PositiveMultiplier,
        SentimentLabel.Negative => NegativeMultiplier,
        _                       => NeutralMultiplier,
    };
}

/// <summary>
/// Maps a node's rank (1 = best) within its anchor's live set to an elasticity value
/// in [0.1, 1.0]. Controls the visual spring-force radius in the D3 physics scene.
/// </summary>
public static class ElasticityCalculator
{
    private const double MaxElasticity = 1.0;
    private const double MinElasticity = 0.1;

    /// <param name="rank">1-based rank (1 = highest HypeScore).</param>
    /// <param name="totalNodes">Total number of live nodes for this anchor.</param>
    public static double Calculate(int rank, int totalNodes)
    {
        if (totalNodes <= 1) return MaxElasticity;
        // Linear interpolation: rank=1 → 1.0, rank=totalNodes → 0.1
        var fraction = (double)(rank - 1) / (totalNodes - 1);
        return Math.Max(MinElasticity, MaxElasticity - fraction * (MaxElasticity - MinElasticity));
    }
}
