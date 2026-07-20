using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lumen.Core.Abstractions;
using Lumen.Core.Models;

namespace Lumen.UI.ViewModels.Pages;

/// <summary>
/// Manages local account labels and honestly surfaces why interactive sign-in is unavailable. It
/// only ever stores nicknames and public metadata — never passwords, cookies, or tokens.
/// </summary>
public sealed partial class AccountsViewModel : PageViewModel
{
    /// <summary>The official Roblox login page. Sign-in happens here, in the user's own browser.</summary>
    public const string RobloxLoginUrl = "https://www.roblox.com/login";

    private readonly IAccountManager _accounts;
    private readonly IProcessLauncher _launcher;
    private readonly IRobloxWebClient _web;

    [ObservableProperty]
    private string _newNickname = string.Empty;

    [ObservableProperty]
    private string _linkUsername = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private Account? _selectedAccount;

    public AccountsViewModel(IAccountManager accounts, IProcessLauncher launcher, IRobloxWebClient web)
    {
        _accounts = accounts;
        _launcher = launcher;
        _web = web;
    }

    public override string Title => "Accounts";

    public override string Description => "Local account labels for organisation. No passwords or cookies are ever stored.";

    public ObservableCollection<Account> Accounts { get; } = new();

    public bool AuthenticationAvailable => _accounts.AuthenticationAvailability.IsAvailable;

    public string AuthenticationExplanation => _accounts.AuthenticationAvailability.Explanation;

    public override async Task InitializeAsync()
    {
        await _accounts.LoadAsync().ConfigureAwait(true);
        RefreshList();
    }

    /// <summary>Links a Roblox account by public username: resolves the user id and public profile.</summary>
    [RelayCommand]
    private async Task LinkAsync()
    {
        if (string.IsNullOrWhiteSpace(LinkUsername))
        {
            StatusMessage = "Enter a Roblox username to link.";
            return;
        }

        StatusMessage = "Looking up Roblox user…";
        var id = await _web.ResolveUsernameAsync(LinkUsername).ConfigureAwait(true);
        if (id.IsFailure)
        {
            StatusMessage = id.Error!;
            return;
        }

        var user = await _web.GetUserAsync(id.Value).ConfigureAwait(true);
        var avatar = await _web.GetAvatarHeadshotUrlAsync(id.Value).ConfigureAwait(true);

        var added = await _accounts.AddAsync(user.IsSuccess ? user.Value!.DisplayName : LinkUsername.Trim()).ConfigureAwait(true);
        if (added.IsFailure)
        {
            StatusMessage = added.Error!;
            return;
        }

        var account = added.Value!;
        account.UserId = id.Value;
        account.Username = user.IsSuccess ? user.Value!.Username : LinkUsername.Trim();
        account.DisplayName = user.IsSuccess ? user.Value!.DisplayName : null;
        account.AvatarUrl = avatar.IsSuccess ? avatar.Value : null;
        await _accounts.UpdateAsync(account).ConfigureAwait(true);

        LinkUsername = string.Empty;
        StatusMessage = $"Linked {account.DisplayLabel} (public profile only).";
        RefreshList();
    }

    /// <summary>Opens Roblox's official login page in the default browser (the supported sign-in method).</summary>
    [RelayCommand]
    private void SignIn()
    {
        var result = _launcher.LaunchUrl(RobloxLoginUrl);
        StatusMessage = result.IsSuccess
            ? "Opened the official Roblox login page in your browser. After signing in there, launches use that session."
            : (result.Error ?? "Could not open the browser.");
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        var result = await _accounts.AddAsync(NewNickname).ConfigureAwait(true);
        if (result.IsSuccess)
        {
            NewNickname = string.Empty;
            StatusMessage = "Account label added.";
            RefreshList();
        }
        else
        {
            StatusMessage = result.Error ?? "Could not add account.";
        }
    }

    [RelayCommand]
    private async Task RemoveAsync(Account? account)
    {
        if (account is null)
        {
            return;
        }

        var result = await _accounts.RemoveAsync(account.Id).ConfigureAwait(true);
        StatusMessage = result.IsSuccess ? "Account removed with its local data." : (result.Error ?? "Could not remove account.");
        RefreshList();
    }

    private void RefreshList()
    {
        Accounts.Clear();
        foreach (var account in _accounts.Accounts)
        {
            Accounts.Add(account);
        }
    }
}
