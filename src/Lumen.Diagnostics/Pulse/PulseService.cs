using Lumen.Core.Abstractions;
using Lumen.Core.Models;

namespace Lumen.Diagnostics.Pulse;

/// <summary>Runs every registered <see cref="IPulseCheck"/> and aggregates the results. A check that
/// throws is reported as a Critical result rather than failing the whole run.</summary>
public sealed class PulseService : IPulseService
{
    private readonly IEnumerable<IPulseCheck> _checks;

    public PulseService(IEnumerable<IPulseCheck> checks)
    {
        _checks = checks ?? throw new ArgumentNullException(nameof(checks));
    }

    public async Task<PulseReport> RunAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<PulseCheck>();

        foreach (var check in _checks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                results.Add(await check.RunAsync(cancellationToken).ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                results.Add(new PulseCheck(
                    check.Id, check.Id, PulseSeverity.Critical, "This check failed to run.", ex.Message));
            }
        }

        return new PulseReport { Checks = results };
    }
}
