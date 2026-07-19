using System.Text.Json;
using Lumen.Accounts;
using Lumen.Core.Models;
using Lumen.Profiles;
using Lumen.Storage;
using Xunit;

namespace Lumen.Tests;

public class AccountManagerTests
{
    private static AccountManager CreateManager(TempDirectory temp) =>
        new(new JsonFileStore(), temp.AsLumenPaths(), new UnavailableCredentialStore());

    [Fact]
    public void Interactive_sign_in_is_marked_unavailable()
    {
        using var temp = new TempDirectory();
        var manager = CreateManager(temp);
        Assert.False(manager.AuthenticationAvailability.IsAvailable);
        Assert.False(string.IsNullOrWhiteSpace(manager.AuthenticationAvailability.Explanation));
    }

    [Fact]
    public async Task Add_and_remove_account()
    {
        using var temp = new TempDirectory();
        var manager = CreateManager(temp);
        await manager.LoadAsync();

        var added = await manager.AddAsync("Main");
        Assert.True(added.IsSuccess);
        Assert.Single(manager.Accounts);

        var removed = await manager.RemoveAsync(added.Value!.Id);
        Assert.True(removed.IsSuccess);
        Assert.Empty(manager.Accounts);
    }

    [Fact]
    public async Task Accounts_persist_across_reload()
    {
        using var temp = new TempDirectory();
        var first = CreateManager(temp);
        await first.LoadAsync();
        await first.AddAsync("Persisted");

        var second = CreateManager(temp);
        await second.LoadAsync();

        Assert.Single(second.Accounts);
        Assert.Equal("Persisted", second.Accounts[0].Nickname);
    }

    [Fact]
    public async Task RemoveAll_clears_accounts()
    {
        using var temp = new TempDirectory();
        var manager = CreateManager(temp);
        await manager.LoadAsync();
        await manager.AddAsync("A");
        await manager.AddAsync("B");

        await manager.RemoveAllAsync();

        Assert.Empty(manager.Accounts);
    }
}

public class ProfileServiceTests
{
    [Fact]
    public async Task Export_strips_account_linkage()
    {
        using var temp = new TempDirectory();
        var service = new ProfileService(new JsonFileStore(), temp.AsLumenPaths());
        await service.LoadAsync();

        var created = await service.CreateAsync("My Profile");
        created.Value!.AccountId = "secret-account-id";
        await service.SaveAsync(created.Value!);

        var destination = temp.Sub("export.lumenprofile");
        var export = await service.ExportAsync(created.Value!.Id, destination);

        Assert.True(export.IsSuccess);
        var json = await File.ReadAllTextAsync(destination);
        Assert.DoesNotContain("secret-account-id", json);
    }

    [Fact]
    public async Task Import_assigns_new_id_and_strips_account()
    {
        using var temp = new TempDirectory();
        var service = new ProfileService(new JsonFileStore(), temp.AsLumenPaths());
        await service.LoadAsync();

        var incoming = new LumenProfile { Id = "original-id", Name = "Imported", AccountId = "acc" };
        var source = temp.Sub("incoming.lumenprofile");
        await File.WriteAllTextAsync(source, JsonSerializer.Serialize(incoming, JsonFileStore.SerializerOptions));

        var result = await service.ImportAsync(source);

        Assert.True(result.IsSuccess);
        Assert.NotEqual("original-id", result.Value!.Id);
        Assert.Null(result.Value!.AccountId);
    }
}
