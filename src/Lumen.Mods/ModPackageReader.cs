using Lumen.Core.Abstractions;
using Lumen.Core.Common;
using Lumen.Core.Models;
using Lumen.Security;

namespace Lumen.Mods;

/// <summary>
/// Opens a <c>.lumenmod</c> package end-to-end: safe extraction (zip-slip and decompression-bomb
/// guarded), manifest parsing and validation, safety scanning, and hash verification. If any step
/// fails, the staging directory is discarded and a failure is returned — a package is never left
/// in a partially-trusted state.
/// </summary>
public sealed class ModPackageReader : IModPackageReader
{
    private readonly IModSafetyScanner _scanner;
    private readonly ArchiveLimits _limits;

    public ModPackageReader(IModSafetyScanner scanner, ArchiveLimits? limits = null)
    {
        _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
        _limits = limits ?? ArchiveLimits.ForMods;
    }

    public async Task<Result<ModPackage>> OpenAsync(
        string packagePath, string stagingDirectory, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(packagePath))
        {
            return Result.Failure<ModPackage>("Package file not found.");
        }

        try
        {
            // 1. Safe extraction into a fresh staging directory.
            if (Directory.Exists(stagingDirectory))
            {
                Directory.Delete(stagingDirectory, recursive: true);
            }

            var extract = SafeArchiveExtractor.ExtractZip(packagePath, stagingDirectory, _limits);
            if (extract.IsFailure)
            {
                return Fail(stagingDirectory, extract.Error!);
            }

            // 2. Manifest.
            var manifestResult = await ModManifestReader
                .ReadFromFileAsync(Path.Combine(stagingDirectory, "manifest.json"), cancellationToken)
                .ConfigureAwait(false);
            if (manifestResult.IsFailure)
            {
                return Fail(stagingDirectory, manifestResult.Error!);
            }

            // 3. Safety scan.
            var report = await _scanner.ScanDirectoryAsync(stagingDirectory, cancellationToken).ConfigureAwait(false);
            if (!report.IsSafe)
            {
                var reason = report.Blocking.FirstOrDefault();
                return Fail(stagingDirectory,
                    $"Package rejected by safety scan: {reason?.Reason ?? "unsafe content"} ({reason?.Path}).");
            }

            // 4. Verify declared hashes against the extracted files.
            var hashResult = await VerifyHashesAsync(manifestResult.Value!, stagingDirectory, cancellationToken)
                .ConfigureAwait(false);
            if (hashResult.IsFailure)
            {
                return Fail(stagingDirectory, hashResult.Error!);
            }

            return Result.Success(new ModPackage(manifestResult.Value!, report, stagingDirectory));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Fail(stagingDirectory, $"Could not open package: {ex.Message}");
        }
    }

    private static async Task<Result> VerifyHashesAsync(
        ModManifest manifest, string stagingDirectory, CancellationToken cancellationToken)
    {
        foreach (var (relativePath, expectedHash) in manifest.Hashes)
        {
            var resolved = PathSafety.ResolveWithinRoot(stagingDirectory, relativePath);
            if (resolved.IsFailure)
            {
                return Result.Failure(resolved.Error!);
            }

            var verify = await FileHashing.VerifyFileAsync(resolved.Value!, expectedHash, cancellationToken)
                .ConfigureAwait(false);
            if (verify.IsFailure)
            {
                return verify;
            }
        }

        return Result.Success();
    }

    private static Result<ModPackage> Fail(string stagingDirectory, string error)
    {
        TryCleanup(stagingDirectory);
        return Result.Failure<ModPackage>(error);
    }

    private static void TryCleanup(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
