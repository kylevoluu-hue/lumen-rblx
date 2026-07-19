using Lumen.Core.Configuration;
using Lumen.Storage;
using Xunit;

namespace Lumen.Tests;

public sealed class SampleDoc
{
    public string Name { get; set; } = string.Empty;

    public int Count { get; set; }
}

public class AtomicFileTests
{
    [Fact]
    public async Task Write_is_atomic_and_creates_backup()
    {
        using var temp = new TempDirectory();
        var file = temp.Sub("data.txt");

        await AtomicFile.WriteAllTextAsync(file, "v1");
        Assert.Equal("v1", await File.ReadAllTextAsync(file));

        await AtomicFile.WriteAllTextAsync(file, "v2");
        Assert.Equal("v2", await File.ReadAllTextAsync(file));
        Assert.Equal("v1", await File.ReadAllTextAsync(file + ".bak"));

        Assert.True(AtomicFile.TryRestoreBackup(file));
        Assert.Equal("v1", await File.ReadAllTextAsync(file));
    }
}

public class JsonFileStoreTests
{
    [Fact]
    public async Task Load_missing_file_creates_default()
    {
        using var temp = new TempDirectory();
        var store = new JsonFileStore();
        var file = temp.Sub("doc.json");

        var result = await store.LoadAsync(file, () => new SampleDoc { Name = "default" });

        Assert.True(result.IsSuccess);
        Assert.Equal("default", result.Value!.Name);
        Assert.True(File.Exists(file));
    }

    [Fact]
    public async Task Save_then_load_roundtrips()
    {
        using var temp = new TempDirectory();
        var store = new JsonFileStore();
        var file = temp.Sub("doc.json");

        await store.SaveAsync(file, new SampleDoc { Name = "hi", Count = 7 });
        var loaded = await store.LoadAsync(file, () => new SampleDoc());

        Assert.Equal("hi", loaded.Value!.Name);
        Assert.Equal(7, loaded.Value!.Count);
    }

    [Fact]
    public async Task Corrupt_file_is_quarantined_and_recovered()
    {
        using var temp = new TempDirectory();
        var store = new JsonFileStore();
        var file = temp.Sub("doc.json");
        await File.WriteAllTextAsync(file, "{ this is not valid json ");

        var result = await store.LoadAsync(file, () => new SampleDoc { Name = "fallback" });

        Assert.True(result.IsSuccess);
        Assert.Equal("fallback", result.Value!.Name);
        Assert.Contains(Directory.GetFiles(temp.Path), f => f.Contains(".corrupt-", StringComparison.Ordinal));
    }
}

public class SettingsServiceTests
{
    [Fact]
    public async Task Defaults_are_privacy_safe()
    {
        using var temp = new TempDirectory();
        var service = new SettingsService(new JsonFileStore(), temp.AsLumenPaths());

        await service.LoadAsync();

        Assert.False(service.Current.FirstLaunchCompleted);
        Assert.False(service.Current.Privacy.OptionalLoggingEnabled);
        Assert.False(service.Current.Privacy.DiscordRichPresenceEnabled);
        Assert.False(service.Current.Privacy.StartupLaunchEnabled);
        Assert.False(service.Current.Privacy.ThirdPartyExtensionsEnabled);
    }

    [Fact]
    public async Task Update_persists_across_reload()
    {
        using var temp = new TempDirectory();
        var paths = temp.AsLumenPaths();
        var store = new JsonFileStore();

        var first = new SettingsService(store, paths);
        await first.LoadAsync();
        await first.UpdateAsync(s => s.Appearance.AccentColor = "#FF3366");

        var second = new SettingsService(store, paths);
        await second.LoadAsync();

        Assert.Equal("#FF3366", second.Current.Appearance.AccentColor);
    }
}
