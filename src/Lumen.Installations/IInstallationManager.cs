using Lumen.Core.Common;
using Lumen.Core.Models;

namespace Lumen.Installations;

/// <summary>
/// Verifies, repairs, and backs up launcher-managed (cosmetic) files layered over an official
/// Roblox installation. It only ever touches Lumen-managed files and always backs up originals
/// before changing them; it never modifies or replaces protected Roblox binaries.
///
/// PHASE 3 (roadmap): concrete repair/restore is Windows-runtime work, deferred from this
/// foundation milestone. The <see cref="IRobloxInstallationLocator"/> that this builds on is fully
/// implemented in <c>Lumen.Roblox</c>.
/// </summary>
public interface IInstallationManager
{
    Task<Result> VerifyAsync(RobloxInstallation installation, CancellationToken cancellationToken = default);

    Task<Result> RestoreCosmeticFilesAsync(CancellationToken cancellationToken = default);
}
