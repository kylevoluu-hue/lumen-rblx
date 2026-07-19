using Lumen.Core.Common;
using Lumen.Core.Models;

namespace Lumen.Roblox;

/// <summary>
/// Builds the official web link Lumen opens to launch an experience. Lumen hands off to the
/// user's installed, already-authenticated Roblox client via the official website URL; it does
/// not construct authenticated launch tickets and does not handle credentials. The private-server
/// code, when present, is included in the URL that is opened but is never logged.
/// </summary>
public static class RobloxDeepLink
{
    private const string GamesBase = "https://www.roblox.com/games/";

    public static Result<string> Build(ExperienceTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (target.PlaceId is not > 0)
        {
            return Result.Failure<string>("A place id is required to build a launch link.");
        }

        var url = GamesBase + target.PlaceId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);

        if (target.Kind == ExperienceLinkKind.PrivateServer && target.HasPrivateServerCode)
        {
            url += "?privateServerLinkCode=" + Uri.EscapeDataString(target.PrivateServerCode!);
        }

        return Result.Success(url);
    }
}
