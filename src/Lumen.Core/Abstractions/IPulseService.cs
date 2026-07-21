using Lumen.Core.Models;

namespace Lumen.Core.Abstractions;

/// <summary>One Lumen Pulse health check. Checks are independent, side-effect-light, and must never throw.</summary>
public interface IPulseCheck
{
    string Id { get; }

    Task<PulseCheck> RunAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Lumen Pulse: runs all registered local health checks and aggregates them into a report. Runs
/// entirely on the device and is never uploaded automatically; the report can be exported with
/// sensitive data redacted.
/// </summary>
public interface IPulseService
{
    Task<PulseReport> RunAsync(CancellationToken cancellationToken = default);
}
