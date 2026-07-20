using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Lumen.Core.Configuration;
using Lumen.UI.Theming;

namespace Lumen.UI.ViewModels.Pages;

/// <summary>A named accent colour choice (framework-agnostic; the view renders the swatch from Hex).</summary>
public sealed record AccentOption(string Name, string Hex);

/// <summary>
/// Lumen appearance settings. The accent colour is applied live and persisted. All settings are
/// local and cosmetic.
/// </summary>
public sealed partial class SettingsViewModel : PageViewModel
{
    private readonly ISettingsService _settings;
    private readonly IThemeApplier _themeApplier;
    private bool _loaded;

    [ObservableProperty]
    private AccentOption? _selectedAccent;

    [ObservableProperty]
    private bool _compactMode;

    public SettingsViewModel(ISettingsService settings, IThemeApplier themeApplier)
    {
        _settings = settings;
        _themeApplier = themeApplier;
    }

    public override string Title => "Lumen Settings";

    public override string Description => "Personalise Lumen's appearance. Changes are saved locally.";

    public ObservableCollection<AccentOption> AccentOptions { get; } = new()
    {
        new AccentOption("Lumen Blue", "#6C8CFF"),
        new AccentOption("Violet", "#8A6CFF"),
        new AccentOption("Emerald", "#3FB984"),
        new AccentOption("Amber", "#E9A23B"),
        new AccentOption("Rose", "#F0668A"),
        new AccentOption("Cyan", "#39B8C6"),
    };

    public override Task InitializeAsync()
    {
        var currentHex = _settings.Current.Appearance.AccentColor;
        SelectedAccent = AccentOptions.FirstOrDefault(a => string.Equals(a.Hex, currentHex, StringComparison.OrdinalIgnoreCase))
                         ?? AccentOptions[0];
        CompactMode = _settings.Current.Appearance.CompactMode;
        _loaded = true;
        return Task.CompletedTask;
    }

    partial void OnSelectedAccentChanged(AccentOption? value)
    {
        if (!_loaded || value is null)
        {
            return;
        }

        _themeApplier.ApplyAccent(value.Hex);
        _ = _settings.UpdateAsync(s => s.Appearance.AccentColor = value.Hex);
    }

    partial void OnCompactModeChanged(bool value)
    {
        if (!_loaded)
        {
            return;
        }

        _ = _settings.UpdateAsync(s => s.Appearance.CompactMode = value);
    }
}
