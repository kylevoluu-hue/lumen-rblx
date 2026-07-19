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
    private readonly IAccountManager _accounts;

    [ObservableProperty]
    private string _newNickname = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private Account? _selectedAccount;

    public AccountsViewModel(IAccountManager accounts)
    {
        _accounts = accounts;
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
