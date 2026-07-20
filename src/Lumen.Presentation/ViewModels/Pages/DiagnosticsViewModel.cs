using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lumen.Core.Abstractions;
using Lumen.Core.Common;
using Lumen.Core.Models;

namespace Lumen.UI.ViewModels.Pages;

/// <summary>
/// Builds a sanitized diagnostic report for preview. Nothing is uploaded — the report is shown
/// here and the user chooses what to do with it. Device identifiers are excluded and the output is
/// redacted.
/// </summary>
public sealed partial class DiagnosticsViewModel : PageViewModel
{
    private readonly IDiagnosticReportBuilder _reportBuilder;
    private readonly IRobloxInstallationLocator _locator;

    [ObservableProperty]
    private string _preview = "Press \"Generate\" to build a sanitized, local-only diagnostic report.";

    public DiagnosticsViewModel(IDiagnosticReportBuilder reportBuilder, IRobloxInstallationLocator locator)
    {
        _reportBuilder = reportBuilder;
        _locator = locator;
    }

    public override string Title => "Diagnostics";

    public override string Description => "A safe, local snapshot for troubleshooting. Previewed here, never uploaded.";

    [RelayCommand]
    private async Task GenerateAsync()
    {
        var installation = await _locator.LocateAsync(RobloxProduct.Player).ConfigureAwait(true);
        var inputs = new DiagnosticInputs
        {
            LumenVersion = LumenInfo.Version,
            RobloxInstallationStatus = installation.State.ToString(),
            RobloxVersion = installation.Version,
        };

        Preview = _reportBuilder.BuildPreview(inputs);
    }
}
