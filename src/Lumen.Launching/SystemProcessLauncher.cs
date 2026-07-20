using System.ComponentModel;
using System.Diagnostics;
using Lumen.Core.Abstractions;
using Lumen.Core.Common;
using Lumen.Security;

namespace Lumen.Launching;

/// <summary>
/// Opens a link with the OS default handler via shell-execute. The target is a single discrete
/// value (never a composed command line), and control characters are rejected, so this cannot be
/// used for command injection.
/// </summary>
public sealed class SystemProcessLauncher : IProcessLauncher
{
    public Result LaunchUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url) || ProcessArguments.HasControlCharacters(url))
        {
            return Result.Failure("Invalid launch target.");
        }

        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true,
            });
            return Result.Success();
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or FileNotFoundException or PlatformNotSupportedException)
        {
            return Result.Failure($"Could not open the launch target: {ex.Message}");
        }
    }
}
