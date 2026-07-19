using Lumen.Core.Common;
using Lumen.Core.Models;

namespace Lumen.Launching;

/// <summary>Ordered phases surfaced to the user during a launch (see spec §9).</summary>
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
/// Orchestrates a launch: validate the target, apply the selected profile and cosmetic mods,
/// start overlays, then hand off to the user's installed, already-authenticated Roblox client via
/// the official web link. It never builds a shell command from user input and never handles
/// credentials.
///
/// PHASE 3/6 (roadmap): the concrete implementation — process hand-off, overlay lifecycle, and
/// temporary-change restoration — is Windows-runtime work and is intentionally not implemented in
/// this foundation milestone. The interface and phase model are defined here so later phases slot
/// in without churn.
/// </summary>
public interface ILaunchService
{
    Task<Result> LaunchAsync(
        ExperienceTarget target,
        IProgress<LaunchPhase>? progress = null,
        CancellationToken cancellationToken = default);
}
