using Lumen.Core.Models;

namespace Lumen.Core.Abstractions;

/// <summary>
/// Locates an existing official Roblox installation. Implementations must only read known
/// official install locations; they must never download, bundle, or modify protected Roblox
/// binaries. On non-Windows platforms the locator reports
/// <see cref="RobloxInstallationState.PlatformUnsupported"/>.
/// </summary>
public interface IRobloxInstallationLocator
{
    Task<RobloxInstallation> LocateAsync(RobloxProduct product, CancellationToken cancellationToken = default);
}
