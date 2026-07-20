using Lumen.Core.Abstractions;
using Lumen.Core.Common;
using Lumen.Core.Configuration;
using Lumen.Core.Models;
using Lumen.Security;

namespace Lumen.Mods;

/// <summary>Tracks which content files Lumen has applied, so they can be reverted.</summary>
public sealed class AppliedMods
{
    public List<string> Files { get; set; } = new();
}

/// <summary>
/// Applies a validated mod package's <c>Content/</c> subtree into the Roblox content folder, backing
/// up originals so <see cref="RestoreAsync"/> can revert. Every destination is confined to the
/// content folder via <see cref="PathSafety"/>; protected Roblox binaries are never touched.
/// The path-mapping, backup, and restore logic is exercised by unit tests against a temp content
/// root; resolving the real Roblox content folder is Windows-only.
/// </summary>
public sealed class ModApplicator : IModApplicator
{
    private readonly IRobloxInstallationLocator _locator;
    private readonly ILumenPaths _paths;
    private readonly IVersionedJsonStore _store;

    public ModApplicator(IRobloxInstallationLocator locator, ILumenPaths paths, IVersionedJsonStore store)
    {
        _locator = locator ?? throw new ArgumentNullException(nameof(locator));
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    private string AppliedFilePath => Path.Combine(_paths.Mods, "applied.json");

    private string ContentBackupRoot => Path.Combine(_paths.Backups, "content");

    public async Task<Result<int>> ApplyAsync(ModPackage package, CancellationToken cancellationToken = default)
    {
        var contentRoot = await ResolveContentRootAsync(cancellationToken).ConfigureAwait(false);
        return contentRoot.IsFailure
            ? Result.Failure<int>(contentRoot.Error!)
            : await ApplyToContentRootAsync(package, contentRoot.Value!, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result> RestoreAsync(CancellationToken cancellationToken = default)
    {
        var contentRoot = await ResolveContentRootAsync(cancellationToken).ConfigureAwait(false);
        return contentRoot.IsFailure
            ? Result.Failure(contentRoot.Error!)
            : await RestoreFromContentRootAsync(contentRoot.Value!, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Core apply logic against an explicit content root (unit-testable without Windows).</summary>
    public async Task<Result<int>> ApplyToContentRootAsync(
        ModPackage package, string contentRoot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(package);

        var sourceContent = FindContentFolder(package.StagingDirectory);
        if (sourceContent is null)
        {
            return Result.Failure<int>("This mod has no 'Content' folder to apply.");
        }

        Directory.CreateDirectory(contentRoot);
        var applied = await LoadAppliedAsync(cancellationToken).ConfigureAwait(false);

        var written = 0;
        foreach (var file in Directory.EnumerateFiles(sourceContent, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relative = Path.GetRelativePath(sourceContent, file).Replace('\\', '/');

            var target = PathSafety.ResolveWithinRoot(contentRoot, relative);
            if (target.IsFailure)
            {
                return Result.Failure<int>(target.Error!);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(target.Value!)!);

            // Back up a pre-existing original exactly once.
            if (File.Exists(target.Value!))
            {
                var backup = PathSafety.ResolveWithinRoot(ContentBackupRoot, relative);
                if (backup.IsSuccess && !File.Exists(backup.Value!))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(backup.Value!)!);
                    File.Copy(target.Value!, backup.Value!, overwrite: false);
                }
            }

            File.Copy(file, target.Value!, overwrite: true);
            if (!applied.Files.Contains(relative))
            {
                applied.Files.Add(relative);
            }

            written++;
        }

        await _store.SaveAsync(AppliedFilePath, applied, cancellationToken).ConfigureAwait(false);
        return Result.Success(written);
    }

    /// <summary>Core restore logic against an explicit content root (unit-testable without Windows).</summary>
    public async Task<Result> RestoreFromContentRootAsync(string contentRoot, CancellationToken cancellationToken = default)
    {
        var applied = await LoadAppliedAsync(cancellationToken).ConfigureAwait(false);

        foreach (var relative in applied.Files)
        {
            var target = PathSafety.ResolveWithinRoot(contentRoot, relative);
            var backup = PathSafety.ResolveWithinRoot(ContentBackupRoot, relative);
            if (target.IsFailure)
            {
                continue;
            }

            try
            {
                if (backup.IsSuccess && File.Exists(backup.Value!))
                {
                    // Restore the original and remove the backup.
                    Directory.CreateDirectory(Path.GetDirectoryName(target.Value!)!);
                    File.Copy(backup.Value!, target.Value!, overwrite: true);
                    File.Delete(backup.Value!);
                }
                else if (File.Exists(target.Value!))
                {
                    // Lumen added this file (no original existed); remove it.
                    File.Delete(target.Value!);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Best-effort; continue restoring the rest.
            }
        }

        applied.Files.Clear();
        await _store.SaveAsync(AppliedFilePath, applied, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private async Task<AppliedMods> LoadAppliedAsync(CancellationToken cancellationToken)
    {
        var result = await _store.LoadAsync(AppliedFilePath, static () => new AppliedMods(), cancellationToken)
            .ConfigureAwait(false);
        return result.IsSuccess ? result.Value! : new AppliedMods();
    }

    private static string? FindContentFolder(string stagingDirectory)
    {
        foreach (var name in new[] { "Content", "content" })
        {
            var candidate = Path.Combine(stagingDirectory, name);
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private async Task<Result<string>> ResolveContentRootAsync(CancellationToken cancellationToken)
    {
        var install = await _locator.LocateAsync(RobloxProduct.Player, cancellationToken).ConfigureAwait(false);
        if (!install.IsUsable || string.IsNullOrEmpty(install.InstallPath))
        {
            return Result.Failure<string>(
                "No usable Roblox installation was detected. Mods apply on Windows with Roblox installed.");
        }

        var versionDirectory = Path.GetDirectoryName(install.InstallPath);
        if (string.IsNullOrEmpty(versionDirectory))
        {
            return Result.Failure<string>("Could not resolve the Roblox installation folder.");
        }

        return Result.Success(Path.Combine(versionDirectory, "content"));
    }
}
