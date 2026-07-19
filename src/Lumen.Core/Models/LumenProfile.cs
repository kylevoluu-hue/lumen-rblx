namespace Lumen.Core.Models;

/// <summary>
/// A named bundle of launch preferences. Profiles contain only references to other local
/// settings (by id) and never contain account credentials, so they can be exported and shared
/// safely once sanitized.
/// </summary>
public sealed class LumenProfile
{
    public required string Id { get; init; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    /// <summary>Local account id to select for this profile (metadata reference only, never a secret).</summary>
    public string? AccountId { get; set; }

    public string? ResolutionId { get; set; }

    public int? FpsLimit { get; set; }

    public string? GraphicsPresetId { get; set; }

    public string? PerformancePresetId { get; set; }

    public string? VisualFilterId { get; set; }

    public string? CursorId { get; set; }

    public string? CrosshairId { get; set; }

    public string? ModPackId { get; set; }

    public string? OverlayLayoutId { get; set; }

    public string? ThemeId { get; set; }

    public bool IsDefault { get; set; }
}
