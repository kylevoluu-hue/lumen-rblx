namespace Lumen.Core.Models;

/// <summary>Severity of a single Lumen Pulse health check, worst-last so it orders/compares naturally.</summary>
public enum PulseSeverity
{
    Ok = 0,
    Info = 1,
    Warning = 2,
    Critical = 3,
}

/// <summary>The result of one health check.</summary>
public sealed record PulseCheck(
    string Id,
    string Title,
    PulseSeverity Severity,
    string Explanation,
    string? RecommendedAction = null,
    bool AutoRepairAvailable = false)
{
    public bool HasRecommendedAction => !string.IsNullOrEmpty(RecommendedAction);
}

/// <summary>A complete Lumen Pulse run: every check plus the aggregate status.</summary>
public sealed class PulseReport
{
    public required IReadOnlyList<PulseCheck> Checks { get; init; }

    public DateTimeOffset GeneratedUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>The worst individual severity (Ok when there are no checks).</summary>
    public PulseSeverity Overall => Checks.Count == 0 ? PulseSeverity.Ok : Checks.Max(c => c.Severity);
}
