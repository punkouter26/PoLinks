using System.Diagnostics.Metrics;
using Microsoft.Extensions.Options;

namespace PoLinks.Web.Features.Diagnostic;

public sealed class UptimeMetricsService
{
    private readonly Meter _meter = new("PoLinks.Diagnostics.Uptime", "1.0.0");
    private readonly Counter<long> _probeCounter;
    private readonly Counter<long> _availableProbeCounter;
    private readonly List<UptimeProbe> _probes = [];
    private readonly object _gate = new();
    private readonly IReadOnlyList<MaintenanceWindow> _maintenanceWindows;

    public UptimeMetricsService(IOptions<UptimeMetricsOptions>? options = null)
    {
        _probeCounter = _meter.CreateCounter<long>("polinks_uptime_probes_total");
        _availableProbeCounter = _meter.CreateCounter<long>("polinks_uptime_available_probes_total");
        _maintenanceWindows = options?.Value.MaintenanceWindows ?? [];
    }

    public void RecordProbe(bool isAvailable, DateTimeOffset observedAtUtc)
    {
        _probeCounter.Add(1);
        if (isAvailable)
        {
            _availableProbeCounter.Add(1);
        }

        lock (_gate)
        {
            _probes.Add(new UptimeProbe(observedAtUtc, isAvailable));
            var cutoff = observedAtUtc.AddDays(-30);
            _probes.RemoveAll(probe => probe.ObservedAtUtc < cutoff);
        }
    }

    public UptimeSliReport GetReport(DateTimeOffset asOfUtc)
    {
        List<UptimeProbe> relevant;
        lock (_gate)
        {
            relevant = _probes
                .Where(probe => !IsInMaintenanceWindow(probe.ObservedAtUtc))
                .ToList();
        }

        var total = relevant.Count;
        var available = relevant.Count(probe => probe.IsAvailable);
        var percentage = total == 0 ? 100d : Math.Round((double)available / total * 100d, 3);

        return new UptimeSliReport(
            percentage,
            available,
            total,
            99.5,
            asOfUtc,
            total == 0 ? "No uptime probes recorded yet; report defaults to 100%." : null);
    }

    private bool IsInMaintenanceWindow(DateTimeOffset timestamp)
    {
        foreach (var window in _maintenanceWindows)
        {
            if (timestamp >= window.StartUtc && timestamp <= window.EndUtc)
            {
                return true;
            }
        }

        return false;
    }
}

public sealed record MaintenanceWindow(DateTimeOffset StartUtc, DateTimeOffset EndUtc, string? Reason = null);

public sealed record UptimeProbe(DateTimeOffset ObservedAtUtc, bool IsAvailable);

public sealed record UptimeSliReport(
    double UptimePercentage,
    int AvailableProbeCount,
    int TotalProbeCount,
    double TargetPercentage,
    DateTimeOffset AsOfUtc,
    string? Note);

public sealed class UptimeMetricsOptions
{
    public List<MaintenanceWindow> MaintenanceWindows { get; init; } = [];
}
