using Lumen.Core.Abstractions;
using Lumen.Core.Common;
using Lumen.Core.Configuration;
using Lumen.Core.Models;

namespace Lumen.Diagnostics.Pulse;

/// <summary>Verifies Lumen's data folder exists and is writable.</summary>
public sealed class DataFolderPulseCheck : IPulseCheck
{
    private readonly ILumenPaths _paths;

    public DataFolderPulseCheck(ILumenPaths paths) => _paths = paths;

    public string Id => "data-folder";

    public Task<PulseCheck> RunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _paths.EnsureCreated();
            var probe = Path.Combine(_paths.Cache, ".pulse-write-test");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
            return Task.FromResult(new PulseCheck(Id, "Data folder", PulseSeverity.Ok,
                "Lumen's data folder is present and writable."));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Task.FromResult(new PulseCheck(Id, "Data folder", PulseSeverity.Critical,
                "Lumen cannot write to its data folder.", "Check the folder's permissions.", AutoRepairAvailable: false));
        }
    }
}

/// <summary>Reports free space on the drive holding Lumen's data.</summary>
public sealed class DiskSpacePulseCheck : IPulseCheck
{
    private readonly ILumenPaths _paths;

    public DiskSpacePulseCheck(ILumenPaths paths) => _paths = paths;

    public string Id => "disk-space";

    public Task<PulseCheck> RunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var root = Path.GetPathRoot(Path.GetFullPath(_paths.Root));
            if (string.IsNullOrEmpty(root))
            {
                return Task.FromResult(new PulseCheck(Id, "Disk space", PulseSeverity.Info,
                    "Could not determine the data drive."));
            }

            var freeGb = new DriveInfo(root).AvailableFreeSpace / (1024.0 * 1024 * 1024);
            var (severity, action) = freeGb switch
            {
                < 0.5 => (PulseSeverity.Critical, "Free up space — Roblox and Lumen need room to install and update."),
                < 2.0 => (PulseSeverity.Warning, "Low disk space can cause update or install failures."),
                _ => (PulseSeverity.Ok, (string?)null),
            };

            return Task.FromResult(new PulseCheck(Id, "Disk space", severity,
                $"{freeGb:F1} GB free on the Lumen data drive.", action));
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or UnauthorizedAccessException)
        {
            return Task.FromResult(new PulseCheck(Id, "Disk space", PulseSeverity.Info,
                "Could not determine free disk space.", ex.Message));
        }
    }
}

/// <summary>Verifies launcher settings load and are valid.</summary>
public sealed class ConfigurationPulseCheck : IPulseCheck
{
    private readonly ISettingsService _settings;

    public ConfigurationPulseCheck(ISettingsService settings) => _settings = settings;

    public string Id => "configuration";

    public async Task<PulseCheck> RunAsync(CancellationToken cancellationToken = default)
    {
        var result = await _settings.LoadAsync(cancellationToken).ConfigureAwait(false);
        return result.IsSuccess
            ? new PulseCheck(Id, "Configuration", PulseSeverity.Ok, "Launcher settings loaded and valid.")
            : new PulseCheck(Id, "Configuration", PulseSeverity.Warning,
                "Settings could not be loaded; defaults are in use.", result.Error);
    }
}

/// <summary>Reports the age of the most recent backup.</summary>
public sealed class BackupsPulseCheck : IPulseCheck
{
    private readonly ILumenPaths _paths;

    public BackupsPulseCheck(ILumenPaths paths) => _paths = paths;

    public string Id => "backups";

    public Task<PulseCheck> RunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Directory.Exists(_paths.Backups))
            {
                return Task.FromResult(new PulseCheck(Id, "Backups", PulseSeverity.Info,
                    "No backups have been created yet.", "A backup is taken automatically before updates and mod changes."));
            }

            var newest = Directory.EnumerateFileSystemEntries(_paths.Backups, "*", SearchOption.AllDirectories)
                .Select(File.GetLastWriteTimeUtc)
                .DefaultIfEmpty(DateTime.MinValue)
                .Max();

            if (newest == DateTime.MinValue)
            {
                return Task.FromResult(new PulseCheck(Id, "Backups", PulseSeverity.Info, "No backups have been created yet."));
            }

            var ageDays = (DateTime.UtcNow - newest).TotalDays;
            return ageDays > 30
                ? Task.FromResult(new PulseCheck(Id, "Backups", PulseSeverity.Warning,
                    $"The most recent backup is {ageDays:F0} days old.", "Create a fresh backup from the Backups page."))
                : Task.FromResult(new PulseCheck(Id, "Backups", PulseSeverity.Ok, "A recent backup is present."));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Task.FromResult(new PulseCheck(Id, "Backups", PulseSeverity.Info, "Could not read the backups folder.", ex.Message));
        }
    }
}

