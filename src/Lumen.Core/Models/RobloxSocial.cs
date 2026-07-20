namespace Lumen.Core.Models;

/// <summary>Public Roblox user profile information (no private or authenticated data).</summary>
public sealed record RobloxUser(
    long UserId,
    string Username,
    string DisplayName,
    string? Description,
    DateTimeOffset? Created);

/// <summary>A public friend entry.</summary>
public sealed record RobloxFriend(long UserId, string Username, string DisplayName)
{
    public string? AvatarUrl { get; set; }
}

/// <summary>
/// An experience the user launched through Lumen. Lumen tracks this locally — it never reads
/// Roblox's private play history (that would require the account cookie Lumen does not use).
/// </summary>
public sealed record RecentExperience(long PlaceId, string? Name, DateTimeOffset LastPlayedUtc)
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string Label => string.IsNullOrEmpty(Name) ? $"Place {PlaceId}" : Name!;
}
