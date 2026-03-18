// T043: Semantic roots resolver — builds the breadcrumb ancestry chain for a node (US2, AC-2).
// Current hierarchy is flat (Anchor → Topic), so the path is always [anchorId, nodeLabel].
// Extending this for deeper hierarchies only requires changing this service.
namespace PoLinks.Web.Features.Constellation;

/// <summary>Resolves the breadcrumb path from a node back to its anchor root.</summary>
public static class SemanticRootsResolver
{
    /// <summary>
    /// Returns the ancestry chain for the given node.
    /// For the current flat Anchor→Topic hierarchy this is [anchorId, nodeLabel].
    /// When <paramref name="anchorId"/> equals <paramref name="nodeLabel"/> (Anchor clicked)
    /// the chain collapses to a single element.
    /// </summary>
    public static IReadOnlyList<string> Resolve(string anchorId, string nodeLabel)
    {
        if (string.Equals(anchorId, nodeLabel, StringComparison.OrdinalIgnoreCase))
            return [anchorId];

        return [anchorId, nodeLabel];
    }
}
