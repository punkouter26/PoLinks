using PoLinks.Web.Features.Shared.Entities;

namespace PoLinks.Web.Features.Constellation;

/// <summary>
/// BFS-based subgraph resolver for Focus Mode (FR-024).
/// Kept as a public class so unit tests can exercise the logic in isolation.
/// The inlined private implementation in <see cref="ConstellationService"/> is used at runtime.
/// </summary>
public sealed class FocusSubgraphResolver
{
    /// <summary>
    /// Returns the connected subgraph reachable from <paramref name="anchorId"/>
    /// using breadth-first search over undirected edges.
    /// </summary>
    public ConstellationSnapshot ResolveSubgraph(ConstellationSnapshot constellation, string anchorId)
    {
        var anchor = constellation.Nodes.FirstOrDefault(n => n.Id == anchorId && n.Type == NodeType.Anchor);
        if (anchor == null)
            throw new ArgumentException($"Anchor '{anchorId}' not found in constellation", nameof(anchorId));

        var connectedIds = BfsConnectedNodes(constellation, anchorId);

        return new ConstellationSnapshot
        {
            Nodes     = constellation.Nodes.Where(n => connectedIds.Contains(n.Id)).ToList(),
            Links     = constellation.Links.Where(l => connectedIds.Contains(l.SourceId)
                                                    && connectedIds.Contains(l.TargetId)).ToList(),
            CreatedAt = constellation.CreatedAt,
        };
    }

    /// <summary>
    /// Returns the minimum hop-count between <paramref name="anchorId"/> and
    /// <paramref name="targetId"/> or <c>-1</c> if unreachable.
    /// </summary>
    public int GetDistanceFromAnchor(ConstellationSnapshot constellation, string anchorId, string targetId)
    {
        var queue   = new Queue<(string Id, int Depth)>();
        var visited = new HashSet<string>();

        queue.Enqueue((anchorId, 0));
        visited.Add(anchorId);

        var adj = BuildAdjacency(constellation);

        while (queue.Count > 0)
        {
            var (current, depth) = queue.Dequeue();
            if (current == targetId) return depth;

            foreach (var neighbour in adj.GetValueOrDefault(current, []))
            {
                if (visited.Add(neighbour))
                    queue.Enqueue((neighbour, depth + 1));
            }
        }
        return -1;
    }

    /// <summary>
    /// Returns <c>true</c> when focus mode can be exited — i.e. the anchor
    /// is still present in the subgraph's node list.
    /// </summary>
    public bool CanExitFocusMode(FocusMode focusMode) =>
        focusMode.NodeIds.Contains(focusMode.AnchorId);

    // -----------------------------------------------------------------------

    private static HashSet<string> BfsConnectedNodes(ConstellationSnapshot constellation, string startId)
    {
        var adj     = BuildAdjacency(constellation);
        var visited = new HashSet<string> { startId };
        var queue   = new Queue<string>();
        queue.Enqueue(startId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var neighbour in adj.GetValueOrDefault(current, []))
            {
                if (visited.Add(neighbour))
                    queue.Enqueue(neighbour);
            }
        }
        return visited;
    }

    private static Dictionary<string, List<string>> BuildAdjacency(ConstellationSnapshot constellation)
    {
        var adj = new Dictionary<string, List<string>>();
        foreach (var link in constellation.Links)
        {
            adj.TryAdd(link.SourceId, []);
            adj.TryAdd(link.TargetId, []);
            adj[link.SourceId].Add(link.TargetId);
            adj[link.TargetId].Add(link.SourceId);
        }
        return adj;
    }
}
