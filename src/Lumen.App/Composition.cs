using System.Net.Http;
using Lumen.Accounts;
using Lumen.Core.Abstractions;
using Lumen.Core.Common;
using Lumen.Core.Configuration;
using Lumen.Diagnostics;
using Lumen.Launching;
using Lumen.Mods;
using Lumen.Profiles;
using Lumen.Roblox;
using Lumen.Security;
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

        // Networking (public Roblox data only; every request is host-allowlisted)
        services.AddSingleton(_ =>
        {
            var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("Lumen");
            return http;
        });
        services.AddSingleton(_ => UrlValidator.CreateDefault());

        // Roblox integration
        services.AddSingleton<IRobloxInstallationLocator, RobloxInstallationLocator>();
        services.AddSingleton<IExperienceLinkValidator, ExperienceLinkValidator>();
        services.AddSingleton<IRobloxWebClient, RobloxWebClient>();
        services.AddSingleton<IRecentExperienceStore, RecentExperienceStore>();

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
        services.AddSingleton<SocialViewModel>();
        services.AddSingleton<ProfilesViewModel>();
        services.AddSingleton<PerformanceViewModel>();
        services.AddSingleton<FastFlagsViewModel>();
        services.AddSingleton<GraphicsViewModel>();
        services.AddSingleton<ModsViewModel>();
        services.AddSingleton<InstallationsViewModel>();
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
            new("social", "Social", "\U0001F465", sp.GetRequiredService<SocialViewModel>()),
            new("profiles", "Profiles", "\U0001F5C2", sp.GetRequiredService<ProfilesViewModel>()),
            new("performance", "Performance", "⚡", sp.GetRequiredService<PerformanceViewModel>()),
            new("fastflags", "FastFlags", "\U0001F6A9", sp.GetRequiredService<FastFlagsViewModel>()),
            new("graphics", "Graphics", "\U0001F3A8", sp.GetRequiredService<GraphicsViewModel>()),
            new("mods", "Mods", "\U0001F9E9", sp.GetRequiredService<ModsViewModel>()),
            new("installations", "Installations", "\U0001F4BD", sp.GetRequiredService<InstallationsViewModel>()),
            new("diagnostics", "Diagnostics", "\U0001FA7A", sp.GetRequiredService<DiagnosticsViewModel>()),
            new("privacy", "Privacy", "\U0001F6E1", sp.GetRequiredService<PrivacyViewModel>()),
            new("settings", "Lumen Settings", "⚙", sp.GetRequiredService<SettingsViewModel>()),
            new("about", "About", "ℹ", sp.GetRequiredService<AboutViewModel>()),
        };

        return new ShellViewModel(entries, sp.GetRequiredService<INavigationService>());
    }
}
