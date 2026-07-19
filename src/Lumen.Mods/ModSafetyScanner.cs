using System.Text.RegularExpressions;
using Lumen.Core.Abstractions;
using Lumen.Core.Models;
using Lumen.Security;

namespace Lumen.Mods;

/// <summary>
/// Inspects mod package contents for disallowed material. Cosmetic mods are data only, so any
/// executable, script, registry file, path-traversal entry, symbolic link, or hidden/double
/// extension is a blocking finding. Unrecognised file types are surfaced as warnings. A passing
/// scan reduces risk but is explicitly not a guarantee of absolute safety.
/// </summary>
public sealed partial class ModSafetyScanner : IModSafetyScanner
{
    // Executables, scripts, installers, shortcuts, registry files, and native libraries.
    private static readonly HashSet<string> DangerousExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".dll", ".bat", ".cmd", ".com", ".scr", ".pif", ".msi", ".msp", ".cpl", ".msc",
        ".ps1", ".psm1", ".psd1", ".vbs", ".vbe", ".js", ".jse", ".wsf", ".wsh", ".hta", ".lnk",
        ".reg", ".sys", ".drv", ".ocx", ".gadget", ".application", ".jar", ".cab", ".sh", ".bash",
        ".zsh", ".py", ".pyc", ".rb", ".pl", ".php", ".apk", ".deb", ".dmg", ".iso", ".img",
        ".so", ".dylib", ".bin", ".elf", ".class", ".o", ".a",
    };

    // Cosmetic asset types Lumen expects inside a mod package.
    private static readonly HashSet<string> SafeExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp", ".gif", ".bmp", ".ico", ".cur", ".ani", ".svg",
        ".tga", ".dds", ".hdr", ".exr", ".ktx", ".ktx2", ".tiff", ".tif",
        ".ttf", ".otf", ".woff", ".woff2",
        ".wav", ".mp3", ".ogg", ".flac", ".aac", ".m4a",
        ".json", ".txt", ".md", ".markdown",
    };

    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".json", ".txt", ".md", ".markdown", ".xml", ".cfg", ".ini",
    };

    private const long MaxTextScanBytes = 2 * 1024 * 1024;

    public ModScanReport ScanEntries(IEnumerable<string> relativePaths)
    {
        ArgumentNullException.ThrowIfNull(relativePaths);

        var report = new ModScanReport();
        foreach (var path in relativePaths)
        {
            ScanSinglePath(report, path);
        }

        return report;
    }

    public async Task<ModScanReport> ScanDirectoryAsync(string directory, CancellationToken cancellationToken = default)
    {
        var report = new ModScanReport();

        if (!Directory.Exists(directory))
        {
            report.Add(ModScanSeverity.Blocked, directory, "Package directory does not exist.");
            return report;
        }

        var root = Path.GetFullPath(directory);

        // Symbolic links / reparse points can redirect writes outside the package; reject them.
        foreach (var entry in Directory.EnumerateFileSystemEntries(root, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relative = Path.GetRelativePath(root, entry);
            var linkTarget = File.Exists(entry) ? new FileInfo(entry).LinkTarget : new DirectoryInfo(entry).LinkTarget;
            if (linkTarget is not null)
            {
                report.Add(ModScanSeverity.Blocked, relative, "Symbolic links are not allowed in mod packages.");
                continue;
            }

            if (File.Exists(entry))
            {
                ScanSinglePath(report, relative);
                await ScanFileContentAsync(report, entry, relative, cancellationToken).ConfigureAwait(false);
            }
        }

        return report;
    }

    private static void ScanSinglePath(ModScanReport report, string relativePath)
    {
        if (!PathSafety.IsSafeRelativePath(relativePath))
        {
            report.Add(ModScanSeverity.Blocked, relativePath, "Unsafe path (traversal, absolute, or invalid characters).");
            return;
        }

        var fileName = Path.GetFileName(relativePath.Replace('\\', '/'));
        var segments = fileName.Split('.');

        // Double / hidden extension: a dangerous type disguised behind another (e.g. cursor.exe.png).
        if (segments.Length > 2)
        {
            for (var i = 1; i < segments.Length - 1; i++)
            {
                if (DangerousExtensions.Contains("." + segments[i]))
                {
                    report.Add(ModScanSeverity.Blocked, relativePath, $"Hidden/double extension '.{segments[i]}'.");
                }
            }
        }

        var extension = Path.GetExtension(fileName);
        if (DangerousExtensions.Contains(extension))
        {
            report.Add(ModScanSeverity.Blocked, relativePath, $"Disallowed file type '{extension}'.");
        }
        else if (extension.Length == 0)
        {
            report.Add(ModScanSeverity.Warning, relativePath, "File has no extension.");
        }
        else if (!SafeExtensions.Contains(extension))
        {
            report.Add(ModScanSeverity.Warning, relativePath, $"Unrecognised file type '{extension}'.");
        }
    }

    private async Task ScanFileContentAsync(
        ModScanReport report, string fullPath, string relativePath, CancellationToken cancellationToken)
    {
        if (!TextExtensions.Contains(Path.GetExtension(fullPath)))
        {
            return;
        }

        try
        {
            var info = new FileInfo(fullPath);
            if (info.Length > MaxTextScanBytes)
            {
                return;
            }

            var content = await File.ReadAllTextAsync(fullPath, cancellationToken).ConfigureAwait(false);
            if (EmbeddedUrlRegex().IsMatch(content))
            {
                report.Add(ModScanSeverity.Warning, relativePath,
                    "Contains an embedded URL; review before trusting (mods should not fetch remote content).");
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            report.Add(ModScanSeverity.Warning, relativePath, "Could not read file for content inspection.");
        }
    }

    [GeneratedRegex(@"https?://", RegexOptions.IgnoreCase)]
    private static partial Regex EmbeddedUrlRegex();
}
