// T032: Pulse scheduler — fires every 30 seconds, assembles PulseBatch, broadcasts via SignalR (FR-001, FR-009, FR-011).
using Microsoft.AspNetCore.SignalR;
using PoLinks.Web.Features.Constellation;
using PoLinks.Web.Features.Shared.Entities;
using PoLinks.Web.Features.Simulation;

namespace PoLinks.Web.Features.Pulse;

/// <summary>Background service that pulses a <see cref="PulseBatch"/> to all hub clients every 30 s.</summary>
public sealed class PulseService(
    ConstellationService constellationService,
    IMockDataService mockDataService,
    IHubContext<PulseHub> hubContext,
    ILogger<PulseService> logger)
    : BackgroundService
{
    public static readonly TimeSpan PulseInterval = TimeSpan.FromSeconds(30);

    private DateTimeOffset _lastPulse = DateTimeOffset.UtcNow - PulseInterval;

    /// <summary>Remaining time until the next pulse fires (may be negative if overdue).</summary>
    public static TimeSpan GetTimeToNextPulse(DateTimeOffset lastPulse, DateTimeOffset now) =>
        PulseInterval - (now - lastPulse);

    /// <summary>Returns true when a pulse is due.</summary>
    public static bool IsPulseDue(DateTimeOffset lastPulse, DateTimeOffset now) =>
        now - lastPulse >= PulseInterval;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

            var now = DateTimeOffset.UtcNow;
            if (!IsPulseDue(_lastPulse, now)) continue;

            try
            {
                await FirePulseAsync(stoppingToken);
                _lastPulse = now;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Pulse fire failed");
            }
        }
    }

    private async Task FirePulseAsync(CancellationToken ct)
    {
        // In simulation mode (no live posts), use mock data
        var isSimulated = constellationService.IsEmpty;
        IReadOnlyList<PulseBatch> batches;

        if (isSimulated)
        {
            batches = mockDataService.GenerateAllAnchorBatches();
        }
        else
        {
            var snapshot = constellationService.BuildSnapshot();
            var anchorIds = snapshot.Nodes
                .Where(n => n.Type == PoLinks.Web.Features.Shared.Entities.NodeType.Anchor)
                .Select(n => n.Id)
                .Distinct();

            batches = anchorIds.Select(anchorId => new PulseBatch
            {
                AnchorId    = anchorId,
                GeneratedAt = snapshot.CreatedAt,
                Nodes       = snapshot.Nodes.Where(n => n.AnchorId == anchorId).ToList(),
                Links       = snapshot.Links.Where(l =>
                    snapshot.Nodes.Any(n => n.Id == l.SourceId && n.AnchorId == anchorId)).ToList(),
                IsSimulated = false,
            }).ToList();
        }

        foreach (var batch in batches)
        {
            await hubContext.Clients.All.SendAsync("ReceivePulseBatch", batch, ct);
        }

        logger.LogDebug("Pulse fired: {BatchCount} batches (simulated={IsSimulated})",
            batches.Count, isSimulated);
    }
}
