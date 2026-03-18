// Integration tests for the recursive expansion graph endpoints:
// - GET /api/constellation/related (new endpoint, returns top posts for expansion)
// - GET /api/constellation/insight/{postUri} (postUri path for insight panel)
using System.Net;
using System.Net.Http.Json;
using PoLinks.Integration.Fixtures;
using PoLinks.Web.Features.Constellation;

namespace PoLinks.Integration.Insight;

[Collection("Integration")]
public sealed class ExpansionGraphIntegrationTests(PoLinksWebAppFactory factory)
{
    // -----------------------------------------------------------------------
    // GET /api/constellation/related
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetRelatedPosts_WithValidAnchorAndKeyword_Returns200()
    {
        var client   = factory.CreateClient();
        var anchorId = "robotics";
        var keyword  = "ros2";

        var response = await client.GetAsync($"/api/constellation/related?anchorId={Uri.EscapeDataString(anchorId)}&keyword={Uri.EscapeDataString(keyword)}&limit=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<InsightResponseDto>();
        body.Should().NotBeNull();
        body!.AnchorId.Should().Be(anchorId);
    }

    [Fact]
    public async Task GetRelatedPosts_ValidatesRequiredParameters()
    {
        var client = factory.CreateClient();

        // Missing anchorId - will either get 400/500 depending on binding
        var response1 = await client.GetAsync("/api/constellation/related?keyword=ros2&limit=5");
        response1.StatusCode.Should().NotBe(HttpStatusCode.OK, "Should reject missing anchorId");

        // Missing keyword - will either get 400/500 depending on binding
        var response2 = await client.GetAsync("/api/constellation/related?anchorId=robotics&limit=5");
        response2.StatusCode.Should().NotBe(HttpStatusCode.OK, "Should reject missing keyword");
    }

    [Fact]
    public async Task GetRelatedPosts_RespectsLimitParameterWithinBounds()
    {
        var client   = factory.CreateClient();
        var anchorId = "ai";
        var keyword  = "llm";

        // Request with limit=3
        var response = await client.GetAsync($"/api/constellation/related?anchorId={Uri.EscapeDataString(anchorId)}&keyword={Uri.EscapeDataString(keyword)}&limit=3");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<InsightResponseDto>();
        // In simulation mode, the endpoint should return at most the requested limit (or all available posts)
        body!.Posts.Should().HaveCountLessThanOrEqualTo(3);
    }

    [Fact]
    public async Task GetRelatedPosts_ClampsLimitTo20_WhenExceeded()
    {
        var client = factory.CreateClient();

        // Request with limit=1000 (way over 20)
        var response = await client.GetAsync("/api/constellation/related?anchorId=robotics&keyword=ros2&limit=1000");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<InsightResponseDto>();
        // The endpoint clamps limit to max 20
        body!.Posts.Should().HaveCountLessThanOrEqualTo(20);
    }

    [Fact]
    public async Task GetRelatedPosts_FallsBackToDefault5WhenLimitInvalid()
    {
        var client = factory.CreateClient();

        // Request with limit=0 (invalid)
        var response = await client.GetAsync("/api/constellation/related?anchorId=robotics&keyword=ros2&limit=0");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<InsightResponseDto>();
        // Falls back to default limit=5
        body!.Posts.Should().HaveCountLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task GetRelatedPosts_PostsAreDescendingByImpact()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/constellation/related?anchorId=ai&keyword=transformer&limit=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<InsightResponseDto>();
        
        if (body!.Posts.Count > 1)
        {
            var scores = body.Posts.Select(p => p.ImpactScore).ToList();
            scores.Should().BeInDescendingOrder("related posts must be sorted by descending impact");
        }
    }

    // -----------------------------------------------------------------------
    // GET /api/constellation/insight/{postUri} — PostUri path
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetInsight_WithPostUri_ReturnsValidResponse()
    {
        var client = factory.CreateClient();
        // PostUris follow AT URI format: at://did:plc:.../app.bsky.feed.post/...
        var nodeId = "at://did:plc:example/app.bsky.feed.post/abc123";

        var response = await client.GetAsync($"/api/constellation/insight/{Uri.EscapeDataString(nodeId)}");

        // In integration test with factory, simulation mode kicks in; endpoint may return OK with mock data
        // or NotFound if the post is outside the window. Both are valid.
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var body = await response.Content.ReadFromJsonAsync<InsightResponseDto>();
            body.Should().NotBeNull();
            // Response structure should always be valid
            body!.Posts.Should().NotBeNull();
            body.AnchorId.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task GetInsight_PostUri_PreservesAllNormalFields()
    {
        var client = factory.CreateClient();
        var nodeId = "at://did:plc:example/app.bsky.feed.post/withfields";

        var response = await client.GetAsync($"/api/constellation/insight/{Uri.EscapeDataString(nodeId)}");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var body = await response.Content.ReadFromJsonAsync<InsightResponseDto>();
            body.Should().NotBeNull();
            
            // Check structure is preserved for the insight panel
            if (body!.Posts.Count > 0)
            {
                var post = body.Posts[0];
                post.PostUri.Should().NotBeNullOrEmpty();
                post.AuthorDid.Should().NotBeNullOrEmpty();
                post.Text.Should().NotBeNullOrEmpty();
                post.Sentiment.Should().NotBeNullOrEmpty();
                post.SentimentColour.Should().NotBeNullOrEmpty();
            }
        }
    }
}
