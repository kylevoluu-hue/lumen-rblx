using Lumen.Core.Abstractions;
using Lumen.Core.Models;

namespace Lumen.Roblox;

/// <summary>
/// Detects an existing official Roblox installation by inspecting the standard per-user install
/// location. It is strictly read-only: it never downloads, bundles, modifies, or replaces Roblox
/// binaries. On non-Windows platforms it reports
/// <see cref="RobloxInstallationState.PlatformUnsupported"/> instead of guessing.
/// </summary>
public sealed class RobloxInstallationLocator : IRobloxInstallationLocator
{
    public Task<RobloxInstallation> LocateAsync(RobloxProduct product, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult(RobloxInstallation.Unsupported(product));
        }

        return Task.Run(() => LocateOnWindows(product), cancellationToken);
    }

    private static RobloxInstallation LocateOnWindows(RobloxProduct product)
    {
        var executableName = product == RobloxProduct.Player ? "RobloxPlayerBeta.exe" : "RobloxStudioBeta.exe";

        var versionsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Roblox",
            "Versions");

        if (!Directory.Exists(versionsDirectory))
        {
            return RobloxInstallation.NotFound(product);
        }

        string? newestExe = null;
        var newestWrite = DateTime.MinValue;

        foreach (var versionDir in Directory.EnumerateDirectories(versionsDirectory))
        {
            var candidate = Path.Combine(versionDir, executableName);
            if (!File.Exists(candidate))
            {
                continue;
            }

            var written = File.GetLastWriteTimeUtc(candidate);
            if (written > newestWrite)
            {
                newestWrite = written;
                newestExe = candidate;
            }
        }

        if (newestExe is null)
        {
            return RobloxInstallation.NotFound(product);
        }

        // The folder name is Roblox's version token (e.g. "version-0123abcd...").
        var version = Path.GetFileName(Path.GetDirectoryName(newestExe));

        return new RobloxInstallation(
            product,
            RobloxInstallationState.Healthy,
            version,
            newestExe,
            "Detected an official Roblox installation.");
    }
}
