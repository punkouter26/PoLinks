using Microsoft.AspNetCore.Mvc;

namespace PoLinks.Web.Features.Snapshot;

public static class SnapshotEndpoints
{
    public static IEndpointRouteBuilder MapSnapshotEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/snapshot/export-metadata", GetExportMetadata)
            .WithName("GetSnapshotExportMetadata")
            .WithTags("Snapshot");

        return app;
    }

    private static IResult GetExportMetadata([AsParameters] SnapshotExportQuery query)
    {
        var format = SnapshotExportMetadata.NormalizeFormat(query.Format);
        if (!SnapshotExportMetadata.IsSupportedFormat(query.Format))
        {
            return TypedResults.BadRequest(new { error = "format must be one of: png, svg" });
        }

        if (query.Scale is < 1 or > 4)
        {
            return TypedResults.BadRequest(new { error = "scale must be between 1 and 4" });
        }

        var generatedAt = DateTimeOffset.UtcNow;
        return TypedResults.Ok(new SnapshotExportMetadataDto(
            SnapshotExportMetadata.BuildFileName(generatedAt, format),
            SnapshotExportMetadata.ContentTypeFor(format),
            format,
            query.Scale,
            generatedAt));
    }
}

public sealed record SnapshotExportQuery(
    [FromQuery(Name = "format")] string Format = "png",
    [FromQuery(Name = "scale")] int Scale = 2);
