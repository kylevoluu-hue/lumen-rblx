using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lumen.Core.Abstractions;
using Lumen.Core.Common;

namespace Lumen.UI.ViewModels.Pages;

/// <summary>
/// Imports and applies safe cosmetic mod packages. Each package is extracted with zip-slip and
/// decompression-bomb guards, its manifest validated, its files safety-scanned and hash-verified,
/// and only then applied into the Roblox content folder with originals backed up.
/// </summary>
public sealed partial class ModsViewModel : PageViewModel
{
    private readonly IModPackageReader _reader;
    private readonly IModApplicator _applicator;
    private readonly ILumenPaths _paths;

    [ObservableProperty]
    private string _statusMessage = "Import a .lumenmod package — Lumen validates and scans it before anything is applied.";

    public ModsViewModel(IModPackageReader reader, IModApplicator applicator, ILumenPaths paths)
    {
        _reader = reader;
        _applicator = applicator;
        _paths = paths;
    }

    public override string Title => "Mods";

    public override string Description =>
        "Install safe cosmetic mods (cursors, fonts, textures). Every package is scanned and hash-verified; originals are backed up so you can restore them.";

    public ObservableCollection<string> Activity { get; } = new();

    /// <summary>Called by the view once the user has picked a package file.</summary>
    public async Task ImportAndApplyAsync(string packagePath)
    {
        var staging = Path.Combine(_paths.Mods, "staging", Guid.NewGuid().ToString("N"));

        var opened = await _reader.OpenAsync(packagePath, staging).ConfigureAwait(true);
        if (opened.IsFailure)
        {
            StatusMessage = "Package rejected: " + opened.Error;
            Activity.Insert(0, "✗ " + Path.GetFileName(packagePath) + " — " + opened.Error);
            return;
        }

        var package = opened.Value!;
        var applied = await _applicator.ApplyAsync(package).ConfigureAwait(true);
        if (applied.IsSuccess)
        {
            StatusMessage = $"Applied '{package.Manifest.Name}' ({applied.Value} file(s)).";
            Activity.Insert(0, $"✓ {package.Manifest.Name} — applied {applied.Value} file(s)");
        }
        else
        {
            StatusMessage = $"'{package.Manifest.Name}' passed validation, but could not be applied: {applied.Error}";
            Activity.Insert(0, $"• {package.Manifest.Name} — validated (safe); not applied: {applied.Error}");
        }
    }

    [RelayCommand]
    private async Task RestoreAsync()
    {
        var result = await _applicator.RestoreAsync().ConfigureAwait(true);
        StatusMessage = result.IsSuccess ? "Restored original Roblox content files." : result.Error!;
        if (result.IsSuccess)
        {
            Activity.Insert(0, "↩ Restored original content files");
        }
    }
}
