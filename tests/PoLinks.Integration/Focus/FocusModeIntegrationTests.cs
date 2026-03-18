using System.Net;
using System.Net.Http.Json;
using PoLinks.Integration.Fixtures;
using PoLinks.Web.Features.Constellation;

namespace PoLinks.Integration.Focus;

[Collection(IntegrationCollection.Name)]
public sealed class FocusModeIntegrationTests(PoLinksWebAppFactory factory)
{
    [Fact]
    public async Task GetFocusModeStatus_DefaultState_IsInactive()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/constellation/focus-mode/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<FocusModeStatusDto>();
        body.Should().NotBeNull();
        body!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task EnterFocusMode_WhenAnchorMissing_Returns404()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/constellation/focus-mode/enter", new EnterFocusModeRequest("robotics"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ExitFocusMode_AlwaysReturnsOk()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsync("/api/constellation/focus-mode/exit", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<FocusModeExitResponseDto>();
        body.Should().NotBeNull();
        body!.IsActive.Should().BeFalse();
    }
}
