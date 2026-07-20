using Lumen.Core.Models;
using Lumen.Mods;
using Lumen.Roblox;
using Lumen.Storage;
using Xunit;

namespace Lumen.Tests;

public class ModApplicatorTests
{
    private static ModApplicator Create(TempDirectory temp) =>
        new(new RobloxInstallationLocator(), temp.AsLumenPaths(), new JsonFileStore());

    private static ModPackage PackageWith(string staging, string relativeContentFile, string contents)
    {
        var full = Path.Combine(staging, "Content", relativeContentFile);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, contents);
        return new ModPackage(
            new ModManifest { Id = "a.b", Name = "M", Author = "A", Version = "1.0.0" },
            new ModScanReport(),
            staging);
    }

    [Fact]
    public async Task Applies_content_backs_up_original_and_restores()
    {
        using var temp = new TempDirectory();
        var applicator = Create(temp);
        var package = PackageWith(temp.Sub("staging"), Path.Combine("textures", "cursor.png"), "MODDED");

        var contentRoot = temp.Sub("contentRoot");
        Directory.CreateDirectory(Path.Combine(contentRoot, "textures"));
        var target = Path.Combine(contentRoot, "textures", "cursor.png");
        await File.WriteAllTextAsync(target, "ORIGINAL");

        var applied = await applicator.ApplyToContentRootAsync(package, contentRoot);
        Assert.True(applied.IsSuccess);
        Assert.Equal(1, applied.Value);
        Assert.Equal("MODDED", await File.ReadAllTextAsync(target));

        var restored = await applicator.RestoreFromContentRootAsync(contentRoot);
        Assert.True(restored.IsSuccess);
        Assert.Equal("ORIGINAL", await File.ReadAllTextAsync(target));
    }

    [Fact]
    public async Task Removes_added_file_on_restore_when_no_original()
    {
        using var temp = new TempDirectory();
        var applicator = Create(temp);
        var package = PackageWith(temp.Sub("staging"), Path.Combine("fonts", "custom.ttf"), "FONT");

        var contentRoot = temp.Sub("contentRoot");
        var target = Path.Combine(contentRoot, "fonts", "custom.ttf");

        await applicator.ApplyToContentRootAsync(package, contentRoot);
        Assert.True(File.Exists(target));

        await applicator.RestoreFromContentRootAsync(contentRoot);
        Assert.False(File.Exists(target));
    }

    [Fact]
    public async Task Rejects_package_without_content_folder()
    {
        using var temp = new TempDirectory();
        var applicator = Create(temp);
        var staging = temp.Sub("staging");
        Directory.CreateDirectory(staging);
        var package = new ModPackage(
            new ModManifest { Id = "a.b", Name = "M", Author = "A", Version = "1.0.0" },
            new ModScanReport(), staging);

        var result = await applicator.ApplyToContentRootAsync(package, temp.Sub("contentRoot"));
        Assert.True(result.IsFailure);
    }
}
