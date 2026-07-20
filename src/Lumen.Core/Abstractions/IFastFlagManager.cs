using Lumen.Core.Common;
using Lumen.Core.Models;

namespace Lumen.Core.Abstractions;

/// <summary>
/// Manages a safe, allowlisted set of Roblox FastFlags by writing the supported
/// <c>ClientAppSettings.json</c> override file for the detected Roblox installation. Every value is
/// validated against the allowlist and range before it is written; the exact file content is
/// previewable; changes are backed up and can be restored to defaults. No flag that affects
/// anti-cheat, moderation, networking fairness, or hidden-player/object visibility is ever offered.
/// </summary>
public interface IFastFlagManager
{
    IReadOnlyList<FastFlagDefinition> Allowlist { get; }

    /// <summary>Validates values against the allowlist and returns the exact JSON that would be written.</summary>
    Result<string> BuildPreview(IReadOnlyDictionary<string, string> values);

    /// <summary>Reads the currently-applied allowlisted flags from the installation (empty if none).</summary>
    Task<Result<Dictionary<string, string>>> ReadCurrentAsync(CancellationToken cancellationToken = default);

    /// <summary>Validates, then atomically writes <c>ClientAppSettings.json</c> (with backup). Returns the path written.</summary>
    Task<Result<string>> ApplyAsync(IReadOnlyDictionary<string, string> values, CancellationToken cancellationToken = default);

    /// <summary>Clears all overrides (writes an empty settings object), backing up the previous file.</summary>
    Task<Result> RestoreDefaultsAsync(CancellationToken cancellationToken = default);
}
