// S3: Extracted from SemanticRootsResolver.cs — single-responsibility class.
// Maps a SentimentLabel to its spec-mandated CSS hex colour token.
// Positive → Electric Blue (#00BFFF), Negative → Deep Crimson (#DC143C), Neutral → grey.
using PoLinks.Web.Features.Shared.Entities;

namespace PoLinks.Web.Features.Constellation;

public static class InsightSentimentColour
{
    public const string Positive = "#00BFFF";
    public const string Negative = "#DC143C";
    public const string Neutral  = "#9CA3AF";

    public static string For(SentimentLabel label) => label switch
    {
        SentimentLabel.Positive => Positive,
        SentimentLabel.Negative => Negative,
        _                       => Neutral,
    };
}
