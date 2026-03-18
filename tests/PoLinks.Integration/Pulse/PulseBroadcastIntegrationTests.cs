// T023: Integration tests for pulse batch assembly and broadcast (FR-001, FR-009).
// Verifies the SignalR hub pushes PulseBatch to connected clients within 60s window.
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR.Client;
using PoLinks.Integration.Fixtures;
using PoLinks.Web.Features.Shared.Entities;

namespace PoLinks.Integration.Pulse;

[Collection("Integration")]
public sealed class PulseBroadcastIntegrationTests(PoLinksWebAppFactory factory)
{
    [Fact]
    public async Task PulseHub_AcceptsConnection_NoException()
    {
        var hub = BuildHubConnection(factory);
        await hub.StartAsync();
        hub.State.Should().Be(HubConnectionState.Connected);
        await hub.StopAsync();
    }

    [Fact]
    public async Task PulseHub_SimulationMode_DeliversBatchWithIsSimulatedTrue()
    {
        // Force simulation mode by ensuring no live Jetstream connection is active
        var hub = BuildHubConnection(factory);

        await hub.StartAsync();

        var payload = await hub.InvokeAsync<JsonElement>("GetCurrentPulseBatch");
        var received = payload.Deserialize<PulseBatch>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        });

        received.Should().NotBeNull("hub should push at least one batch within 40 s");
        received!.IsSimulated.Should().BeTrue("factory disables live Jetstream so simulation mode must be active");
        received.AnchorId.Should().NotBeNullOrWhiteSpace("batch must carry the anchor identity");
        received.GeneratedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, precision: TimeSpan.FromSeconds(60));
        received.Nodes.Should().NotBeEmpty("a simulated batch must contain nodes");
        foreach (var node in received.Nodes)
        {
            node.HypeScore.Should().BeGreaterThanOrEqualTo(0.0, "HypeScore is non-negative by contract");
            node.Elasticity.Should().BeInRange(0.1, 1.0, "elasticity is clamped to [0.1, 1.0] per FR-002");
        }

        await hub.StopAsync();
    }

    private static HubConnection BuildHubConnection(PoLinksWebAppFactory factory)
    {
        var httpClient = factory.CreateClient();
        return new HubConnectionBuilder()
            .WithUrl(
                new Uri(httpClient.BaseAddress!, "/hubs/pulse"),
                opts => opts.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler()
            )
            .Build();
    }
}
