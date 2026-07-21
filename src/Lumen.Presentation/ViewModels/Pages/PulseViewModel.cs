using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lumen.Core.Abstractions;
using Lumen.Core.Models;

namespace Lumen.UI.ViewModels.Pages;

/// <summary>
/// Lumen Pulse: a local health check of the Lumen and Roblox setup. Runs on the device and is never
/// uploaded automatically.
/// </summary>
public sealed partial class PulseViewModel : PageViewModel
{
    private readonly IPulseService _pulse;

    [ObservableProperty]
    private string _overallLabel = "Not run yet";

    [ObservableProperty]
    private bool _isRunning;

    public PulseViewModel(IPulseService pulse)
    {
        _pulse = pulse;
    }

    public override string Title => "Lumen Pulse";

    public override string Description =>
        "A local health check of your Lumen and Roblox setup. It runs on your device and nothing is uploaded.";

    public ObservableCollection<PulseCheck> Checks { get; } = new();

    public override Task InitializeAsync() => RunAsync();

    [RelayCommand]
    private async Task RunAsync()
    {
        IsRunning = true;
        try
        {
            var report = await _pulse.RunAsync().ConfigureAwait(true);
            Checks.Clear();
            foreach (var check in report.Checks)
            {
                Checks.Add(check);
            }

            OverallLabel = report.Overall switch
            {
                PulseSeverity.Ok => "All good",
                PulseSeverity.Info => "All good, with notes",
                PulseSeverity.Warning => "Needs attention",
                PulseSeverity.Critical => "Problems found",
                _ => "Unknown",
            };
        }
        finally
        {
            IsRunning = false;
        }
    }
}
