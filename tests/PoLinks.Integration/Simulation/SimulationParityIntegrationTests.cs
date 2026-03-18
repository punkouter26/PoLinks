// T095: Integration tests for Simulation Mode PulseBatch contract parity (FR-030).
// A simulated PulseBatch must have the same JSON shape as a live one.
using Microsoft.Extensions.DependencyInjection;
using PoLinks.Integration.Fixtures;
using PoLinks.Web.Features.Shared.Entities;
using PoLinks.Web.Features.Simulation;

namespace PoLinks.Integration.Simulation;

[Collection("Integration")]
public sealed class SimulationParityIntegrationTests(PoLinksWebAppFactory factory)
{
    [Fact]
    public void SimulatedBatch_HasRequiredContractFields()
    {
        var service = factory.Services.GetRequiredService<IMockDataService>();
        var batch = service.GeneratePulseBatch(anchorId: "did:plc:test");

        batch.Should().NotBeNull();
        batch.AnchorId.Should().NotBeNullOrWhiteSpace();
        batch.Nodes.Should().NotBeEmpty();
        batch.Links.Should().NotBeNull();
        batch.GeneratedAt.Should().BeAfter(DateTimeOffset.UtcNow.AddMinutes(-1));
        batch.IsSimulated.Should().BeTrue("simulation service must tag its own batches");
    }

    [Fact]
    public void SimulatedBatch_NodesHaveRequiredProperties()
    {
        var service = factory.Services.GetRequiredService<IMockDataService>();
        var batch = service.GeneratePulseBatch(anchorId: "did:plc:test");

        foreach (var node in batch.Nodes)
        {
            node.Id.Should().NotBeNullOrWhiteSpace();
            node.Label.Should().NotBeNullOrWhiteSpace();
            node.HypeScore.Should().BeGreaterThanOrEqualTo(0.0);
            node.Elasticity.Should().BeInRange(0.1, 1.0);
        }
    }
}
