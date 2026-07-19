namespace Lumen.Core.Models;

/// <summary>
/// A validated, opened mod package: its parsed manifest, the safety-scan report, and the
/// staging directory the (verified) contents were extracted to. Never trusted until
/// <see cref="ScanReport"/> reports safe and hashes verify.
/// </summary>
public sealed record ModPackage(
    ModManifest Manifest,
    ModScanReport ScanReport,
    string StagingDirectory);
