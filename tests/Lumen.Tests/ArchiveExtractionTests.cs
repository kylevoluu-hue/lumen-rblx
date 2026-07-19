using System.IO.Compression;
using Lumen.Security;
using Xunit;

namespace Lumen.Tests;

public class ArchiveExtractionTests
{
    [Fact]
    public void Extracts_a_valid_archive()
    {
        using var temp = new TempDirectory();
        var zip = CreateZip(temp, "good.zip", archive =>
        {
            AddEntry(archive, "manifest.json", "{}"u8.ToArray());
            AddEntry(archive, "assets/icon.png", new byte[] { 1, 2, 3, 4 });
        });

        var dest = temp.Sub("out");
        var result = SafeArchiveExtractor.ExtractZip(zip, dest);

        Assert.True(result.IsSuccess);
        Assert.True(File.Exists(Path.Combine(dest, "manifest.json")));
        Assert.True(File.Exists(Path.Combine(dest, "assets", "icon.png")));
    }

    [Fact]
    public void Rejects_zip_slip_entry()
    {
        using var temp = new TempDirectory();
        var zip = CreateZip(temp, "slip.zip", archive =>
            AddEntry(archive, "../escaped.txt", "pwned"u8.ToArray()));

        var dest = temp.Sub("out");
        var result = SafeArchiveExtractor.ExtractZip(zip, dest);

        Assert.True(result.IsFailure);
        // The traversal target must never be written outside the destination.
        Assert.False(File.Exists(temp.Sub("escaped.txt")));
    }

    [Fact]
    public void Rejects_decompression_bomb_by_size()
    {
        using var temp = new TempDirectory();
        var zip = CreateZip(temp, "bomb.zip", archive =>
            AddEntry(archive, "big.bin", new byte[2 * 1024 * 1024])); // 2 MB of highly compressible zeros

        var limits = new ArchiveLimits(MaxEntryBytes: 1024, MaxTotalBytes: 4096, RatioCheckFloorBytes: 512);
        var result = SafeArchiveExtractor.ExtractZip(zip, temp.Sub("out"), limits);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Rejects_too_many_entries()
    {
        using var temp = new TempDirectory();
        var zip = CreateZip(temp, "many.zip", archive =>
        {
            AddEntry(archive, "a.txt", "a"u8.ToArray());
            AddEntry(archive, "b.txt", "b"u8.ToArray());
            AddEntry(archive, "c.txt", "c"u8.ToArray());
        });

        var limits = new ArchiveLimits(MaxEntries: 2);
        var result = SafeArchiveExtractor.ExtractZip(zip, temp.Sub("out"), limits);

        Assert.True(result.IsFailure);
    }

    private static string CreateZip(TempDirectory temp, string name, Action<ZipArchive> build)
    {
        var path = temp.Sub(name);
        using var stream = new FileStream(path, FileMode.Create);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
        build(archive);
        return path;
    }

    private static void AddEntry(ZipArchive archive, string entryName, byte[] data)
    {
        var entry = archive.CreateEntry(entryName);
        using var entryStream = entry.Open();
        entryStream.Write(data, 0, data.Length);
    }
}
