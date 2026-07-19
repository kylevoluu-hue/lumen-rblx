using Lumen.Core.Common;

namespace Lumen.Graphics;

public enum GraphicsPreset
{
    Potato,
    UltraLow,
    Low,
    Competitive,
    Balanced,
    High,
    Ultra,
    Cinematic,
    Screenshot,
    BatterySaver,
    Custom,
}

/// <summary>Whether a graphics setting is something Lumen controls, Roblox exposes, or is advisory.</summary>
public enum GraphicsControlScope
{
    /// <summary>Lumen can apply this directly and reliably.</summary>
    LumenControlled,

    /// <summary>Roblox exposes this; Lumen can recommend/write a supported configuration value.</summary>
    RobloxExposed,

    /// <summary>Recommendation only — Lumen cannot enforce it.</summary>
    Recommendation,
}

/// <summary>
/// Graphics presets (spec §13). Every option is honestly labelled with what actually controls it,
/// so the UI never implies Lumen can change something it cannot.
///
/// PHASE 5 (roadmap): writing supported Roblox graphics configuration is deferred from this
/// foundation milestone; the preset taxonomy is defined here.
/// </summary>
public interface IGraphicsManager
{
    Task<Result> ApplyAsync(GraphicsPreset preset, CancellationToken cancellationToken = default);
}
