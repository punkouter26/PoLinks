// T012: Sensitive value masker for Serilog destructuring (FR-017).
// Prevents API keys, connection strings, and access tokens from being logged.
using System.Text.RegularExpressions;

namespace PoLinks.Web.Features.Shared.Masking;

/// <summary>
/// Provides masking utilities for secrets in log output.
/// Applied as a Serilog enricher to ensure keys and tokens do not appear in logs.
/// </summary>
public static partial class SensitiveValueMasker
{
    private const string MaskedValue = "[REDACTED]";

    // Matches common connection string secret segments (AccountKey, Password, pwd, secret, token)
    [GeneratedRegex(
        @"(?<key>AccountKey|Password|pwd|secret|token|api[_-]?key)\s*=\s*(?<value>[^;,\s""']+)",
        RegexOptions.IgnoreCase,
        matchTimeoutMilliseconds: 500)]
    private static partial Regex SensitiveSegmentPattern();

    /// <summary>
    /// Scrubs known sensitive key-value patterns from a single string value,
    /// replacing the secret portion with <c>[REDACTED]</c>.
    /// </summary>
    public static string Mask(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        return SensitiveSegmentPattern().Replace(value, m =>
            $"{m.Groups["key"].Value}={MaskedValue}");
    }
}
