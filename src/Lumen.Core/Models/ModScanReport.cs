namespace Lumen.Core.Models;

public enum ModScanSeverity
{
    Info = 0,
    Warning = 1,

    /// <summary>A disqualifying problem. Any Blocked finding makes the whole package unsafe.</summary>
    Blocked = 2,
}

public sealed record ModScanFinding(ModScanSeverity Severity, string Path, string Reason);

/// <summary>
/// The result of scanning a candidate mod package. A package is only considered safe when it
/// has zero <see cref="ModScanSeverity.Blocked"/> findings. A passing scan reduces risk but is
/// explicitly not a guarantee of absolute safety — this is stated to users.
/// </summary>
public sealed class ModScanReport
{
    private readonly List<ModScanFinding> _findings = new();

    public IReadOnlyList<ModScanFinding> Findings => _findings;

    public bool IsSafe => _findings.TrueForAll(f => f.Severity != ModScanSeverity.Blocked);

    public bool HasWarnings => _findings.Exists(f => f.Severity == ModScanSeverity.Warning);

    public void Add(ModScanSeverity severity, string path, string reason) =>
        _findings.Add(new ModScanFinding(severity, path, reason));

    public IEnumerable<ModScanFinding> Blocking =>
        _findings.Where(f => f.Severity == ModScanSeverity.Blocked);
}
