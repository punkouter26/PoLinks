// T088: Unit tests for pulse countdown cadence (FR-011).
// Pulse fires every 30 seconds. Countdown resets immediately after each pulse.
using PoLinks.Web.Features.Pulse;

namespace PoLinks.Unit.Pulse;


public sealed class PulseCountdownTests
{
    [Fact]
    public void PulseInterval_IsThirtySeconds()
    {
        PulseService.PulseInterval.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void GetTimeToNextPulse_AtZeroElapsed_ReturnsFullInterval()
    {
        var now = DateTimeOffset.UtcNow;
        var lastPulse = now;

        var remaining = PulseService.GetTimeToNextPulse(lastPulse, now);

        remaining.Should().BeCloseTo(TimeSpan.FromSeconds(30), precision: TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void GetTimeToNextPulse_AfterHalfInterval_ReturnsHalf()
    {
        var lastPulse = DateTimeOffset.UtcNow.AddSeconds(-15);
        var now = DateTimeOffset.UtcNow;

        var remaining = PulseService.GetTimeToNextPulse(lastPulse, now);

        remaining.Should().BeCloseTo(TimeSpan.FromSeconds(15), precision: TimeSpan.FromMilliseconds(200));
    }

    [Fact]
    public void GetTimeToNextPulse_AfterFullInterval_ReturnsZero()
    {
        var lastPulse = DateTimeOffset.UtcNow.AddSeconds(-30);
        var now = DateTimeOffset.UtcNow;

        var remaining = PulseService.GetTimeToNextPulse(lastPulse, now);

        remaining.Should().BeLessOrEqualTo(TimeSpan.Zero);
    }

    [Fact]
    public void IsPulseDue_AfterExactInterval_ReturnsTrue()
    {
        var lastPulse = DateTimeOffset.UtcNow.AddSeconds(-30);
        PulseService.IsPulseDue(lastPulse, DateTimeOffset.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void IsPulseDue_BeforeInterval_ReturnsFalse()
    {
        var lastPulse = DateTimeOffset.UtcNow.AddSeconds(-10);
        PulseService.IsPulseDue(lastPulse, DateTimeOffset.UtcNow).Should().BeFalse();
    }
}
