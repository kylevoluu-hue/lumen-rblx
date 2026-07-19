using Lumen.Core.Models;

namespace Lumen.Core.Abstractions;

/// <summary>
/// Inspects a candidate mod package for disallowed content (executables, scripts, path
/// traversal, absolute paths, double extensions, suspicious embedded URLs, and so on) and
/// returns a <see cref="ModScanReport"/>. Never mutates the package; never auto-trusts.
/// </summary>
public interface IModSafetyScanner
{
    /// <summary>
    /// Pure, allocation-light scan over a set of relative package paths. Deterministic and
    /// side-effect free — the primary unit-testable entry point.
    /// </summary>
    ModScanReport ScanEntries(IEnumerable<string> relativePaths);

    /// <summary>
    /// Scans an extracted staging directory: runs <see cref="ScanEntries"/> over its file list
    /// and additionally inspects file contents (e.g. for suspicious embedded URLs).
    /// </summary>
    Task<ModScanReport> ScanDirectoryAsync(string directory, CancellationToken cancellationToken = default);
}
