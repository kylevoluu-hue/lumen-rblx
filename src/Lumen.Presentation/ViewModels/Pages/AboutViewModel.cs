using Lumen.Core.Common;

namespace Lumen.UI.ViewModels.Pages;

/// <summary>The About page. Prominently carries the mandatory independence disclaimer.</summary>
public sealed class AboutViewModel : PageViewModel
{
    public override string Title => "About";

    public override string Description => LumenInfo.Tagline;

    public string Version => LumenInfo.Version;

    public string Disclaimer => LumenInfo.Disclaimer;

    public string AboutText =>
        "Lumen is an open, community-built launcher and cosmetic configuration manager for Roblox. " +
        "It focuses on performance, safe cosmetic customization, privacy, and transparency. " +
        "Lumen never injects into Roblox, reads Roblox memory, executes scripts, imports account cookies, " +
        "or bypasses anti-cheat. Features that cannot be built safely are left disabled and clearly explained.";
}
