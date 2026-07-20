using System.Net.Http;
using Lumen.Roblox;
using Lumen.Security;
using Lumen.Storage;
using Xunit;

namespace Lumen.Tests;

public class RecentExperienceStoreTests
{
    [Fact]
    public async Task Records_dedupes_and_persists()
    {
        using var temp = new TempDirectory();
        var paths = temp.AsLumenPaths();

        var store = new RecentExperienceStore(new JsonFileStore(), paths);
        await store.LoadAsync();
        await store.RecordAsync(1, "Game A");
        await store.RecordAsync(2, "Game B");
        await store.RecordAsync(1, "Game A again"); // de-dupes and moves to front

        Assert.Equal(2, store.Recent.Count);
        Assert.Equal(1, store.Recent[0].PlaceId);

        var reloaded = new RecentExperienceStore(new JsonFileStore(), paths);
        await reloaded.LoadAsync();
        Assert.Equal(2, reloaded.Recent.Count);
    }
}

public class RobloxWebClientTests
{
    [Fact]
    public async Task GetImage_rejects_non_allowlisted_host()
    {
        var client = new RobloxWebClient(new HttpClient(), new UrlValidator(Array.Empty<string>()));
        var result = await client.GetImageAsync("https://evil.example.com/avatar.png");
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task ResolveUsername_rejects_empty_input()
    {
        var client = new RobloxWebClient(new HttpClient(), UrlValidator.CreateDefault());
        var result = await client.ResolveUsernameAsync("   ");
        Assert.True(result.IsFailure);
    }
}
