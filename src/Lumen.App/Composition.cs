using Lumen.Accounts;
using Lumen.Core.Abstractions;
using Lumen.Core.Common;
using Lumen.Core.Configuration;
using Lumen.Diagnostics;
using Lumen.Launching;
using Lumen.Mods;
using Lumen.Profiles;
using Lumen.Roblox;
using Lumen.Storage;
using Lumen.UI.Navigation;
using Lumen.UI.ViewModels;
using Lumen.UI.ViewModels.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.App;

/// <summary>
/// The composition root. All concrete services are registered here so the rest of the application
/// depends only on interfaces. Platform-specific choices (e.g. the secure credential store) are
/// resolved here based on the running OS.
/// </summary>
internal static class Composition
{
    public static IServiceProvider Build()
    {
        var services = new ServiceCollection();

        // Core infrastructure
        services.AddSingleton<ILumenPaths>(_ => LumenPaths.CreateDefault());
        services.AddSingleton<IVersionedJsonStore, JsonFileStore>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IDiagnosticsLogger>(sp =>
        {
            var paths = sp.GetRequiredService<ILumenPaths>();
            var settings = sp.GetRequiredService<ISettingsService>();
            return new FileDiagnosticsLogger(paths, () => settings.Current.Privacy.OptionalLoggingEnabled);
        });

        // Roblox integration
        services.AddSingleton<IRobloxInstallationLocator, RobloxInstallationLocator>();
        services.AddSingleton<IExperienceLinkValidator, ExperienceLinkValidator>();

        // Navigation + launching
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IProcessLauncher, SystemProcessLauncher>();
        services.AddSingleton<ILaunchService, LaunchService>();

        // Accounts — DPAPI on Windows, an explicit "unavailable" store elsewhere (no insecure fallback).
        services.AddSingleton<ISecureCredentialStore>(sp =>
            OperatingSystem.IsWindows()
                ? new DpapiCredentialStore(sp.GetRequiredService<ILumenPaths>())
                : new UnavailableCredentialStore());
        services.AddSingleton<IAccountManager, AccountManager>();

        // Profiles
        services.AddSingleton<IProfileService, ProfileService>();

        // Mods
        services.AddSingleton<IModSafetyScanner, ModSafetyScanner>();
        services.AddSingleton<IModPackageReader, ModPackageReader>();
        services.AddSingleton<IModApplicator, ModApplicator>();

        // FastFlags
        services.AddSingleton<IFastFlagManager, FastFlagManager>();

        // Diagnostics report
        services.AddSingleton<IDiagnosticReportBuilder, DiagnosticReportBuilder>();

        // Page view-models
        services.AddSingleton<HomeViewModel>();
        services.AddSingleton<LaunchViewModel>();
        services.AddSingleton<AccountsViewModel>();
        services.AddSingleton<ProfilesViewModel>();
        services.AddSingleton<PerformanceViewModel>();
        services.AddSingleton<FastFlagsViewModel>();
        services.AddSingleton<ModsViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<PrivacyViewModel>();
        services.AddSingleton<DiagnosticsViewModel>();
        services.AddSingleton<AboutViewModel>();

        // Shell (navigation)
        services.AddSingleton(BuildShell);

        return services.BuildServiceProvider();
    }

    private static ShellViewModel BuildShell(IServiceProvider sp)
    {
        var entries = new List<NavigationEntry>
        {
            new("home", "Home", "\U0001F3E0", sp.GetRequiredService<HomeViewModel>()),
            new("launch", "Launch", "▶", sp.GetRequiredService<LaunchViewModel>()),
            new("accounts", "Accounts", "\U0001F464", sp.GetRequiredService<AccountsViewModel>()),
            new("profiles", "Profiles", "\U0001F5C2", sp.GetRequiredService<ProfilesViewModel>()),
            new("performance", "Performance", "⚡", sp.GetRequiredService<PerformanceViewModel>()),
            new("fastflags", "FastFlags", "\U0001F6A9", sp.GetRequiredService<FastFlagsViewModel>()),
            new("display", "Display", "\U0001F5A5", Placeholder(
                "Display",
                "Resolution and window-mode manager.",
                "Resolution presets and validation exist in Lumen.Resolution; monitor placement is Phase 5.")),
            new("graphics", "Graphics", "\U0001F3A8", Placeholder(
                "Graphics",
                "Graphics presets with honest control-scope labels.",
                "Preset taxonomy is defined in Lumen.Graphics; writing supported Roblox config is Phase 5.")),
            new("overlays", "Overlays", "\U0001F4CA", Placeholder(
                "Overlays",
                "Optional, privacy-respecting overlays. Disabled by default; never read Roblox memory.",
                "Overlay rendering and safe measurement backends are Phase 6.")),
            new("mods", "Mods", "\U0001F9E9", sp.GetRequiredService<ModsViewModel>()),
            new("modpacks", "Mod Packs", "\U0001F4E6", Placeholder(
                "Mod Packs",
                "Combine several safe mods into a shareable pack.",
                "Phase 7.")),
            new("themes", "Themes", "\U0001F308", Placeholder(
                "Themes",
                "Non-executable cosmetic themes.",
                "The theme schema is defined in Lumen.Themes; the runtime theme engine lands in a later phase.")),
            new("installations", "Installations", "\U0001F4BD", Placeholder(
                "Installations",
                "Detect, verify, and repair launcher-managed files.",
                "Read-only detection is implemented in Lumen.Roblox; repair/restore is Phase 3.")),
            new("downloads", "Downloads", "⬇", Placeholder(
                "Downloads",
                "HTTPS-only, allowlisted, hash-verified downloads.",
                "The URL allowlist and hash verification exist in Lumen.Security; the downloader is Phase 7.")),
            new("diagnostics", "Diagnostics", "\U0001FA7A", sp.GetRequiredService<DiagnosticsViewModel>()),
            new("privacy", "Privacy", "\U0001F6E1", sp.GetRequiredService<PrivacyViewModel>()),
            new("settings", "Lumen Settings", "⚙", sp.GetRequiredService<SettingsViewModel>()),
            new("about", "About", "ℹ", sp.GetRequiredService<AboutViewModel>()),
        };

        return new ShellViewModel(entries, sp.GetRequiredService<INavigationService>());
    }

    private static PlaceholderPageViewModel Placeholder(string title, string description, string note) =>
        new(title, description, note);
}
