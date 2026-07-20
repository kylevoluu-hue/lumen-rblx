using Lumen.Core.Common;

namespace Lumen.Core.Abstractions;

/// <summary>
/// Opens a validated URL or protocol link with the operating system's default handler
/// (e.g. the official Roblox client for a <c>roblox://</c> link, or the default browser for an
/// <c>https://</c> link). Implementations launch via the OS shell-execute mechanism with a single
/// discrete target — never by building a shell command string — so there is no command injection.
/// </summary>
public interface IProcessLauncher
{
    Result LaunchUrl(string url);
}
