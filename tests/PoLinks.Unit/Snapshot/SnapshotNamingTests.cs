using PoLinks.Web.Features.Snapshot;

namespace PoLinks.Unit.Snapshot;

public sealed class SnapshotNamingTests
{
    [Fact]
    public void BuildFileName_UsesTimestampedCanonicalPattern()
    {
        var timestamp = new DateTimeOffset(2026, 03, 17, 14, 23, 45, TimeSpan.Zero);

        var fileName = SnapshotExportMetadata.BuildFileName(timestamp, "png");

        fileName.Should().Be("polinks-constellation-20260317-142345.png");
    }

    [Theory]
    [InlineData("png", "image/png")]
    [InlineData("svg", "image/svg+xml")]
    public void ContentTypeFor_ReturnsExpectedMimeType(string format, string expected)
    {
        SnapshotExportMetadata.ContentTypeFor(format).Should().Be(expected);
    }

    [Theory]
    [InlineData("PNG", "png")]
    [InlineData("svg", "svg")]
    [InlineData("jpeg", "png")]
    [InlineData(null, "png")]
    public void NormalizeFormat_FallsBackToPngWhenUnsupported(string? input, string expected)
    {
        SnapshotExportMetadata.NormalizeFormat(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("png", true)]
    [InlineData("svg", true)]
    [InlineData("jpg", false)]
    [InlineData("", false)]
    public void IsSupportedFormat_RecognizesAllowedFormats(string? input, bool expected)
    {
        SnapshotExportMetadata.IsSupportedFormat(input).Should().Be(expected);
    }
}
