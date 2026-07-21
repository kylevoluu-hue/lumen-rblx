using System.Net.Http;
using Lumen.Accounts;
using Lumen.Core.Abstractions;
using Lumen.Core.Common;
using Lumen.Core.Configuration;
using Lumen.Diagnostics;
using Lumen.Diagnostics.Pulse;
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

namespace Lumen.Composition;

/// <summary>
/// Registers every Lumen service and shared view-model. UI-head-agnostic: callers add their own
/// <see cref="IThemeApplier"/> implementation and build the provider. This guarantees the Avalonia
/// and WinUI heads run on an identical service graph.
/// </summary>
public static class LumenServiceRegistration
{
    public static IServiceCollection AddLumenServices(this IServiceCollection services)
    {
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

        // Lumen Pulse (local health checks)
        services.AddSingleton<IPulseCheck, DataFolderPulseCheck>();
        services.AddSingleton<IPulseCheck, DiskSpacePulseCheck>();
        services.AddSingleton<IPulseCheck, ConfigurationPulseCheck>();
        services.AddSingleton<IPulseCheck, BackupsPulseCheck>();
        services.AddSingleton<IPulseCheck, CrashLogPulseCheck>();
        services.AddSingleton<IPulseCheck, RobloxInstallationPulseCheck>();
        services.AddSingleton<IPulseCheck, NetworkPulseCheck>();
        services.AddSingleton<IPulseService, PulseService>();

        // Shared page view-models
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
        services.AddSingleton<PulseViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<PrivacyViewModel>();
        services.AddSingleton<DiagnosticsViewModel>();
        services.AddSingleton<AboutViewModel>();

        // Shell (navigation) — identical page set across UI heads
        services.AddSingleton(BuildShell);

        return services;
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
            new("pulse", "Lumen Pulse", "\U0001F493", sp.GetRequiredService<PulseViewModel>()),
            new("diagnostics", "Diagnostics", "\U0001FA7A", sp.GetRequiredService<DiagnosticsViewModel>()),
            new("privacy", "Privacy", "\U0001F6E1", sp.GetRequiredService<PrivacyViewModel>()),
            new("settings", "Lumen Settings", "⚙", sp.GetRequiredService<SettingsViewModel>()),
            new("about", "About", "ℹ", sp.GetRequiredService<AboutViewModel>()),
        };

        return new ShellViewModel(entries, sp.GetRequiredService<INavigationService>());
    }
}
