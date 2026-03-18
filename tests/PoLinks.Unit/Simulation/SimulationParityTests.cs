// T094: Unit tests for deterministic simulation/live parity (FR-013).
// In Simulation Mode the same anchor/topic map must be produced by the mock data service.
// "Deterministic" means: same seed → identical output; different seed → different output.

using PoLinks.Web.Features.Shared.Entities;
using PoLinks.Web.Features.Simulation;

namespace PoLinks.Unit.Simulation;

public sealed class SimulationParityTests
{
    [Fact]
    public void MockDataService_SameSeed_ProducesSameNodes()
    {
        var a = MockDataService.GenerateNodes(seed: 42, anchorId: "test-anchor");
        var b = MockDataService.GenerateNodes(seed: 42, anchorId: "test-anchor");

        a.Select(n => n.Id).Should().Equal(b.Select(n => n.Id));
    }

    [Fact]
    public void MockDataService_DifferentSeed_ProducesDifferentNodes()
    {
        var a = MockDataService.GenerateNodes(seed: 1, anchorId: "test-anchor");
        var b = MockDataService.GenerateNodes(seed: 2, anchorId: "test-anchor");

        a.Select(n => n.Id).Should().NotEqual(b.Select(n => n.Id));
    }

    [Fact]
    public void MockDataService_AlwaysProducesAnchorNode()
    {
        var nodes = MockDataService.GenerateNodes(seed: 99, anchorId: "my-anchor");
        nodes.Should().Contain(n => n.Type == NodeType.Anchor && n.Id == "my-anchor");
    }

    [Fact]
    public void MockDataService_NeverExceedsCap()
    {
        var nodes = MockDataService.GenerateNodes(seed: 7, anchorId: "anchor", count: 150);
        nodes.Should().HaveCountLessOrEqualTo(100);
    }
}
