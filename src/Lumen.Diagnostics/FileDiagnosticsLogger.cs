using Lumen.Core.Abstractions;
using Lumen.Core.Common;
using Lumen.Security;

namespace Lumen.Diagnostics;

/// <summary>
/// Local, append-only logger. Every component name and message is passed through
/// <see cref="Redactor"/> before it touches disk, so secrets can never be logged. Logging is
/// opt-in (gated by a predicate the host wires to the privacy setting), local-only, and never
/// uploaded. Failures while logging are swallowed — logging must never crash the app — but never
/// silently swallow application errors elsewhere.
/// </summary>
public sealed class FileDiagnosticsLogger : IDiagnosticsLogger
{
    private readonly ILumenPaths _paths;
    private readonly Func<bool> _isEnabled;
    private readonly object _gate = new();

    public FileDiagnosticsLogger(ILumenPaths paths, Func<bool>? isEnabled = null)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _isEnabled = isEnabled ?? (static () => true);
    }

    private string CurrentLogFile => Path.Combine(_paths.Logs, $"lumen-{DateTime.UtcNow:yyyyMMdd}.log");

    public void Log(LogLevel level, string component, string message)
    {
        if (!_isEnabled())
        {
            return;
        }

        var safeComponent = Redactor.Redact(component);
        var safeMessage = Redactor.Redact(message);
        var line = $"{DateTimeOffset.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ} [{level,-7}] {safeComponent}: {safeMessage}";

        try
        {
            lock (_gate)
            {
                Directory.CreateDirectory(_paths.Logs);
                File.AppendAllText(CurrentLogFile, line + Environment.NewLine);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Logging must never throw; drop the line rather than disrupt the app.
        }
    }

    /// <summary>Deletes log files older than the retention window. Best-effort.</summary>
    public void PruneOldLogs(int retentionDays)
    {
        if (retentionDays <= 0 || !Directory.Exists(_paths.Logs))
        {
            return;
        }

        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        foreach (var file in Directory.EnumerateFiles(_paths.Logs, "lumen-*.log"))
        {
            try
            {
                if (File.GetLastWriteTimeUtc(file) < cutoff)
                {
                    File.Delete(file);
                }
            }
            catch (IOException)
            {
            }
        }
    }
}