/// <summary>Flags recent critical entries in today's log.</summary>
public sealed class CrashLogPulseCheck : IPulseCheck
{
    private readonly ILumenPaths _paths;

    public CrashLogPulseCheck(ILumenPaths paths) => _paths = paths;

    public string Id => "crash-logs";

    public Task<PulseCheck> RunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Directory.Exists(_paths.Logs))
            {
                return Task.FromResult(new PulseCheck(Id, "Crash logs", PulseSeverity.Ok, "No logs to review."));
            }

            var cutoff = DateTime.UtcNow.AddDays(-1);
            foreach (var file in Directory.EnumerateFiles(_paths.Logs, "lumen-*.log"))
            {
                if (File.GetLastWriteTimeUtc(file) < cutoff)
                {
                    continue;
                }

                if (File.ReadAllText(file).Contains("[Critical", StringComparison.Ordinal))
                {
                    return Task.FromResult(new PulseCheck(Id, "Crash logs", PulseSeverity.Warning,
                        "Critical errors were logged in the last day.", "Open Diagnostics to review the (redacted) log."));
                }
            }

            return Task.FromResult(new PulseCheck(Id, "Crash logs", PulseSeverity.Ok, "No recent critical errors were logged."));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Task.FromResult(new PulseCheck(Id, "Crash logs", PulseSeverity.Info, "Could not read the logs folder.", ex.Message));
        }
    }
}

/// <summary>Reports whether an official Roblox installation is detected (Windows).</summary>
public sealed class RobloxInstallationPulseCheck : IPulseCheck
{
    private readonly IRobloxInstallationLocator _locator;

    public RobloxInstallationPulseCheck(IRobloxInstallationLocator locator) => _locator = locator;

    public string Id => "roblox-install";

    public async Task<PulseCheck> RunAsync(CancellationToken cancellationToken = default)
    {
        var install = await _locator.LocateAsync(RobloxProduct.Player, cancellationToken).ConfigureAwait(false);
        return install.State switch
        {
            RobloxInstallationState.Healthy => new PulseCheck(Id, "Roblox installation", PulseSeverity.Ok,
                $"Roblox Player detected (version {install.Version})."),
            RobloxInstallationState.UpdateAvailable => new PulseCheck(Id, "Roblox installation", PulseSeverity.Info,
                "A Roblox update is available."),
            RobloxInstallationState.Broken => new PulseCheck(Id, "Roblox installation", PulseSeverity.Critical,
                "The Roblox installation appears broken.", "Use Repair, or reinstall Roblox.", AutoRepairAvailable: true),
            RobloxInstallationState.NotDetected => new PulseCheck(Id, "Roblox installation", PulseSeverity.Warning,
                "No Roblox installation was detected.", "Install Roblox, then re-run Pulse."),
            _ => new PulseCheck(Id, "Roblox installation", PulseSeverity.Info,
                "Installation detection runs on Windows."),
        };
    }
}

/// <summary>Checks connectivity to official Roblox services (public endpoint).</summary>
public sealed class NetworkPulseCheck : IPulseCheck
{
    private readonly IRobloxWebClient _web;

    public NetworkPulseCheck(IRobloxWebClient web) => _web = web;

    public string Id => "network";

    public async Task<PulseCheck> RunAsync(CancellationToken cancellationToken = default)
    {
        // User id 1 is a stable, public Roblox account — a lightweight reachability probe.
        var result = await _web.GetUserAsync(1, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess
            ? new PulseCheck(Id, "Network", PulseSeverity.Ok, "Connected to official Roblox services.")
            : new PulseCheck(Id, "Network", PulseSeverity.Warning,
                "Could not reach Roblox services.", "Check your internet connection; some launches and profile data may be unavailable.");
    }
}
