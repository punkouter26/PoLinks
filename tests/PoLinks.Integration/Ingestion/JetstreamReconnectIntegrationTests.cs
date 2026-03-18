// T024: Integration tests for Jetstream reconnect cursor handling (FR-006).
// The worker must provide a cursor on reconnect so no posts are missed.
using Microsoft.Extensions.DependencyInjection;
using PoLinks.Integration.Fixtures;
using PoLinks.Web.Features.Ingestion;

namespace PoLinks.Integration.Ingestion;

[Collection("Integration")]
public sealed class JetstreamReconnectIntegrationTests(PoLinksWebAppFactory factory)
{
    [Fact]
    public void JetstreamWorker_IsRegistered_InDIContainer()
    {
        // Confirms the hosted service is registered — essential for auto-restart behaviour
        var services = factory.Services;
        var workers = services
            .GetServices<Microsoft.Extensions.Hosting.IHostedService>()
            .Select(s => s.GetType().Name);

        workers.Should().Contain(nameof(BlueskyJetstreamWorker),
            "worker must be registered so the runtime manages its lifecycle");
    }

    [Fact]
    public void JetstreamWorker_CursorIsUtcTicks()
    {
        // Cursor encoding: UNIX microseconds (int64) so Jetstream can resume post-reconnect
        var now = DateTimeOffset.UtcNow;
        var cursor = BlueskyJetstreamWorker.ToJetstreamCursor(now);
        cursor.Should().BeGreaterThan(0);

        // Roundtrip: can reconstruct approximate timestamp from cursor
        var roundtripped = BlueskyJetstreamWorker.FromJetstreamCursor(cursor);
        roundtripped.Should().BeCloseTo(now, precision: TimeSpan.FromMilliseconds(1));
    }
}
