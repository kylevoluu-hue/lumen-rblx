using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lumen.Core.Abstractions;
using Lumen.Core.Models;

namespace Lumen.UI.ViewModels.Pages;

/// <summary>One editable allowlisted FastFlag row. Not a page — rendered inside the FastFlags list.</summary>
public sealed partial class FastFlagEntryViewModel : ObservableObject
{
    public FastFlagEntryViewModel(FastFlagDefinition definition)
    {
        Definition = definition;
        _value = definition.DefaultValue;
    }

    public FastFlagDefinition Definition { get; }

    public string Name => Definition.Name;

    public string Description => Definition.Description;

    public bool IsExperimental => Definition.IsExperimental;

    [ObservableProperty]
    private bool _enabled;

    [ObservableProperty]
    private string _value;
}

/// <summary>
/// The safe, allowlisted FastFlag editor. Only curated flags appear; values are validated and the
/// exact <c>ClientAppSettings.json</c> is previewable before it is applied (with backup), and can be
/// restored to defaults. No unsafe flags are ever offered.
/// </summary>
public sealed partial class FastFlagsViewModel : PageViewModel
{
    private readonly IFastFlagManager _manager;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreview))]
    private string _previewText = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public bool HasPreview => !string.IsNullOrEmpty(PreviewText);

    public FastFlagsViewModel(IFastFlagManager manager)
    {
        _manager = manager;
        foreach (var definition in _manager.Allowlist)
        {
            Flags.Add(new FastFlagEntryViewModel(definition));
        }
    }

    public override string Title => "FastFlags";

    public override string Description =>
        "A safe, allowlisted set of Roblox configuration flags. Enable the ones you want, preview the exact file, then apply.";

    public ObservableCollection<FastFlagEntryViewModel> Flags { get; } = new();

    private Dictionary<string, string> Collect() =>
        Flags.Where(f => f.Enabled).ToDictionary(f => f.Name, f => f.Value, StringComparer.Ordinal);

    [RelayCommand]
    private void Preview()
    {
        var result = _manager.BuildPreview(Collect());
        if (result.IsSuccess)
        {
            PreviewText = result.Value!;
            StatusMessage = "Preview built. Nothing has been written yet.";
        }
        else
        {
            StatusMessage = result.Error!;
        }
    }

    [RelayCommand]
    private async Task ApplyAsync()
    {
        var result = await _manager.ApplyAsync(Collect()).ConfigureAwait(true);
        StatusMessage = result.IsSuccess
            ? "Applied and backed up the previous file."
            : result.Error!;
    }

    [RelayCommand]
    private async Task RestoreDefaultsAsync()
    {
        var result = await _manager.RestoreDefaultsAsync().ConfigureAwait(true);
        PreviewText = string.Empty;
        StatusMessage = result.IsSuccess ? "Cleared all FastFlag overrides." : result.Error!;
    }
}
