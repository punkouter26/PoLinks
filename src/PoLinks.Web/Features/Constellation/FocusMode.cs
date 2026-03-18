namespace PoLinks.Web.Features.Constellation;

/// <summary>Immutable snapshot of an active Focus-Mode session (FR-024).</summary>
/// <param name="AnchorId">The node ID of the selected anchor.</param>
/// <param name="NodeIds">All node IDs reachable within the focus subgraph.</param>
/// <param name="EnteredAt">When focus mode was entered (UTC).</param>
public sealed record FocusMode(
    string              AnchorId,
    IEnumerable<string> NodeIds,
    DateTimeOffset      EnteredAt);
