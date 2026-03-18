// T026: Bluesky Jetstream WebSocket ingestion hosted worker (FR-004, FR-006).
// Connects to the public Jetstream endpoint, filters posts by keyword, and feeds
// ConstellationService with IngestedPost records. Reconnects with cursor on failure.
//
// Architecture: the WebSocket receive loop posts raw IngestedPost records to a bounded
// Channel<IngestedPost>.  A separate consumer task reads from the channel, calls the
// Azure AI Language sentiment service, adds enriched posts to ConstellationService, and
// persists them to Azure Table Storage — all without blocking the WebSocket pump.
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using PoLinks.Web.Features.Constellation;
using PoLinks.Web.Features.Shared.Entities;
using PoLinks.Web.Infrastructure.TableStorage;

namespace PoLinks.Web.Features.Ingestion;

/// <summary>Background service that streams Bluesky Jetstream and populates constellation state.</summary>
public sealed class BlueskyJetstreamWorker(
    ConstellationService constellationService,
    ISentimentAnalyzer sentimentAnalyzer,
    IPostStorageRepository postStorage,
    IOptions<JetstreamOptions> options,
    ILogger<BlueskyJetstreamWorker> logger)
    : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // Bounded channel: if sentiment analysis falls behind, back-pressure drops the
    // oldest posts rather than letting memory grow unbounded.
    private readonly Channel<IngestedPost> _postChannel =
        Channel.CreateBounded<IngestedPost>(new BoundedChannelOptions(1_000)
        {
            FullMode    = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
        });

    // Max documents per AnalyzeSentimentBatchAsync call (Azure Language API limit).
    private const int SentimentBatchSize = 25;

    // Rolling counter used to implement 1-in-N sampling for sentiment analysis.
    private int _sampleCounter;

    /// <summary>Encode a reconnect cursor as Jetstream microseconds-since-epoch.</summary>
    public static long ToJetstreamCursor(DateTimeOffset ts) =>
        ts.ToUnixTimeMilliseconds() * 1000L;

    /// <summary>Decode a Jetstream cursor back to a DateTimeOffset.</summary>
    public static DateTimeOffset FromJetstreamCursor(long cursor) =>
        DateTimeOffset.FromUnixTimeMilliseconds(cursor / 1000L);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Consumer task: sentiment → constellation → storage
        var consumerTask = ConsumePostsAsync(stoppingToken);

        var cfg    = options.Value;
        long? cursor = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAndConsumeAsync(cfg, cursor, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Jetstream connection lost; reconnecting in 5 s");
                cursor = ToJetstreamCursor(DateTimeOffset.UtcNow.AddSeconds(-5));
                await Task.Delay(5_000, stoppingToken);
            }
        }

        _postChannel.Writer.Complete();
        await consumerTask;
    }

    // -----------------------------------------------------------------------
    // Channel consumer: enriches posts with sentiment and persists to storage.
    // Cost controls applied here:
    //   · Sampling  — only 1-in-SentimentSampleRate posts is sent to the API;
    //                 the rest are stored/displayed with Neutral sentiment.
    //   · Batching  — sampled posts in each drain cycle are sent as a single
    //                 AnalyzeSentimentBatchAsync call (≤25 docs).
    //   · Daily cap — enforced inside LanguageSentimentService.TryConsumeQuota.
    // -----------------------------------------------------------------------
    private async Task ConsumePostsAsync(CancellationToken ct)
    {
        var sampleRate = Math.Max(1, options.Value.SentimentSampleRate);
        var batch      = new List<IngestedPost>(SentimentBatchSize);
        var toAnalyze  = new List<(int batchIndex, string text)>(SentimentBatchSize);

        while (!ct.IsCancellationRequested)
        {
            // Wait for at least one post before draining.
            try { if (!await _postChannel.Reader.WaitToReadAsync(ct)) break; }
            catch (OperationCanceledException) { break; }

            batch.Clear();
            while (batch.Count < SentimentBatchSize && _postChannel.Reader.TryRead(out var item))
                batch.Add(item);

            if (batch.Count == 0) continue;

            // Apply sampling: every sampleRate-th post (globally) goes to the API.
            toAnalyze.Clear();
            for (int i = 0; i < batch.Count; i++)
            {
                if (Interlocked.Increment(ref _sampleCounter) % sampleRate == 0)
                    toAnalyze.Add((i, batch[i].Text));
            }

            // One batched API call for the sampled subset.
            IReadOnlyList<SentimentLabel> labels = [];
            if (toAnalyze.Count > 0)
            {
                var texts = toAnalyze.Select(t => t.text).ToList();
                try { labels = await sentimentAnalyzer.AnalyzeBatchAsync(texts, ct); }
                catch (OperationCanceledException) { break; }
            }

            // Enrich every post and forward: sampled posts get real sentiment, rest get Neutral.
            for (int i = 0; i < batch.Count; i++)
            {
                var sentiment  = SentimentLabel.Neutral;
                var analyzeIdx = toAnalyze.FindIndex(t => t.batchIndex == i);
                if (analyzeIdx >= 0 && analyzeIdx < labels.Count)
                    sentiment = labels[analyzeIdx];

                var enriched = batch[i] with { Sentiment = sentiment };
                constellationService.AddPost(enriched);
                _ = postStorage.WriteAsync(enriched, ct);
            }
        }
    }

    // -----------------------------------------------------------------------
    // WebSocket pump
    // -----------------------------------------------------------------------
    private async Task ConnectAndConsumeAsync(
        JetstreamOptions cfg, long? cursor, CancellationToken ct)
    {
        using var ws = new ClientWebSocket();
        var uriBuilder = new UriBuilder(cfg.Endpoint);

        var qs = "wanted_collections=app.bsky.feed.post";
        if (cursor.HasValue) qs += $"&cursor={cursor.Value}";
        uriBuilder.Query = qs;

        logger.LogInformation("Connecting to Jetstream: {Uri}", uriBuilder.Uri);
        await ws.ConnectAsync(uriBuilder.Uri, ct);
        logger.LogInformation("Jetstream connected");

        var buffer = new byte[16 * 1024];

        while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            var result = await ws.ReceiveAsync(buffer, ct);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, ct);
                break;
            }

            if (result.MessageType != WebSocketMessageType.Text) continue;

            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            ParseAndQueue(json, cfg.Anchors);
        }
    }

    private void ParseAndQueue(string json, IReadOnlyList<AnchorConfig> anchors)
    {
        JetstreamEvent? evt;
        try
        {
            evt = JsonSerializer.Deserialize<JetstreamEvent>(json, JsonOpts);
        }
        catch (JsonException ex)
        {
            logger.LogDebug(ex, "Skipping malformed Jetstream frame");
            return;
        }

        if (evt?.Kind != "commit") return;
        if (evt.Commit?.Collection != "app.bsky.feed.post") return;
        if (evt.Commit.Operation != "create") return;

        var text = evt.Commit.Record?.Text;
        if (string.IsNullOrWhiteSpace(text)) return;

        foreach (var anchor in anchors)
        {
            var matched = anchor.Keywords.FirstOrDefault(
                kw => text.Contains(kw, StringComparison.OrdinalIgnoreCase));
            if (matched is null) continue;

            var post = new IngestedPost
            {
                PostUri         = $"at://{evt.Did}/app.bsky.feed.post/{evt.Commit.Rkey}",
                AuthorDid       = evt.Did,
                Text            = text,
                MatchedAnchorId = anchor.AnchorId,
                MatchedKeyword  = matched,
                CreatedAt       = FromJetstreamCursor(evt.TimeUs),
                Sentiment       = SentimentLabel.Neutral, // enriched asynchronously in consumer
                ImpactScore     = 1.0,
            };

            // TryWrite is non-blocking; DropOldest mode handles a full channel
            if (!_postChannel.Writer.TryWrite(post))
                logger.LogDebug("Post channel full; post dropped for anchor {AnchorId}", anchor.AnchorId);
        }
    }
}

