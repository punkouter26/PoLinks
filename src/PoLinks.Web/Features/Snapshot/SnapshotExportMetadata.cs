namespace PoLinks.Web.Features.Snapshot;

public static class SnapshotExportMetadata
{
    private static readonly HashSet<string> SupportedFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        "png",
        "svg",
    };

    public static bool IsSupportedFormat(string? format) =>
        !string.IsNullOrWhiteSpace(format) && SupportedFormats.Contains(format);

    public static string NormalizeFormat(string? format) =>
        IsSupportedFormat(format) ? format!.ToLowerInvariant() : "png";

    public static string ContentTypeFor(string format) => format switch
    {
        "svg" => "image/svg+xml",
        _ => "image/png",
    };

    public static string BuildFileName(DateTimeOffset timestamp, string format)
    {
        var normalizedFormat = NormalizeFormat(format);
        return $"polinks-constellation-{timestamp:yyyyMMdd-HHmmss}.{normalizedFormat}";
    }
}

public sealed record SnapshotExportMetadataDto(
    string FileName,
    string ContentType,
    string Format,
    int Scale,
    DateTimeOffset GeneratedAtUtc);
