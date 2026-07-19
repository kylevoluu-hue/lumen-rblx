namespace Lumen.Core.Abstractions;

public enum LogLevel
{
    Info = 0,
    Warning = 1,
    Error = 2,
}

/// <summary>
/// Privacy-safe local logger. Every message is passed through redaction before being written,
/// so cookies, tokens, authorization headers, private-server links, IP addresses, and user
/// home paths never reach the log files. Logging is local-only and never uploaded.
/// </summary>
public interface IDiagnosticsLogger
{
    void Log(LogLevel level, string component, string message);

    void Info(string component, string message) => Log(LogLevel.Info, component, message);

    void Warning(string component, string message) => Log(LogLevel.Warning, component, message);

    void Error(string component, string message) => Log(LogLevel.Error, component, message);
}
