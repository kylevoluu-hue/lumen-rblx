namespace Lumen.Core.Models;

/// <summary>Non-sensitive inputs the caller supplies to a diagnostic report.</summary>
public sealed record DiagnosticInputs
{
    public string LumenVersion { get; init; } = "unknown";

    public string? RobloxVersion { get; init; }

    public string? RobloxInstallationStatus { get; init; }

    public int EnabledModCount { get; init; }

    public string? ActiveProfile { get; init; }

    /// <summary>Extra key/value lines to include. Values are redacted before display.</summary>
    public IReadOnlyList<KeyValuePair<string, string>> Extra { get; init; } = Array.Empty<KeyValuePair<string, string>>();
}
