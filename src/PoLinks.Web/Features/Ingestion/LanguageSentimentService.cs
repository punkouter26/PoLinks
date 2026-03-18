// Azure AI Language (Text Analytics) sentiment analysis service.
// Config keys (loaded from Key Vault via AzureAI--Language--* secrets):
//   AzureAI:Language:Endpoint        – e.g. https://eastus.api.cognitive.microsoft.com/
//   AzureAI:Language:ApiKey          – Key 1 from language-poshared-eastus
//   AzureAI:Language:DailyApiCallCap – hard daily cap on text records (default 3000 ≈ $0.90/day)
using Azure;
using Azure.AI.TextAnalytics;
using PoLinks.Web.Features.Shared.Entities;

namespace PoLinks.Web.Features.Ingestion;

/// <summary>Analyzes sentiment of text and returns a <see cref="SentimentLabel"/>.</summary>
public interface ISentimentAnalyzer
{
    Task<SentimentLabel> AnalyzeAsync(string text, CancellationToken ct = default);

    /// <summary>
    /// Analyze up to 25 texts in a single API call (same cost per record, far fewer round-trips).
    /// Returns one label per input in the same order; per-document failures return Neutral.
    /// </summary>
    Task<IReadOnlyList<SentimentLabel>> AnalyzeBatchAsync(
        IReadOnlyList<string> texts, CancellationToken ct = default);
}

/// <summary>
/// Azure AI Language implementation of <see cref="ISentimentAnalyzer"/>.
/// Uses <see cref="TextAnalyticsClient.AnalyzeSentimentBatchAsync"/> (≤25 docs per call).
/// <para>Cost guardrails:
/// <list type="bullet">
///   <item>Hard daily cap (default 3,000 text records ≈ $0.90/day at $3/10 k) resets at UTC midnight.</item>
///   <item>Posts beyond the cap receive <see cref="SentimentLabel.Neutral"/> without an API call.</item>
/// </list></para>
/// Falls back to <see cref="SentimentLabel.Neutral"/> on any API error so the ingestion
/// pipeline is never blocked by a transient Language service failure.
/// </summary>
public sealed class LanguageSentimentService : ISentimentAnalyzer
{
    private readonly TextAnalyticsClient _client;
    private readonly ILogger<LanguageSentimentService> _logger;
    private readonly int _dailyCap;

    // Daily-cap tracking — all access under _capLock
    private readonly object _capLock = new();
    private int _dailyCallCount;
    private DateTime _dailyWindowStart = DateTime.UtcNow.Date;

    public LanguageSentimentService(IConfiguration configuration, ILogger<LanguageSentimentService> logger)
    {
        _logger = logger;

        var endpoint = configuration["AzureAI:Language:Endpoint"]
            ?? throw new InvalidOperationException("AzureAI:Language:Endpoint is required.");
        var apiKey = configuration["AzureAI:Language:ApiKey"]
            ?? throw new InvalidOperationException("AzureAI:Language:ApiKey is required.");
        _dailyCap = configuration.GetValue<int>("AzureAI:Language:DailyApiCallCap", 3_000);

        _logger.LogInformation("Sentiment daily API cap set to {Cap} text records/day", _dailyCap);
        _client = new TextAnalyticsClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
    }

    /// <inheritdoc/>
    public async Task<SentimentLabel> AnalyzeAsync(string text, CancellationToken ct = default)
    {
        var labels = await AnalyzeBatchAsync([text], ct);
        return labels[0];
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SentimentLabel>> AnalyzeBatchAsync(
        IReadOnlyList<string> texts, CancellationToken ct = default)
    {
        if (texts.Count == 0) return [];

        var neutral = Enumerable.Repeat(SentimentLabel.Neutral, texts.Count).ToArray();

        if (!TryConsumeQuota(texts.Count))
        {
            _logger.LogDebug(
                "Sentiment daily cap ({Cap}) reached; returning Neutral for {Count} record(s)",
                _dailyCap, texts.Count);
            return neutral;
        }

        try
        {
            var docs = texts.Select((t, i) => new TextDocumentInput(i.ToString(), t));
            var response = await _client.AnalyzeSentimentBatchAsync(docs, cancellationToken: ct);

            var result = (SentimentLabel[])neutral.Clone();
            foreach (var docResult in response.Value)
            {
                if (docResult.HasError || !int.TryParse(docResult.Id, out var idx)) continue;
                result[idx] = docResult.DocumentSentiment.Sentiment switch
                {
                    TextSentiment.Positive => SentimentLabel.Positive,
                    TextSentiment.Negative => SentimentLabel.Negative,
                    _                      => SentimentLabel.Neutral,
                };
            }
            return result;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Batch sentiment analysis failed for {Count} record(s); defaulting to Neutral",
                texts.Count);
            return neutral;
        }
    }

    // Returns true and charges `count` records against today's daily quota.
    // Resets automatically when the UTC calendar day rolls over.
    private bool TryConsumeQuota(int count)
    {
        lock (_capLock)
        {
            var today = DateTime.UtcNow.Date;
            if (today != _dailyWindowStart)
            {
                _logger.LogInformation(
                    "Sentiment daily counter reset ({Used}/{Cap} records used yesterday)",
                    _dailyCallCount, _dailyCap);
                _dailyCallCount = 0;
                _dailyWindowStart = today;
            }

            if (_dailyCallCount + count > _dailyCap)
                return false;

            _dailyCallCount += count;
            return true;
        }
    }
}
