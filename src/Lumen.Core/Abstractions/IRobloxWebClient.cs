using Lumen.Core.Common;
using Lumen.Core.Models;

namespace Lumen.Core.Abstractions;

/// <summary>
/// Fetches PUBLIC Roblox data through official Roblox web APIs (users, friends, thumbnails, games).
/// It sends no cookies or credentials and reads only public information the user has chosen to link
/// (by entering their username). All requests go to allowlisted roblox.com hosts and are disclosed
/// on the Privacy page. Private data (play history, messages) is never accessed.
/// </summary>
public interface IRobloxWebClient
{
    Task<Result<long>> ResolveUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<Result<RobloxUser>> GetUserAsync(long userId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<RobloxFriend>>> GetFriendsAsync(long userId, CancellationToken cancellationToken = default);

    Task<Result<string>> GetAvatarHeadshotUrlAsync(long userId, CancellationToken cancellationToken = default);

    Task<Result<string>> GetExperienceNameAsync(long placeId, CancellationToken cancellationToken = default);

    /// <summary>Downloads image bytes from an allowlisted host (e.g. a Roblox avatar thumbnail).</summary>
    Task<Result<byte[]>> GetImageAsync(string url, CancellationToken cancellationToken = default);
}
