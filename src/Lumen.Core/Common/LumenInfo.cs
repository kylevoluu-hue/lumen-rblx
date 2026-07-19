using System.Reflection;

namespace Lumen.Core.Common;

/// <summary>
/// Static application identity. The <see cref="Disclaimer"/> is mandatory and must be
/// surfaced prominently in the UI and documentation.
/// </summary>
public static class LumenInfo
{
    public const string Name = "Lumen";

    public const string Tagline = "A secure, high-performance, customizable Roblox launcher.";

    /// <summary>
    /// Mandatory independence disclaimer. Displayed in the About page, the first-launch
    /// wizard, and the README. Do not remove or obscure.
    /// </summary>
    public const string Disclaimer =
        "Lumen is an independent, community-created launcher and is not affiliated with, " +
        "endorsed by, sponsored by, or officially connected to Roblox Corporation.";

    /// <summary>Full informational version (e.g. "1.0.0-alpha.1"), read from assembly metadata.</summary>
    public static string Version { get; } = ResolveVersion();

    private static string ResolveVersion()
    {
        var informational = typeof(LumenInfo).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (string.IsNullOrWhiteSpace(informational))
        {
            return "1.0.0";
        }

        // Strip the "+<git-sha>" build-metadata suffix the SDK appends.
        var plus = informational.IndexOf('+');
        return plus >= 0 ? informational[..plus] : informational;
    }
}
