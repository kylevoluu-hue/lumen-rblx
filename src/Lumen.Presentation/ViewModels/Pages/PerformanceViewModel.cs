using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lumen.Core.Abstractions;

namespace Lumen.UI.ViewModels.Pages;

/// <summary>
/// A friendly frame-rate cap, applied through the safe FastFlag mechanism (it composes with any
/// flags set on the FastFlags page). Honest by design: no placebo RAM cleaners, ping boosters,
/// registry edits, or security toggles.
/// </summary>
public sealed partial class PerformanceViewModel : PageViewModel
{
    private const string FpsFlag = "DFIntTaskSchedulerTargetFps";

    private readonly IFastFlagManager _fastFlags;

    [ObservableProperty]
    private string _selectedFps = "60";

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public PerformanceViewModel(IFastFlagManager fastFlags)
    {
        _fastFlags = fastFlags;
    }

    public override string Title => "Performance";

    public override string Description =>
        "Set a frame-rate cap. Lumen applies it through Roblox's supported configuration file — it never patches memory or changes system settings.";

    public ObservableCollection<string> FpsOptions { get; } = new()
    {
        "30", "60", "75", "120", "144", "165", "240", "360", "Unlimited",
    };

    [RelayCommand]
    private async Task ApplyFpsAsync()
    {
        var value = SelectedFps == "Unlimited" ? "9999" : SelectedFps;

        // Merge with any existing allowlisted flags so we don't clobber the FastFlags page.
        var current = await _fastFlags.ReadCurrentAsync().ConfigureAwait(true);
        var flags = current.IsSuccess
            ? new Dictionary<string, string>(current.Value!, StringComparer.Ordinal)
            : new Dictionary<string, string>(StringComparer.Ordinal);
        flags[FpsFlag] = value;

        var result = await _fastFlags.ApplyAsync(flags).ConfigureAwait(true);
        StatusMessage = result.IsSuccess
            ? $"FPS cap set to {SelectedFps}. Restart Roblox for it to take effect."
            : result.Error!;
    }
}
