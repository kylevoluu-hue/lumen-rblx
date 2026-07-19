namespace Lumen.Core.Configuration;

public enum NavigationLayout
{
    Sidebar = 0,
    Top = 1,
}

public enum InterfaceDensity
{
    Comfortable = 0,
    Cozy = 1,
    Compact = 2,
}

/// <summary>Cosmetic interface preferences. All are safe, local, and reversible.</summary>
public sealed class AppearanceSettings
{
    public string ThemeId { get; set; } = "lumen-dark";

    /// <summary>Accent colour as <c>#RRGGBB</c>. Validated before use.</summary>
    public string AccentColor { get; set; } = "#6C8CFF";

    public NavigationLayout NavigationLayout { get; set; } = NavigationLayout.Sidebar;

    public InterfaceDensity Density { get; set; } = InterfaceDensity.Comfortable;

    public bool CompactMode { get; set; }

    /// <summary>Corner radius in device-independent pixels (0–24).</summary>
    public double CornerRadius { get; set; } = 10;

    /// <summary>Animation speed multiplier (0 disables animations; 1 is default).</summary>
    public double AnimationSpeed { get; set; } = 1.0;

    public bool UseBlur { get; set; }

    public string? CustomBackgroundPath { get; set; }
}

/// <summary>
/// Privacy-related toggles. Every potentially sensitive capability defaults to OFF, per the
/// first-launch requirements: nothing is enabled without explicit user opt-in.
/// </summary>
public sealed class PrivacySettings
{
    /// <summary>Local, sanitized diagnostic logging. Opt-in.</summary>
    public bool OptionalLoggingEnabled { get; set; }

    /// <summary>Diagnostic report uploads are never automatic. This flag only permits a manual, previewed export flow.</summary>
    public bool DiagnosticExportsAllowed { get; set; }

    public bool StartupLaunchEnabled { get; set; }

    public bool DiscordRichPresenceEnabled { get; set; }

    public bool UpdateChecksEnabled { get; set; }

    public bool CommunityModsEnabled { get; set; }

    public bool ThirdPartyExtensionsEnabled { get; set; }

    /// <summary>Require Windows Hello / account authentication before revealing saved account metadata.</summary>
    public bool RequireAuthToViewAccounts { get; set; }

    /// <summary>Auto-lock the account manager after this many minutes of inactivity (0 disables).</summary>
    public int AccountAutoLockMinutes { get; set; }
}

/// <summary>Root Lumen configuration document. Persisted as versioned JSON. Contains no secrets.</summary>
public sealed class LumenSettings
{
    /// <summary>Schema version, used for forward/backward-compatible migrations.</summary>
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public const int CurrentSchemaVersion = 1;

    public bool FirstLaunchCompleted { get; set; }

    public bool PortableMode { get; set; }

    public AppearanceSettings Appearance { get; set; } = new();

    public PrivacySettings Privacy { get; set; } = new();

    /// <summary>Ordered navigation page identifiers. Enables reordering.</summary>
    public List<string> NavigationOrder { get; set; } = new();

    /// <summary>Navigation page identifiers the user has hidden.</summary>
    public List<string> HiddenPages { get; set; } = new();

    public string? SelectedProfileId { get; set; }

    /// <summary>Local identifier of the last-used account. Never a token or secret.</summary>
    public string? LastUsedAccountId { get; set; }
}
