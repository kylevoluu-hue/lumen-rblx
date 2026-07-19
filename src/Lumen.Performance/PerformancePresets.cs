using Lumen.Core.Common;

namespace Lumen.Performance;

public enum PerformancePreset
{
    Default,
    Balanced,
    MaximumPerformance,
    LowEndPc,
    LowEndLaptop,
    BatterySaver,
    Competitive,
    Recording,
    Streaming,
    HighQuality,
    Cinematic,
    Screenshot,
    Custom,
}

/// <summary>Honest, non-placebo descriptions of the built-in performance presets (spec §10).</summary>
public static class PerformancePresets
{
    public static IReadOnlyDictionary<PerformancePreset, string> Descriptions { get; } =
        new Dictionary<PerformancePreset, string>
        {
            [PerformancePreset.Default] = "Lumen's standard behaviour with no changes.",
            [PerformancePreset.Balanced] = "Trims Lumen's own overhead while Roblox runs, without touching Roblox itself.",
            [PerformancePreset.MaximumPerformance] = "Minimises Lumen's footprint: pauses animations, blur, and background tasks during play.",
            [PerformancePreset.LowEndPc] = "Reduces Lumen visual effects and background work for lower-spec desktops.",
            [PerformancePreset.LowEndLaptop] = "Like Low-End PC, plus battery-aware behaviour for laptops.",
            [PerformancePreset.BatterySaver] = "Pauses animated backgrounds and optional update checks to save power.",
            [PerformancePreset.Competitive] = "Prioritises a clean, distraction-free launcher; applies your supported FPS-limit choice.",
            [PerformancePreset.Recording] = "A stable configuration suited to screen recording.",
            [PerformancePreset.Streaming] = "Reduces launcher activity that could interfere with streaming software.",
            [PerformancePreset.HighQuality] = "Keeps all launcher visual effects enabled.",
            [PerformancePreset.Cinematic] = "Full launcher effects for showcase and capture.",
            [PerformancePreset.Screenshot] = "Pairs with a high-resolution display preset for still capture.",
            [PerformancePreset.Custom] = "Your own combination of the individual options.",
        };
}

/// <summary>
/// Applies safe, real performance options (spec §10). Every option is honest: no fake RAM
/// cleaners, ping boosters, registry edits, or security-feature toggles. Options that affect the
/// Roblox process (priority, supported FPS limits) use only legitimate, supported mechanisms.
///
/// PHASE 5 (roadmap): concrete application (process priority, cache cleaning) is Windows-runtime
/// work, deferred from this foundation milestone.
/// </summary>
public interface IPerformanceManager
{
    Task<Result> ApplyAsync(PerformancePreset preset, CancellationToken cancellationToken = default);
}
