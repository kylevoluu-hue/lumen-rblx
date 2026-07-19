namespace Lumen.Extensions;

/// <summary>
/// The capabilities an extension may request (spec §37). Every permission is shown to the user
/// before installation and enforced at runtime. Notably absent: any ability to touch Roblox
/// memory, browser cookies, authentication tokens, drivers, or arbitrary files.
/// </summary>
public enum ExtensionPermission
{
    ReadLumenTheme,
    AddLumenPage,
    AddDashboardWidget,
    ReadActiveProfile,
    AccessExtensionDataFolder,
    DisplayNotification,
    RequestApprovedDomains,
    ReadNonSensitiveRobloxInfo,
}

/// <summary>
/// Declarative manifest for a launcher extension. Extensions are strictly separate from cosmetic
/// mods and run under least privilege.
/// </summary>
public sealed class ExtensionManifest
{
    public int FormatVersion { get; set; } = 1;

    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public string Version { get; set; } = "1.0.0";

    public string Description { get; set; } = string.Empty;

    public string MinimumLumenVersion { get; set; } = "1.0.0";

    public string EntryPoint { get; set; } = string.Empty;

    public List<ExtensionPermission> RequestedPermissions { get; set; } = new();

    /// <summary>Approved domains the extension may contact (each requires explicit user consent).</summary>
    public List<string> ApprovedDomains { get; set; } = new();

    public Dictionary<string, string> Hashes { get; set; } = new();
}

/// <summary>
/// Loads, permission-gates, and runs extensions in a restricted process, with a Safe Mode that
/// disables all third-party extensions and crash isolation so a faulty extension cannot take down
/// Lumen.
///
/// PHASE 8 (roadmap): the sandboxed extension host is deferred from this foundation milestone. The
/// manifest and permission model are defined here so the security review can reason about them.
/// </summary>
public interface IExtensionHost
{
    IReadOnlyList<ExtensionManifest> LoadedExtensions { get; }

    bool SafeMode { get; }
}
