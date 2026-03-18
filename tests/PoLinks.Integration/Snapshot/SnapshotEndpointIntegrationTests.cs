using System.Net;
using System.Net.Http.Json;
using PoLinks.Integration.Fixtures;
using PoLinks.Web.Features.Snapshot;

namespace PoLinks.Integration.Snapshot;

[Collection(IntegrationCollection.Name)]
public sealed class SnapshotEndpointIntegrationTests(PoLinksWebAppFactory factory)
{
    [Fact]
    public async Task GetExportMetadata_DefaultQuery_ReturnsPngPayload()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/snapshot/export-metadata");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SnapshotExportMetadataDto>();
        body.Should().NotBeNull();
        body!.Format.Should().Be("png");
        body.ContentType.Should().Be("image/png");
        body.Scale.Should().Be(2);
        body.FileName.Should().EndWith(".png");
    }

    [Fact]
    public async Task GetExportMetadata_SvgQuery_ReturnsSvgPayload()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/snapshot/export-metadata?format=svg&scale=3");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SnapshotExportMetadataDto>();
        body.Should().NotBeNull();
        body!.Format.Should().Be("svg");
        body.ContentType.Should().Be("image/svg+xml");
        body.Scale.Should().Be(3);
        body.FileName.Should().EndWith(".svg");
    }

    [Fact]
    public async Task GetExportMetadata_UnsupportedFormat_Returns400()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/snapshot/export-metadata?format=jpg");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public async Task GetExportMetadata_InvalidScale_Returns400(int scale)
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/snapshot/export-metadata?format=png&scale={scale}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetExportMetadata_FileNameContainsTimestampedPrefix()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/snapshot/export-metadata?format=png");
        var body = await response.Content.ReadFromJsonAsync<SnapshotExportMetadataDto>();

        body.Should().NotBeNull();
        body!.FileName.Should().MatchRegex("^polinks-constellation-[0-9]{8}-[0-9]{6}\\.png$");
    }
}
