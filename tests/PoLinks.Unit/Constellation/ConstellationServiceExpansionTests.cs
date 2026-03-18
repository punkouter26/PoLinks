// Unit tests for ConstellationService.GetRelatedPosts and the new PostUri path
// in GetNodeInsight (expansion graph feature).
using Microsoft.Extensions.Options;
using PoLinks.Web.Features.Constellation;
using PoLinks.Web.Features.Shared.Entities;

namespace PoLinks.Unit.Constellation;

public sealed class ConstellationServiceExpansionTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static ConstellationService CreateService(int maxNodes = 100, int windowMinutes = 60)
    {
        var opts = Options.Create(new ConstellationOptions
        {
            MaxNodeCount         = maxNodes,
            NodeFadeWindowMinutes = windowMinutes,
        });
        return new ConstellationService(opts);
    }

    private static IngestedPost MakePost(
        string anchorId,
        string keyword,
        string postUri,
        string authorDid    = "did:plc:author1",
        double impactScore  = 1.0,
        int    minutesAgo   = 10) => new()
    {
        PostUri        = postUri,
        AuthorDid      = authorDid,
        Text           = $"Post about {keyword}",
        MatchedAnchorId = anchorId,
        MatchedKeyword  = keyword,
        CreatedAt       = DateTimeOffset.UtcNow.AddMinutes(-minutesAgo),
        ImpactScore     = impactScore,
        Sentiment       = SentimentLabel.Neutral,
    };

    // -----------------------------------------------------------------------
    // GetRelatedPosts
    // -----------------------------------------------------------------------

    [Fact]
    public void GetRelatedPosts_ReturnsTopPostsMatchingKeyword_OrderedByImpact()
    {
        var svc = CreateService();
        svc.AddPost(MakePost("robotics", "ros2", "at://did:post1", impactScore: 3.0));
        svc.AddPost(MakePost("robotics", "ros2", "at://did:post2", impactScore: 1.0));
        svc.AddPost(MakePost("robotics", "ros2", "at://did:post3", impactScore: 2.0));
        svc.AddPost(MakePost("robotics", "lidar", "at://did:post4", impactScore: 9.0)); // different keyword

        var result = svc.GetRelatedPosts("robotics", "ros2", limit: 5);

        result.Should().HaveCount(3);
        result[0].PostUri.Should().Be("at://did:post1"); // highest impact first
        result[1].PostUri.Should().Be("at://did:post3");
        result[2].PostUri.Should().Be("at://did:post2");
    }

    [Fact]
    public void GetRelatedPosts_RespectsLimit()
    {
        var svc = CreateService();
        for (var i = 1; i <= 10; i++)
            svc.AddPost(MakePost("robotics", "ros2", $"at://did:post{i}", impactScore: i));

        var result = svc.GetRelatedPosts("robotics", "ros2", limit: 5);

        result.Should().HaveCount(5);
    }

    [Fact]
    public void GetRelatedPosts_ReturnsEmpty_WhenAnchorNotFound()
    {
        var svc = CreateService();
        svc.AddPost(MakePost("robotics", "ros2", "at://did:post1"));

        var result = svc.GetRelatedPosts("nonexistent", "ros2");

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetRelatedPosts_ExcludesPostsOutsideRollingWindow()
    {
        var svc = CreateService(windowMinutes: 60);
        svc.AddPost(MakePost("robotics", "ros2", "at://did:fresh",  minutesAgo: 10));
        svc.AddPost(MakePost("robotics", "ros2", "at://did:stale",  minutesAgo: 90)); // outside 60-min window

        var result = svc.GetRelatedPosts("robotics", "ros2");

        result.Should().ContainSingle();
        result[0].PostUri.Should().Be("at://did:fresh");
    }

    [Fact]
    public void GetRelatedPosts_IsCaseInsensitiveForKeyword()
    {
        var svc = CreateService();
        svc.AddPost(MakePost("robotics", "ROS2", "at://did:post1"));
        svc.AddPost(MakePost("robotics", "Ros2", "at://did:post2"));

        var result = svc.GetRelatedPosts("robotics", "ros2");

        result.Should().HaveCount(2);
    }

    // -----------------------------------------------------------------------
    // GetNodeInsight — PostUri path
    // -----------------------------------------------------------------------

    [Fact]
    public void GetNodeInsight_WithPostUri_ReturnsThatSinglePost()
    {
        const string postUri = "at://did:plc:xyz/app.bsky.feed.post/abc123";
        var svc = CreateService();
        svc.AddPost(MakePost("ml", "llm", postUri));
        svc.AddPost(MakePost("ml", "llm", "at://did:plc:xyz/other"));

        var result = svc.GetNodeInsight(postUri);

        result.Should().NotBeNull();
        result!.Posts.Should().ContainSingle();
        result.Posts[0].PostUri.Should().Be(postUri);
    }

    [Fact]
    public void GetNodeInsight_WithPostUri_ReturnsNull_WhenUriNotFound()
    {
        var svc = CreateService();
        svc.AddPost(MakePost("ml", "llm", "at://did:plc:xyz/other"));

        var result = svc.GetNodeInsight("at://did:plc:xyz/notexist");

        result.Should().BeNull();
    }

    [Fact]
    public void GetNodeInsight_WithPostUri_ReturnsNull_WhenPostOutsideWindow()
    {
        const string postUri = "at://did:plc:xyz/app.bsky.feed.post/old";
        var svc = CreateService(windowMinutes: 60);
        svc.AddPost(MakePost("ml", "llm", postUri, minutesAgo: 120));

        var result = svc.GetNodeInsight(postUri);

        result.Should().BeNull();
    }
}
