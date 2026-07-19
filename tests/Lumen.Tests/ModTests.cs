using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Lumen.Core.Models;
using Lumen.Mods;
using Lumen.Security;
using Lumen.Storage;
using Xunit;

namespace Lumen.Tests;

public class ModSafetyScannerTests
{
    private readonly ModSafetyScanner _scanner = new();

    [Fact]
    public void Accepts_cosmetic_assets()
    {
        var report = _scanner.ScanEntries(new[] { "manifest.json", "assets/cursor.png", "sounds/click.wav" });
        Assert.True(report.IsSafe);
    }

    [Theory]
    [InlineData("payload.exe")]
    [InlineData("hook.dll")]
    [InlineData("script.ps1")]
    [InlineData("installer.msi")]
    [InlineData("shortcut.lnk")]
    [InlineData("keys.reg")]
    public void Blocks_executable_and_script_types(string path)
    {
        var report = _scanner.ScanEntries(new[] { path });
        Assert.False(report.IsSafe);
    }

    [Fact]
    public void Blocks_double_extension()
    {
        var report = _scanner.ScanEntries(new[] { "cursor.exe.png" });
        Assert.False(report.IsSafe);
    }

    [Fact]
    public void Blocks_path_traversal()
    {
        var report = _scanner.ScanEntries(new[] { "../../evil.png" });
        Assert.False(report.IsSafe);
    }

    [Fact]
    public void Warns_but_allows_unknown_type()
    {
        var report = _scanner.ScanEntries(new[] { "notes.xyz" });
        Assert.True(report.IsSafe);
        Assert.True(report.HasWarnings);
    }
}

public class ModManifestReaderTests
{
    [Fact]
    public void Accepts_valid_manifest()
    {
        var manifest = new ModManifest { Id = "author.mod-name", Name = "Example", Author = "Author", Version = "1.0.0" };
        Assert.True(ModManifestReader.Validate(manifest).IsSuccess);
    }

    [Theory]
    [InlineData("", "1.0.0")]          // missing id
    [InlineData("author.mod", "1.0")]  // bad version
    public void Rejects_invalid_manifest(string id, string version)
    {
        var manifest = new ModManifest { Id = id, Name = "X", Author = "A", Version = version };
        Assert.True(ModManifestReader.Validate(manifest).IsFailure);
    }

    [Fact]
    public void Rejects_unsupported_format_version()
    {
        var manifest = new ModManifest { FormatVersion = 2, Id = "a.b", Name = "X", Author = "A", Version = "1.0.0" };
        Assert.True(ModManifestReader.Validate(manifest).IsFailure);
    }
}

public class ModPackageReaderTests
{
    [Fact]
    public async Task Opens_a_valid_package_and_verifies_hashes()
    {
        using var temp = new TempDirectory();
        var icon = new byte[] { 1, 2, 3, 4 };
        var manifest = new ModManifest { Id = "test.mod", Name = "Test", Author = "A", Version = "1.0.0", Category = ModCategory.Cursor };
        manifest.Hashes["assets/icon.png"] = FileHashing.ComputeSha256(icon);

        var package = BuildPackage(temp, "good.lumenmod", manifest, new() { ["assets/icon.png"] = icon });
        var reader = new ModPackageReader(new ModSafetyScanner());

        var result = await reader.OpenAsync(package, temp.Sub("stage-good"));

        Assert.True(result.IsSuccess);
        Assert.Equal("test.mod", result.Value!.Manifest.Id);
    }

    [Fact]
    public async Task Rejects_package_containing_executable_and_cleans_staging()
    {
        using var temp = new TempDirectory();
        var manifest = new ModManifest { Id = "bad.mod", Name = "Bad", Author = "A", Version = "1.0.0" };
        var package = BuildPackage(temp, "bad.lumenmod", manifest, new() { ["payload.exe"] = new byte[] { 0 } });
        var reader = new ModPackageReader(new ModSafetyScanner());

        var staging = temp.Sub("stage-bad");
        var result = await reader.OpenAsync(package, staging);

        Assert.True(result.IsFailure);
        Assert.False(Directory.Exists(staging));
    }

    [Fact]
    public async Task Rejects_package_with_bad_hash()
    {
        using var temp = new TempDirectory();
        var manifest = new ModManifest { Id = "hash.mod", Name = "H", Author = "A", Version = "1.0.0" };
        manifest.Hashes["assets/icon.png"] = new string('0', 64); // wrong hash
        var package = BuildPackage(temp, "hash.lumenmod", manifest, new() { ["assets/icon.png"] = new byte[] { 9 } });
        var reader = new ModPackageReader(new ModSafetyScanner());

        var result = await reader.OpenAsync(package, temp.Sub("stage-hash"));

        Assert.True(result.IsFailure);
    }

    private static string BuildPackage(
        TempDirectory temp, string name, ModManifest manifest, Dictionary<string, byte[]> files)
    {
        var path = temp.Sub(name);
        using var stream = new FileStream(path, FileMode.Create);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);

        var json = JsonSerializer.Serialize(manifest, JsonFileStore.SerializerOptions);
        WriteEntry(archive, "manifest.json", Encoding.UTF8.GetBytes(json));
        foreach (var (entryName, data) in files)
        {
            WriteEntry(archive, entryName, data);
        }

        return path;
    }

    private static void WriteEntry(ZipArchive archive, string entryName, byte[] data)
    {
        var entry = archive.CreateEntry(entryName);
        using var entryStream = entry.Open();
        entryStream.Write(data, 0, data.Length);
    }
}
