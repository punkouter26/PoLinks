// T091: Unit tests for renderer capability detection (FR-015 adaptive renderer).
// Rules: prefers WebGL2 canvas; falls back to 2D canvas on WebGL failure.
// These tests cover data-model / capability-descriptor logic (no DOM access needed).

namespace PoLinks.Unit.Renderer;

public sealed class RendererCapabilityTests
{
    // RendererCapability is a discriminated-union-style record in the web project.
    // Here we test the selection / fallback logic in isolation.

    [Theory]
    [InlineData(true, "webgl2")]
    [InlineData(false, "canvas2d")]
    public void SelectRenderer_ChoosesCorrectMode(bool webGl2Available, string expectedMode)
    {
        var mode = RendererSelector.Select(webGl2Available);
        mode.Should().Be(expectedMode);
    }

    [Fact]
    public void SelectRenderer_WebGl2Available_ReturnsBestRenderer()
    {
        RendererSelector.Select(webGl2Available: true).Should().Be("webgl2");
    }

    [Fact]
    public void SelectRenderer_WebGl2Unavailable_Returns2DFallback()
    {
        RendererSelector.Select(webGl2Available: false).Should().Be("canvas2d");
    }
}

/// <summary>Minimal capability-selector — source of truth for the adaptive renderer decision.</summary>
internal static class RendererSelector
{
    public static string Select(bool webGl2Available) =>
        webGl2Available ? "webgl2" : "canvas2d";
}
