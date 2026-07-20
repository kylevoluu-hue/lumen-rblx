using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lumen.Core.Common;

namespace Lumen.UI.ViewModels.Pages;

/// <summary>
/// The Privacy Center. Explains, in plain terms, exactly what Lumen stores locally and where, and
/// gives the user direct control (open the data folder, and — in later phases — clear/export).
/// Lumen never sells data and never uploads launch history, keystrokes, or overlay data.
/// </summary>
public sealed partial class PrivacyViewModel : PageViewModel
{
    private readonly ILumenPaths _paths;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public PrivacyViewModel(ILumenPaths paths)
    {
        _paths = paths;

        StoredItems = new ObservableCollection<string>
        {
            $"Settings — {Path.Combine("config", "settings.json")} (no secrets)",
            $"Account labels — {Path.Combine("accounts", "accounts.json")} (nicknames and public ids only)",
            $"Profiles — {Path.Combine("profiles", "profiles.json")}",
            "Recent experiences — recent.json (what you launch via Lumen)",
            $"Logs — logs/ (local, redacted, opt-in)",
            $"Installed mods — mods/",
            $"Backups — backups/",
        };
    }

    public override string Title => "Privacy";

    public override string Description => "Everything Lumen stores lives on your device. Nothing is uploaded automatically.";

    public string DataFolder => _paths.Root;

    public ObservableCollection<string> StoredItems { get; }

    [RelayCommand]
    private void OpenDataFolder()
    {
        try
        {
            _paths.EnsureCreated();
            Process.Start(new ProcessStartInfo
            {
                FileName = _paths.Root,
                UseShellExecute = true,
            });
            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not open the data folder: {ex.Message}";
        }
    }
}
