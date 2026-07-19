namespace Lumen.Core.Models;

public enum ModCategory
{
    Unknown = 0,
    Performance,
    Display,
    VisualFilter,
    Accessibility,
    Texture,
    Cursor,
    Crosshair,
    Sound,
    Font,
    Skybox,
    Ui,
    Theme,
    LoadingScreen,
    Overlay,
    Icon,
    Profile,
}

/// <summary>Trust signal shown next to a mod. Popularity is deliberately not a verification level.</summary>
public enum ModVerification
{
    Unverified = 0,
    CommunitySubmitted = 1,
    SignatureVerified = 2,
    SourceReviewed = 3,
    OfficialLumen = 4,
}

/// <summary>
/// The <c>manifest.json</c> of a <c>.lumenmod</c> package. This is pure declarative data:
/// it MUST NOT contain executable commands, scripts, or network instructions. The reader
/// validates every field before a package is trusted.
/// </summary>
public sealed class ModManifest
{
    public int FormatVersion { get; set; } = 1;

    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ModCategory Category { get; set; } = ModCategory.Unknown;

    public string MinimumLumenVersion { get; set; } = "1.0.0";

    public List<string> SupportedRobloxVersions { get; set; } = new();

    /// <summary>Relative paths of the files this mod contributes.</summary>
    public List<string> Files { get; set; } = new();

    /// <summary>Ids of mods this one conflicts with.</summary>
    public List<string> Conflicts { get; set; } = new();

    /// <summary>Ids of mods this one depends on.</summary>
    public List<string> Dependencies { get; set; } = new();

    /// <summary>Map of relative file path to expected SHA-256 (hex).</summary>
    public Dictionary<string, string> Hashes { get; set; } = new();
}
