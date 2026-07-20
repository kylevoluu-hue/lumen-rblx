using Lumen.Core.Models;

namespace Lumen.Roblox;

/// <summary>
/// The curated set of FastFlags Lumen will offer. This is a strict allowlist of safe
/// performance/rendering/telemetry-reduction flags. It intentionally contains NO flags that:
/// bypass anti-cheat or moderation, manipulate networking, reveal hidden players or objects,
/// change hitboxes/reach, or remove gameplay visibility effects. Anything not listed here is
/// rejected.
/// </summary>
public static class FastFlagAllowlist
{
    public static IReadOnlyList<FastFlagDefinition> All { get; } = new[]
    {
        new FastFlagDefinition(
            "DFIntTaskSchedulerTargetFps", FastFlagValueType.Int,
            "Maximum frame-rate cap. Higher values allow more FPS if your hardware can render them.",
            DefaultValue: "60", Min: 1, Max: 10000),

        new FastFlagDefinition(
            "FFlagDebugGraphicsPreferD3D11", FastFlagValueType.Bool,
            "Prefer the DirectX 11 renderer.", DefaultValue: "False"),

        new FastFlagDefinition(
            "FFlagDebugGraphicsPreferD3D11FL10", FastFlagValueType.Bool,
            "Prefer DirectX 11 (feature level 10) for older GPUs.", DefaultValue: "False"),

        new FastFlagDefinition(
            "FFlagDebugGraphicsPreferVulkan", FastFlagValueType.Bool,
            "Prefer the Vulkan renderer (experimental; may be unstable on some GPUs).",
            DefaultValue: "False", IsExperimental: true),

        new FastFlagDefinition(
            "DFFlagTextureQualityOverrideEnabled", FastFlagValueType.Bool,
            "Enable a manual texture-quality level below.", DefaultValue: "False"),

        new FastFlagDefinition(
            "DFIntTextureQualityOverride", FastFlagValueType.Int,
            "Texture quality level (0 = lowest, 3 = highest). Requires the override above to be on.",
            DefaultValue: "3", Min: 0, Max: 3),

        new FastFlagDefinition(
            "FIntRenderShadowIntensity", FastFlagValueType.Int,
            "Shadow intensity (0 = off, 100 = full).", DefaultValue: "100", Min: 0, Max: 100),

        new FastFlagDefinition(
            "FFlagDebugDisableTelemetryEphemeralCounter", FastFlagValueType.Bool,
            "Reduce Roblox's own telemetry (ephemeral counters).", DefaultValue: "False"),

        new FastFlagDefinition(
            "FFlagDebugDisableTelemetryPoint", FastFlagValueType.Bool,
            "Reduce Roblox's own telemetry (points).", DefaultValue: "False"),
    };

    public static FastFlagDefinition? Find(string name) =>
        All.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.Ordinal));
}
