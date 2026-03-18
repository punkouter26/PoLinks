// T059: In-memory log buffer for the Live Error Terminal Drawer (FR-012).
// Captures the most recent log entries from the ASP.NET Core logging pipeline
// and exposes them via GET /diagnostic/logs.
using System.Collections.Concurrent;

namespace PoLinks.Web.Features.Diagnostic;

/// <summary>A single captured log entry.</summary>
public sealed record DiagnosticLogEntry(
    string Id,
    string Level,
    string Message,
    string? Exception,
    IReadOnlyDictionary<string, string> Context,
    DateTimeOffset Timestamp);

/// <summary>
/// Thread-safe ring-buffer of recent log entries.
/// Holds at most <see cref="Capacity"/> entries; oldest are dropped when full.
/// Registered as a singleton and consumed by both the logger provider and the endpoint.
/// </summary>
public sealed class DiagnosticLogBuffer
{
    private const int Capacity = 200;

    private readonly ConcurrentQueue<DiagnosticLogEntry> _entries = new();

    public void Add(DiagnosticLogEntry entry)
    {
        _entries.Enqueue(entry);

        // Trim to capacity — a ConcurrentQueue is FIFO so dequeue from the front.
        while (_entries.Count > Capacity)
            _entries.TryDequeue(out _);
    }

    /// <summary>Returns a snapshot of all buffered entries, newest last.</summary>
    public IReadOnlyList<DiagnosticLogEntry> GetAll() => _entries.ToArray();
}

/// <summary>
/// ILogger implementation that writes to a <see cref="DiagnosticLogBuffer"/>.
/// </summary>
internal sealed class DiagnosticBufferLogger(string categoryName, DiagnosticLogBuffer buffer) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var context = new Dictionary<string, string>
        {
            ["category"] = categoryName,
        };

        if (state is IEnumerable<KeyValuePair<string, object?>> props)
        {
            foreach (var (k, v) in props)
                if (k != "{OriginalFormat}" && v is not null)
                    context[k] = v.ToString() ?? string.Empty;
        }

        var entry = new DiagnosticLogEntry(
            Id: Guid.NewGuid().ToString("N"),
            Level: logLevel.ToString(),
            Message: formatter(state, exception),
            Exception: exception?.ToString(),
            Context: context,
            Timestamp: DateTimeOffset.UtcNow);

        buffer.Add(entry);
    }
}

/// <summary>
/// ILoggerProvider that feeds log entries into <see cref="DiagnosticLogBuffer"/>.
/// </summary>
[ProviderAlias("DiagnosticBuffer")]
public sealed class DiagnosticLoggerProvider(DiagnosticLogBuffer buffer) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) =>
        new DiagnosticBufferLogger(categoryName, buffer);

    public void Dispose() { }
}
