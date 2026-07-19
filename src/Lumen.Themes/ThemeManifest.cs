using Lumen.Core.Common;

namespace Lumen.Themes;

/// <summary>
/// A non-executable theme document. Themes only describe cosmetics (colours, fonts, spacing,
/// radii, approved images). They cannot run code, make network requests, read files outside their
/// folder, or touch account or Roblox data — those guarantees are enforced by the loader.
/// </summary>
public sealed class ThemeManifest
{
    public int FormatVersion { get; set; } = 1;

    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public string Version { get; set; } = "1.0.0";

    public string? AccentColor { get; set; }

    public string? Background { get; set; }

    public double? CornerRadius { get; set; }

    /// <summary>Named colour overrides (validated as colours before use).</summary>
    public Dictionary<string, string> Colors { get; set; } = new();
}

/// <summary>
/// Loads and validates themes.
///
/// PHASE 2/continued (roadmap): the runtime theme engine that maps a validated manifest onto
/// Avalonia resources is deferred from this foundation milestone. The example theme in
/// <c>examples/themes</c> conforms to this schema.
/// </summary>
public interface IThemeService
{
    Result<ThemeManifest> Validate(ThemeManifest manifest);
}
