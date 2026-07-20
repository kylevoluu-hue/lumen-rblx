using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lumen.Core.Abstractions;

namespace Lumen.UI.ViewModels.Pages;

/// <summary>
/// Graphics presets applied through Roblox's supported FastFlag configuration (texture quality and
/// shadow intensity). Honest by design: some effects are only recommendations Roblox may override,
/// and this composes with any flags set on the FastFlags page.
/// </summary>
public sealed partial class GraphicsViewModel : PageViewModel
{
    private readonly IFastFlagManager _fastFlags;

    [ObservableProperty]
    private string _selectedPreset = "Balanced";

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public GraphicsViewModel(IFastFlagManager fastFlags)
    {
        _fastFlags = fastFlags;
    }

    public override string Title => "Graphics";

    public override string Description =>
        "Apply a graphics preset through Roblox's supported configuration. Restart Roblox for changes to take effect.";

    public ObservableCollection<string> Presets { get; } = new() { "Potato", "Low", "Balanced", "High", "Ultra" };

    [RelayCommand]
    private async Task ApplyAsync()
    {
        var (textureQuality, shadowIntensity) = SelectedPreset switch
        {
            "Potato" => (0, 0),
            "Low" => (0, 25),
            "Balanced" => (2, 75),
            "High" => (3, 100),
            "Ultra" => (3, 100),
            _ => (2, 75),
        };

        var current = await _fastFlags.ReadCurrentAsync().ConfigureAwait(true);
        var flags = current.IsSuccess
            ? new Dictionary<string, string>(current.Value!, StringComparer.Ordinal)
            : new Dictionary<string, string>(StringComparer.Ordinal);

        flags["DFFlagTextureQualityOverrideEnabled"] = "True";
        flags["DFIntTextureQualityOverride"] = textureQuality.ToString(CultureInfo.InvariantCulture);
        flags["FIntRenderShadowIntensity"] = shadowIntensity.ToString(CultureInfo.InvariantCulture);

        var result = await _fastFlags.ApplyAsync(flags).ConfigureAwait(true);
        StatusMessage = result.IsSuccess
            ? $"Applied '{SelectedPreset}'. Restart Roblox to see the change."
            : result.Error!;
    }
}
