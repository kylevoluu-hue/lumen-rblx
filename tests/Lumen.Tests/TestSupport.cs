using Lumen.Core.Common;

namespace Lumen.Tests;

/// <summary>A disposable temporary directory for tests that touch the filesystem.</summary>
public sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "lumen-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string Sub(params string[] parts)
    {
        var combined = Path;
        foreach (var part in parts)
        {
            combined = System.IO.Path.Combine(combined, part);
        }

        return combined;
    }

    /// <summary>A real <see cref="LumenPaths"/> rooted in this temp directory.</summary>
    public LumenPaths AsLumenPaths()
    {
        var paths = new LumenPaths(Path, isPortable: true);
        paths.EnsureCreated();
        return paths;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch (IOException)
        {
        }
    }
}
