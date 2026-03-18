// T018: Simulation Mode state contract.
// Used by the frontend SignalR context to decide between live and mock data.
namespace PoLinks.Web.Features.Shared.Entities;

/// <summary>
/// Describes the current simulation mode status propagated to connected clients.
/// The React client must show a visible banner when IsSimulated == true (FR-029).
/// </summary>
public sealed record SimulationModeState
{
    public required bool IsSimulated { get; init; }
    public string? Reason { get; init; }
    public DateTimeOffset? SinceUtc { get; init; }
}
