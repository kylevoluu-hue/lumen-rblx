using Lumen.Core.Models;

namespace Lumen.Core.Abstractions;

/// <summary>
/// Builds a human-readable, sanitized diagnostic report that is always previewed to the user and
/// never uploaded automatically. Device identifiers are excluded and the output is redacted.
/// </summary>
public interface IDiagnosticReportBuilder
{
    string BuildPreview(DiagnosticInputs inputs, bool includeIpAddresses = false);
}
