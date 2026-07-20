using Lumen.Core.Common;
using Lumen.Core.Models;

namespace Lumen.Core.Abstractions;

/// <summary>Ordered phases surfaced to the user during a launch (spec §9).</summary>
public enum LaunchPhase
{
    CheckingRoblox,
    CheckingForUpdates,
    ValidatingConfiguration,
    ApplyingProfile,
    ApplyingCosmeticMods,
    VerifyingBackups,
    StartingOverlays,
    LaunchingRoblox,
    RobloxRunning,
    RestoringTemporaryChanges,
    Complete,
}

/// <summary>
/// Orchestrates a launch: validates the target, optionally applies the selected profile and
/// cosmetic configuration, then hands off to the user's installed, already-authenticated Roblox
/// client via the official launch link. It handles no credentials, builds no shell commands, and
/// never injects into Roblox.
/// </summary>
public interface ILaunchService
{
    Task<Result> LaunchAsync(
        ExperienceTarget target,
        IProgress<LaunchPhase>? progress = null,
        CancellationToken cancellationToken = default);
}
