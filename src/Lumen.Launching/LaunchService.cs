using Lumen.Core.Abstractions;
using Lumen.Core.Common;
using Lumen.Core.Models;
using Lumen.Roblox;

namespace Lumen.Launching;

/// <summary>
/// Orchestrates a launch: reports progress phases, builds the official Roblox launch link for the
/// validated target, and hands off to the installed Roblox client via <see cref="IProcessLauncher"/>.
/// It handles no credentials, builds no shell commands, and never injects into Roblox.
/// </summary>
public sealed class LaunchService : ILaunchService
{
    private readonly IProcessLauncher _launcher;

    public LaunchService(IProcessLauncher launcher)
    {
        _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
    }

    public Task<Result> LaunchAsync(
        ExperienceTarget target,
        IProgress<LaunchPhase>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        cancellationToken.ThrowIfCancellationRequested();

        progress?.Report(LaunchPhase.ValidatingConfiguration);

        var link = RobloxDeepLink.BuildLaunchLink(target);
        if (link.IsFailure)
        {
            return Task.FromResult(Result.Failure(link.Error!));
        }

        progress?.Report(LaunchPhase.LaunchingRoblox);

        var launched = _launcher.LaunchUrl(link.Value!);
        if (launched.IsFailure)
        {
            return Task.FromResult(launched);
        }

        progress?.Report(LaunchPhase.RobloxRunning);
        progress?.Report(LaunchPhase.Complete);
        return Task.FromResult(Result.Success());
    }
}
