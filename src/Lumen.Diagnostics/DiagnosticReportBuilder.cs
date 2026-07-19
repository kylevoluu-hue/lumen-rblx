using System.Runtime.InteropServices;
using System.Text;
using Lumen.Core.Abstractions;
using Lumen.Core.Models;
using Lumen.Security;

namespace Lumen.Diagnostics;

/// <summary>
/// Builds a human-readable diagnostic report for the Diagnostics page. The report is always
/// previewed to the user and never uploaded automatically. Machine name, user name, and other
/// device identifiers are deliberately excluded, and the whole report is passed through
/// <see cref="Redactor"/> as a final safety net. IP addresses are excluded unless the user
/// explicitly opts in.
/// </summary>
public sealed class DiagnosticReportBuilder : IDiagnosticReportBuilder
{
    public string BuildPreview(DiagnosticInputs inputs, bool includeIpAddresses = false)
    {
        ArgumentNullException.ThrowIfNull(inputs);

        var report = new StringBuilder();
        report.AppendLine("Lumen diagnostic report");
        report.AppendLine("=======================");
        report.AppendLine("This report is local only and is never uploaded automatically.");
        report.AppendLine();

        report.AppendLine("[Lumen]");
        report.AppendLine($"Version: {inputs.LumenVersion}");
        report.AppendLine($"Active profile: {inputs.ActiveProfile ?? "none"}");
        report.AppendLine($"Enabled mods: {inputs.EnabledModCount}");
        report.AppendLine();

        report.AppendLine("[Roblox]");
        report.AppendLine($"Installation status: {inputs.RobloxInstallationStatus ?? "unknown"}");
        report.AppendLine($"Version: {inputs.RobloxVersion ?? "unknown"}");
        report.AppendLine();

        report.AppendLine("[System]");
        report.AppendLine($"OS: {RuntimeInformation.OSDescription}");
        report.AppendLine($"Architecture: {RuntimeInformation.OSArchitecture}");
        report.AppendLine($"Runtime: {RuntimeInformation.FrameworkDescription}");
        report.AppendLine($"Logical processors: {Environment.ProcessorCount}");
        // Deliberately NOT included: machine name, user name, or any device identifier.
        report.AppendLine();

        if (inputs.Extra.Count > 0)
        {
            report.AppendLine("[Details]");
            foreach (var (key, value) in inputs.Extra)
            {
                report.AppendLine($"{key}: {value}");
            }
            report.AppendLine();
        }

        // Final safety net: redact anything sensitive that slipped through.
        return Redactor.Redact(report.ToString(), redactIpAddresses: !includeIpAddresses);
    }
}
