using Lumen.Core.Common;

namespace Lumen.Updates;

public enum UpdateChannel
{
    Stable,
    Beta,
    Development,
}

public sealed record UpdateInfo(string Version, string ReleaseNotes, UpdateChannel Channel);

/// <summary>
/// Checks for and applies Lumen updates securely: HTTPS only, from the official release source,
/// with signed metadata and file-hash verification, release notes and user confirmation before
/// installing, rollback on failure, and never while Roblox is running. It never bundles unrelated
/// software, installs advertising, or silently adds startup services.
///
/// PHASE 10 (roadmap): the concrete updater is deferred from this foundation milestone. The
/// security building blocks (HTTPS allowlist, hashing, signature verification hooks) live in
/// <c>Lumen.Security</c>.
/// </summary>
public interface IUpdateService
{
    Task<Result<UpdateInfo?>> CheckForUpdateAsync(UpdateChannel channel, CancellationToken cancellationToken = default);
}