/// <summary>Configuration bound from appsettings: Jetstream endpoint, anchor keyword lists, and sentiment sampling.</summary>
public sealed class JetstreamOptions
{
    public const string Section = "Jetstream";
    public string Endpoint { get; set; } = "wss://jetstream2.us-east.bsky.network/subscribe";

    /// <summary>
    /// Sentiment sampling rate: only 1-in-N matching posts is sent to the Azure Language API.
    /// The rest are stored with <see cref="SentimentLabel.Neutral"/>.
    /// Default 10 reduces API calls by 90 % before the daily cap kicks in.
    /// </summary>
    public int SentimentSampleRate { get; set; } = 10;

    public IReadOnlyList<AnchorConfig> Anchors { get; set; } =
    [
        new AnchorConfig { AnchorId = "robotics",  Keywords = ["robotics","robot","ROS2","servo"] },
        new AnchorConfig { AnchorId = "dronetech", Keywords = ["drone","UAV","quadcopter"] },
        new AnchorConfig { AnchorId = "ai",        Keywords = ["AI","machine learning","LLM"] },
        new AnchorConfig { AnchorId = "autonomy",  Keywords = ["autonomous","self-driving","SLAM"] },
        new AnchorConfig { AnchorId = "sensors",   Keywords = ["lidar","camera sensor","IMU"] },
    ];
}
