using Lumen.Core.Abstractions;
using Lumen.Core.Configuration;
using Lumen.Core.Models;
using Lumen.Diagnostics.Pulse;
using Lumen.Storage;
using Xunit;

namespace Lumen.Tests;

public class PulseServiceTests
{
    private sealed class StubCheck : IPulseCheck
    {
        private readonly PulseSeverity _severity;
        public StubCheck(string id, PulseSeverity severity) { Id = id; _severity = severity; }
        public string Id { get; }
        public Task<PulseCheck> RunAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new PulseCheck(Id, Id, _severity, "stub"));
    }

    private sealed class ThrowingCheck : IPulseCheck
    {
        public string Id => "throws";
        public Task<PulseCheck> RunAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("boom");
    }

    [Fact]
    public async Task Overall_is_the_worst_severity()
    {
        var service = new PulseService(new IPulseCheck[]
        {
            new StubCheck("a", PulseSeverity.Ok),
            new StubCheck("b", PulseSeverity.Warning),
            new StubCheck("c", PulseSeverity.Info),
        });

        var report = await service.RunAsync();

        Assert.Equal(3, report.Checks.Count);
        Assert.Equal(PulseSeverity.Warning, report.Overall);
    }

    [Fact]
    public async Task A_throwing_check_becomes_a_critical_result()
    {
        var report = await new PulseService(new IPulseCheck[] { new ThrowingCheck() }).RunAsync();
        Assert.Equal(PulseSeverity.Critical, report.Overall);
    }
}

public class PulseCheckTests
{
    [Fact]
    public async Task DataFolder_is_ok_when_writable()
    {
        using var temp = new TempDirectory();
        var result = await new DataFolderPulseCheck(temp.AsLumenPaths()).RunAsync();
        Assert.Equal(PulseSeverity.Ok, result.Severity);
    }

    [Fact]
    public async Task DiskSpace_returns_a_result()
    {
        using var temp = new TempDirectory();
        var result = await new DiskSpacePulseCheck(temp.AsLumenPaths()).RunAsync();
        Assert.Equal("disk-space", result.Id);
    }

    [Fact]
    public async Task Backups_is_info_when_none_exist()
    {
        using var temp = new TempDirectory();
        var result = await new BackupsPulseCheck(temp.AsLumenPaths()).RunAsync();
        Assert.Equal(PulseSeverity.Info, result.Severity);
    }

    [Fact]
    public async Task CrashLog_warns_on_a_recent_critical_entry()
    {
        using var temp = new TempDirectory();
        var paths = temp.AsLumenPaths();
        var logFile = Path.Combine(paths.Logs, $"lumen-{DateTime.UtcNow:yyyyMMdd}.log");
        await File.WriteAllTextAsync(logFile, "2026-07-21T00:00:00Z [Critical] something failed\n");

        var result = await new CrashLogPulseCheck(paths).RunAsync();
        Assert.Equal(PulseSeverity.Warning, result.Severity);
    }

    [Fact]
    public async Task CrashLog_is_ok_when_clean()
    {
        using var temp = new TempDirectory();
        var result = await new CrashLogPulseCheck(temp.AsLumenPaths()).RunAsync();
        Assert.Equal(PulseSeverity.Ok, result.Severity);
    }

    [Fact]
    public async Task Configuration_is_ok_on_a_fresh_install()
    {
        using var temp = new TempDirectory();
        var settings = new SettingsService(new JsonFileStore(), temp.AsLumenPaths());
        var result = await new ConfigurationPulseCheck(settings).RunAsync();
        Assert.Equal(PulseSeverity.Ok, result.Severity);
    }
}
