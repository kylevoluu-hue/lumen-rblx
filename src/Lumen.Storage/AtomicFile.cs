namespace Lumen.Storage;

/// <summary>
/// Atomic file writes: content is written to a temporary file on the same volume and then moved
/// into place, so a crash mid-write can never leave a half-written (corrupt) file. An optional
/// backup of the previous contents is taken first to support rollback.
/// </summary>
public static class AtomicFile
{
    public static async Task WriteAllTextAsync(
        string filePath, string contents, bool backup = true, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath))!;
        Directory.CreateDirectory(directory);

        if (backup && File.Exists(filePath))
        {
            File.Copy(filePath, filePath + ".bak", overwrite: true);
        }

        var tempPath = filePath + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            await File.WriteAllTextAsync(tempPath, contents, cancellationToken).ConfigureAwait(false);
            // Move is atomic on the same volume; overwrite replaces the previous file in one step.
            File.Move(tempPath, filePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                TryDelete(tempPath);
            }
        }
    }

    /// <summary>Restores the last <c>.bak</c> written for a file, if one exists.</summary>
    public static bool TryRestoreBackup(string filePath)
    {
        var backupPath = filePath + ".bak";
        if (!File.Exists(backupPath))
        {
            return false;
        }

        File.Copy(backupPath, filePath, overwrite: true);
        return true;
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // Best-effort cleanup of the temp file; ignore.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
