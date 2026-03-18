// T033: SignalR Pulse hub endpoint (FR-001, FR-009).
// Clients connect and receive PulseBatch messages pushed by PulseService.
using Microsoft.AspNetCore.SignalR;
using PoLinks.Web.Features.Constellation;
using PoLinks.Web.Features.Shared.Entities;
using PoLinks.Web.Features.Simulation;

namespace PoLinks.Web.Features.Pulse;

/// <summary>
/// SignalR hub through which PulseBatch frames are broadcast to all connected clients.
/// Clients subscribe by connecting; no inbound messages are currently handled.
/// </summary>
public sealed class PulseHub(
    ConstellationService constellationService,
    IMockDataService mockDataService,
    ILogger<PulseHub> logger) : Hub
{
    // Server-to-client only at MVP; client-to-server methods can be added for future US4 focus mode.

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();

        await Task.Delay(1500);

        var batches = GetInitialBatches();
        logger.LogInformation("OnConnectedAsync: sending {BatchCount} batches to {ConnectionId}",
            batches.Count, Context.ConnectionId);

        foreach (var batch in batches)
        {
            logger.LogDebug("Sending batch anchorId={AnchorId} nodes={NodeCount}",
                batch.AnchorId, batch.Nodes.Count);
            await Clients.Caller.SendAsync("ReceivePulseBatch", batch);
        }
    }

    public Task<PulseBatch?> GetCurrentPulseBatch()
    {
        var batch = GetInitialBatches().FirstOrDefault();
        return Task.FromResult(batch);
    }

    private IReadOnlyList<PulseBatch> GetInitialBatches()
    {
        logger.LogInformation("GetInitialBatches: IsEmpty={IsEmpty} PostCount={PostBuckets}",
            constellationService.IsEmpty, constellationService.PostBucketCount);
        if (constellationService.IsEmpty)
        {
            var mocks = mockDataService.GenerateAllAnchorBatches();
            logger.LogInformation("GetInitialBatches: using mock data, count={MockCount}", mocks.Count);
            return mocks;
        }

        var snapshot = constellationService.BuildSnapshot();
        var anchorIds = snapshot.Nodes
            .Where(node => node.Type == NodeType.Anchor)
            .Select(node => node.Id)
            .Distinct();

        return anchorIds.Select(anchorId => new PulseBatch
        {
            AnchorId = anchorId,
            GeneratedAt = snapshot.CreatedAt,
            Nodes = snapshot.Nodes.Where(node => node.AnchorId == anchorId).ToList(),
            Links = snapshot.Links.Where(link =>
                snapshot.Nodes.Any(node => node.Id == link.SourceId && node.AnchorId == anchorId)).ToList(),
            IsSimulated = false,
        }).ToList();
    }
}
