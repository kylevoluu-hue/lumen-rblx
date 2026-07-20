using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lumen.Core.Abstractions;
using Lumen.Core.Common;
using Lumen.Core.Configuration;
using Lumen.Core.Models;
using Lumen.UI.Navigation;

namespace Lumen.UI.ViewModels.Pages;

/// <summary>
/// The Home dashboard. Binds to real data where it is available in this environment (Lumen
/// version, selected account, current profile, mod count) and honestly reports Roblox installation
/// status — including "Windows only" when detection cannot run on the current platform.
/// </summary>
public sealed partial class HomeViewModel : PageViewModel
{
    private readonly ISettingsService _settings;
    private readonly IAccountManager _accounts;
    private readonly IProfileService _profiles;
    private readonly IRobloxInstallationLocator _locator;
    private readonly INavigationService _navigation;

    [ObservableProperty]
    private string _selectedAccount = "No account selected";

    [ObservableProperty]
    private string _currentProfile = "Default";

    [ObservableProperty]
    private string _robloxStatus = "Checking…";

    [ObservableProperty]
    private string _robloxVersion = "—";

    [ObservableProperty]
    private int _enabledModCount;

    [ObservableProperty]
    private string _lastLaunchStatus = "No launches yet.";

    public HomeViewModel(
        ISettingsService settings,
        IAccountManager accounts,
        IProfileService profiles,
        IRobloxInstallationLocator locator,
        INavigationService navigation)
    {
        _settings = settings;
        _accounts = accounts;
        _profiles = profiles;
        _locator = locator;
        _navigation = navigation;
    }

    public override string Title => "Home";

    public override string Description => "Your launch dashboard.";

    public string LumenVersion => LumenInfo.Version;

    public string Disclaimer => LumenInfo.Disclaimer;

    public override async Task InitializeAsync()
    {
        var account = _accounts.Accounts.FirstOrDefault();
        SelectedAccount = account?.DisplayLabel ?? "No account selected";

        var profile = _profiles.Profiles.FirstOrDefault(p => p.IsDefault)
                      ?? _profiles.Profiles.FirstOrDefault();
        CurrentProfile = profile?.Name ?? "Default";

        try
        {
            var installation = await _locator.LocateAsync(RobloxProduct.Player).ConfigureAwait(true);
            RobloxStatus = installation.State switch
            {
                RobloxInstallationState.Healthy => "Installed",
                RobloxInstallationState.UpdateAvailable => "Update available",
                RobloxInstallationState.Broken => "Needs repair",
                RobloxInstallationState.NotDetected => "Not detected",
                _ => "Windows only",
            };
            RobloxVersion = installation.Version ?? "—";
        }
        catch (Exception)
        {
            RobloxStatus = "Unknown";
        }
    }

    [RelayCommand]
    private void Launch() => _navigation.NavigateTo("launch");
}
