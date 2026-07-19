using Lumen.Core.Common;

namespace Lumen.Security;

/// <summary>
/// Validation for untrusted relative paths coming from mod manifests, archive entries, and
/// user input. Blocks path traversal, absolute/rooted paths, drive and UNC prefixes,
/// alternate-data-stream colons, and control characters — the building blocks of zip-slip and
/// directory-escape attacks.
/// </summary>
public static class PathSafety
{
    /// <summary>
    /// True only if <paramref name="relativePath"/> is a safe, contained, relative path
    /// (no traversal, not absolute/rooted, no drive letter, no UNC, no ':' or control chars).
    /// </summary>
    public static bool IsSafeRelativePath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        // .NET treats '\' as a separator only on Windows; normalize so the checks below are
        // identical on every platform (important: this code is tested on Linux, ships on Windows).
        var normalized = relativePath.Replace('\\', '/');

        // Absolute (POSIX) or UNC ("//server/share" after normalization).
        if (normalized.StartsWith('/'))
        {
            return false;
        }

        // Rooted per the current OS, or an explicit Windows drive prefix ("C:...").
        if (Path.IsPathRooted(relativePath))
        {
            return false;
        }

        if (normalized.Length >= 2 && char.IsLetter(normalized[0]) && normalized[1] == ':')
        {
            return false;
        }

        foreach (var ch in normalized)
        {
            // Control characters and ':' (Windows alternate data streams) are never allowed.
            if (char.IsControl(ch) || ch == ':')
            {
                return false;
            }
        }

        foreach (var segment in normalized.Split('/'))
        {
            if (segment == "..")
            {
                return false;
            }

            // Trailing dots/spaces are stripped by Windows and can be used to smuggle names.
            if (segment.Length > 0 && (segment[^1] == ' ' || segment[^1] == '.') && segment != ".")
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Validates a relative path and resolves it to an absolute path guaranteed to live inside
    /// <paramref name="root"/>. Returns a failure (never an escaping path) if the input is unsafe.
    /// The containment check is defence-in-depth on top of <see cref="IsSafeRelativePath"/>.
    /// </summary>
    public static Result<string> ResolveWithinRoot(string root, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            return Result.Failure<string>("Root directory must be provided.");
        }

        if (!IsSafeRelativePath(relativePath))
        {
            return Result.Failure<string>($"Unsafe path rejected: '{relativePath}'.");
        }

        var fullRoot = Path.GetFullPath(root);
        var combined = Path.GetFullPath(Path.Combine(fullRoot, relativePath));

        var rootWithSeparator = fullRoot.EndsWith(Path.DirectorySeparatorChar)
            ? fullRoot
            : fullRoot + Path.DirectorySeparatorChar;

        if (combined != fullRoot && !combined.StartsWith(rootWithSeparator, StringComparison.Ordinal))
        {
            return Result.Failure<string>($"Path escapes the target directory: '{relativePath}'.");
        }

        return Result.Success(combined);
    }
}
