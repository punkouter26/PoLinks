// T031: 100-node hard-cap eviction policy (FR-003).
// Anchor nodes are protected; Topic nodes with the lowest HypeScore are evicted first.
using PoLinks.Web.Features.Shared.Entities;

namespace PoLinks.Web.Features.Constellation;

/// <summary>Enforces the per-anchor node cap, evicting by lowest HypeScore (Topics only).</summary>
public static class NodeCapEnforcer
{
    /// <summary>
    /// Returns at most <paramref name="cap"/> nodes, always preserving all Anchor nodes.
    /// Topic nodes beyond the cap are evicted in ascending HypeScore order.
    /// </summary>
    public static IReadOnlyList<NexusNode> Enforce(IList<NexusNode> nodes, int cap)
    {
        if (nodes.Count <= cap) return nodes.ToArray();

        var anchors = nodes.Where(n => n.Type == NodeType.Anchor).ToList();
        var topics  = nodes.Where(n => n.Type == NodeType.Topic)
                           .OrderByDescending(n => n.HypeScore)
                           .ToList();

        var topicSlots = Math.Max(0, cap - anchors.Count);
        return [.. anchors, .. topics.Take(topicSlots)];
    }
}
