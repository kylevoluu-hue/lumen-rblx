using System.Buffers;
using System.IO.Compression;
using Lumen.Core.Common;

namespace Lumen.Security;

/// <summary>Caps that bound archive extraction to protect against decompression bombs.</summary>
public sealed record ArchiveLimits(
    int MaxEntries = 4000,
    long MaxTotalBytes = 512L * 1024 * 1024,
    long MaxEntryBytes = 128L * 1024 * 1024,
    double MaxCompressionRatio = 120.0,
    long RatioCheckFloorBytes = 1_000_000)
{
    /// <summary>Reasonable defaults for cosmetic mod packages.</summary>
    public static ArchiveLimits ForMods { get; } = new();
}

/// <summary>
/// Extracts ZIP archives (the container for <c>.lumenmod</c> packages) with strict safety
/// guarantees:
/// <list type="bullet">
///   <item>Zip-slip prevention: every entry is resolved and confined to the destination.</item>
///   <item>Decompression-bomb prevention: per-entry, total-size, and compression-ratio caps,
///   enforced while streaming so a lying <c>entry.Length</c> cannot bypass them.</item>
///   <item>Duplicate/overwrite prevention: entries are written with <c>CreateNew</c>.</item>
/// </list>
/// Extraction is not atomic on its own; callers extract into a disposable staging directory and
/// discard it on failure.
/// </summary>
public static class SafeArchiveExtractor
{
    public static Result ExtractZip(string zipPath, string destinationDirectory, ArchiveLimits? limits = null)
    {
        limits ??= ArchiveLimits.ForMods;

        try
        {
            Directory.CreateDirectory(destinationDirectory);

            using var archive = ZipFile.OpenRead(zipPath);

            if (archive.Entries.Count > limits.MaxEntries)
            {
                return Result.Failure(
                    $"Archive has too many entries ({archive.Entries.Count} > {limits.MaxEntries}).");
            }

            long totalWritten = 0;

            foreach (var entry in archive.Entries)
            {
                // Directory entries have an empty Name; nothing to extract.
                if (string.IsNullOrEmpty(entry.Name))
                {
                    continue;
                }

                var resolved = PathSafety.ResolveWithinRoot(destinationDirectory, entry.FullName);
                if (resolved.IsFailure)
                {
                    return Result.Failure(resolved.Error!);
                }

                // Fast reject using the declared size and compression ratio.
                if (entry.Length > limits.MaxEntryBytes)
                {
                    return Result.Failure($"Entry '{entry.FullName}' exceeds the per-file size limit.");
                }

                if (entry.CompressedLength > 0 && entry.Length > limits.RatioCheckFloorBytes)
                {
                    var ratio = (double)entry.Length / entry.CompressedLength;
                    if (ratio > limits.MaxCompressionRatio)
                    {
                        return Result.Failure(
                            $"Entry '{entry.FullName}' has a suspicious compression ratio (~{ratio:F0}x); rejected as a possible decompression bomb.");
                    }
                }

                var targetPath = resolved.Value!;
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

                var streamResult = ExtractEntry(entry, targetPath, limits, ref totalWritten);
                if (streamResult.IsFailure)
                {
                    return streamResult;
                }
            }

            return Result.Success();
        }
        catch (InvalidDataException ex)
        {
            // Corrupt archive, or an encrypted entry that cannot be opened.
            return Result.Failure($"Archive is invalid, encrypted, or corrupt: {ex.Message}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return Result.Failure($"Archive extraction failed: {ex.Message}");
        }
    }

    private static Result ExtractEntry(ZipArchiveEntry entry, string targetPath, ArchiveLimits limits, ref long totalWritten)
    {
        using var source = entry.Open();
        using var destination = new FileStream(targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);

        var buffer = ArrayPool<byte>.Shared.Rent(81920);
        try
        {
            long entryWritten = 0;
            int read;
            while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                entryWritten += read;
                totalWritten += read;

                if (entryWritten > limits.MaxEntryBytes)
                {
                    return Result.Failure(
                        $"Entry '{entry.FullName}' exceeded the per-file size limit while extracting (possible decompression bomb).");
                }

                if (totalWritten > limits.MaxTotalBytes)
                {
                    return Result.Failure(
                        "Archive exceeded the total size limit while extracting (possible decompression bomb).");
                }

                destination.Write(buffer, 0, read);
            }

            return Result.Success();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
