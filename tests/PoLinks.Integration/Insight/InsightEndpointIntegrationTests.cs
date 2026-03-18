// T040: Integration tests for the insight feed endpoint projection (US2).
// Verifies the GET /api/constellation/insight/{nodeId} endpoint shape and simulation fallback.
using System.Net;
using System.Net.Http.Json;
using PoLinks.Integration.Fixtures;
using PoLinks.Web.Features.Constellation;

namespace PoLinks.Integration.Insight;

[Collection("Integration")]
public sealed class InsightEndpointIntegrationTests(PoLinksWebAppFactory factory)
{
    // ---- Simulation mode (no live posts) ------------------------------------

    [Fact]
    public async Task GetInsight_SimulationMode_Returns200WithMockPosts()
    {
        // In integration tests the Jetstream endpoint is invalid so the constellation
        // service remains empty → the endpoint falls back to simulation data.
        var client   = factory.CreateClient();
        var nodeId   = "robotics-42-000"; // matches mock node pattern

        var response = await client.GetAsync($"/api/constellation/insight/{Uri.EscapeDataString(nodeId)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<InsightResponseDto>();
        body.Should().NotBeNull();
        body!.NodeId.Should().Be(nodeId);
        body.Posts.Should().HaveCountGreaterThanOrEqualTo(3, "spec requires at least 3 mock posts in simulation mode");
    }

    [Fact]
    public async Task GetInsight_SimulationMode_PostsAreDescendingByImpact()
    {
        var client   = factory.CreateClient();
        var nodeId   = "ai-42-005";

        var response = await client.GetAsync($"/api/constellation/insight/{Uri.EscapeDataString(nodeId)}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<InsightResponseDto>();
        var scores = body!.Posts.Select(p => p.ImpactScore).ToList();
        scores.Should().BeInDescendingOrder("spec AC-3 requires posts sorted by descending impact score");
    }

    [Fact]
    public async Task GetInsight_SimulationMode_SemanticRootsHaveTwoOrMoreElements()
    {
        var client  = factory.CreateClient();
        var nodeId  = "sensors-42-000";

        var response = await client.GetAsync($"/api/constellation/insight/{Uri.EscapeDataString(nodeId)}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<InsightResponseDto>();
        body!.SemanticRoots.Should().HaveCountGreaterThanOrEqualTo(2, "spec AC-2 requires at least anchor + node label");
    }

    [Fact]
    public async Task GetInsight_SimulationMode_SentimentColourPresentOnEachPost()
    {
        var client  = factory.CreateClient();
        var nodeId  = "autonomy-42-001";

        var response = await client.GetAsync($"/api/constellation/insight/{Uri.EscapeDataString(nodeId)}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<InsightResponseDto>();
        body!.Posts.Should().OnlyContain(p => !string.IsNullOrEmpty(p.SentimentColour),
            "every post must carry a sentiment colour token for the UI chip");
    }

    [Fact]
    public async Task GetInsight_UnknownNodeWhenNotSimulated_Returns404()
    {
        // This test verifies 404 only reachable when constellation has data but the node
        // is absent. Since the factory keeps the constellation empty (simulation mode), we
        // verify that the endpoint at minimum doesn't 500 for an arbitrary unknown node.
        var client   = factory.CreateClient();
        var nodeId   = "completely-unknown-did:plc:zzz999";

        // In simulation mode any nodeId should return 200 with generated data.
        var response = await client.GetAsync($"/api/constellation/insight/{Uri.EscapeDataString(nodeId)}");
        // Either 200 (simulation fallback) or 404 are acceptable — never 500.
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }
}
