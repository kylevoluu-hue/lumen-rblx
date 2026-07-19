using Lumen.Core.Common;

namespace Lumen.Overlays;

/// <summary>Overlay widget types (spec §17). Display-only; none read Roblox memory or inject.</summary>
public enum OverlayWidgetKind
{
    Fps,
    Ping,
    FrameTime,
    CpuUsage,
    GpuUsage,
    RamUsage,
    VramUsage,
    Battery,
    PowerMode,
    Clock,
    SessionTimer,
    RecordingIndicator,
    ProfileName,
    Resolution,
    FpsLimit,
    ActiveModPack,
    Keystrokes,
    MouseButtons,
    Cps,
    Crosshair,
    NetworkStatus,
}

/// <summary>
/// Manages optional overlays. Overlays are disabled by default, run as a separate, clearly-visible
/// Lumen process, never read Roblox memory or inject, never capture passwords/chat/sensitive input,
/// never send data externally, and stop when Roblox closes.
///
/// PHASE 6 (roadmap): overlay rendering and the safe measurement backends are deferred from this
/// foundation milestone. The widget taxonomy and privacy contract are defined here. Note that some
/// widgets (e.g. accurate in-game FPS/ping) are only enabled where a safe, non-injection measurement
/// exists, and are otherwise disabled with an explanation rather than showing fabricated values.
/// </summary>
public interface IOverlayManager
{
    IReadOnlyList<OverlayWidgetKind> AvailableWidgets { get; }

    Task<Result> StopAllAsync(CancellationToken cancellationToken = default);
}
