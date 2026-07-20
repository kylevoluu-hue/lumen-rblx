using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lumen.Core.Abstractions;
using Lumen.Core.Models;

namespace Lumen.UI.ViewModels.Pages;

/// <summary>Shows detected official Roblox installations (read-only). Lumen never modifies Roblox binaries.</summary>
public sealed partial class InstallationsViewModel : PageViewModel
{
    private readonly IRobloxInstallationLocator _locator;

    [ObservableProperty]
    private string _playerStatus = "Checking…";

    [ObservableProperty]
    private string _playerVersion = "—";

    [ObservableProperty]
    private string _playerPath = "—";

    [ObservableProperty]
    private string _studioStatus = "Checking…";

    public InstallationsViewModel(IRobloxInstallationLocator locator)
    {
        _locator = locator;
    }

    public override string Title => "Installations";

    public override string Description =>
        "Official Roblox installations detected on this PC. Lumen only reads these — it never modifies protected Roblox files.";

    public override async Task InitializeAsync()
    {
        var player = await _locator.LocateAsync(RobloxProduct.Player).ConfigureAwait(true);
        PlayerStatus = Describe(player.State);
        PlayerVersion = player.Version ?? "—";
        PlayerPath = player.InstallPath ?? "—";

        var studio = await _locator.LocateAsync(RobloxProduct.Studio).ConfigureAwait(true);
        StudioStatus = Describe(studio.State);
    }

    [RelayCommand]
    private Task RefreshAsync() => InitializeAsync();

    private static string Describe(RobloxInstallationState state) => state switch
    {
        RobloxInstallationState.Healthy => "Installed",
        RobloxInstallationState.UpdateAvailable => "Update available",
        RobloxInstallationState.Broken => "Needs repair",
        RobloxInstallationState.NotDetected => "Not detected",
        _ => "Windows only",
    };
}
