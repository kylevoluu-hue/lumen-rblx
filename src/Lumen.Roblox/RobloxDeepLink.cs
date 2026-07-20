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

    /// <summary>
    /// Builds the best link to actually start the experience. For a place or experience URL this
    /// is the official <c>roblox://</c> app deep link, which launches the installed, already
    /// signed-in Roblox client directly into the experience. Private servers fall back to the
    /// official web link carrying the (never-logged) private-server code.
    /// </summary>
    public static Result<string> BuildLaunchLink(ExperienceTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (target.PlaceId is not > 0)
        {
            return Result.Failure<string>("A place id is required to build a launch link.");
        }

        // Private-server joins are only reliable through the official web link + code.
        if (target.Kind == ExperienceLinkKind.PrivateServer && target.HasPrivateServerCode)
        {
            return Build(target);
        }

        var placeId = target.PlaceId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return Result.Success($"roblox://experiences/start?placeId={placeId}");
    }
}
