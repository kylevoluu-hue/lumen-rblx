using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lumen.Core.Abstractions;

namespace Lumen.UI.ViewModels.Pages;

/// <summary>
/// Launch by place id, official experience URL, or a supported private-server link. The input is
/// validated before anything is launched, and the launch hands off to the installed Roblox client
/// via the official launch link.
/// </summary>
public sealed partial class LaunchViewModel : PageViewModel
{
    private readonly IExperienceLinkValidator _validator;
    private readonly ILaunchService _launchService;

    [ObservableProperty]
    private string _input = string.Empty;

    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    private bool _isLaunching;

    public LaunchViewModel(IExperienceLinkValidator validator, ILaunchService launchService)
    {
        _validator = validator;
        _launchService = launchService;
    }

    public override string Title => "Launch";

    public override string Description => "Enter a place id, an official roblox.com experience link, or a private-server link.";

    [RelayCommand]
    private async Task LaunchAsync()
    {
        Status = string.Empty;

        var validated = _validator.Validate(Input);
        if (validated.IsFailure)
        {
            Status = validated.Error!;
            return;
        }

        IsLaunching = true;
        try
        {
            var progress = new Progress<LaunchPhase>(phase => Status = Describe(phase));
            var result = await _launchService.LaunchAsync(validated.Value!, progress).ConfigureAwait(true);
            Status = result.IsSuccess
                ? $"Launch requested for {validated.Value!.SafeDescription} — Roblox should be starting."
                : result.Error!;
        }
        finally
        {
            IsLaunching = false;
        }
    }

    private static string Describe(LaunchPhase phase) => phase switch
    {
        LaunchPhase.ValidatingConfiguration => "Validating…",
        LaunchPhase.LaunchingRoblox => "Launching Roblox…",
        LaunchPhase.RobloxRunning => "Roblox is starting…",
        LaunchPhase.Complete => "Launch complete.",
        _ => "Working…",
    };
}
