using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lumen.Core.Abstractions;
using Lumen.Core.Models;

namespace Lumen.UI.ViewModels.Pages;

/// <summary>Create, delete, and set a default among Lumen profiles. Backed by the profile service.</summary>
public sealed partial class ProfilesViewModel : PageViewModel
{
    private readonly IProfileService _profiles;

    [ObservableProperty]
    private string _newName = string.Empty;

    [ObservableProperty]
    private LumenProfile? _selectedProfile;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public ProfilesViewModel(IProfileService profiles)
    {
        _profiles = profiles;
    }

    public override string Title => "Profiles";

    public override string Description => "Named bundles of launch preferences. Exported profiles never contain account secrets.";

    public ObservableCollection<LumenProfile> Profiles { get; } = new();

    public override async Task InitializeAsync()
    {
        await _profiles.LoadAsync().ConfigureAwait(true);
        Refresh();
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        var result = await _profiles.CreateAsync(NewName).ConfigureAwait(true);
        if (result.IsSuccess)
        {
            NewName = string.Empty;
            StatusMessage = "Profile created.";
            Refresh();
        }
        else
        {
            StatusMessage = result.Error!;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(LumenProfile? profile)
    {
        if (profile is null)
        {
            return;
        }

        var result = await _profiles.DeleteAsync(profile.Id).ConfigureAwait(true);
        StatusMessage = result.IsSuccess ? "Profile deleted." : result.Error!;
        Refresh();
    }

    [RelayCommand]
    private async Task SetDefaultAsync(LumenProfile? profile)
    {
        if (profile is null)
        {
            return;
        }

        foreach (var existing in _profiles.Profiles.ToList())
        {
            existing.IsDefault = existing.Id == profile.Id;
            await _profiles.SaveAsync(existing).ConfigureAwait(true);
        }

        StatusMessage = $"'{profile.Name}' is now the default profile.";
        Refresh();
    }

    private void Refresh()
    {
        Profiles.Clear();
        foreach (var profile in _profiles.Profiles)
        {
            Profiles.Add(profile);
        }
    }
}
