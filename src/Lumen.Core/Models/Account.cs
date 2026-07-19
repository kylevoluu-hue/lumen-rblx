namespace Lumen.Core.Models;

/// <summary>
/// Local, non-secret metadata about a Roblox account the user has added to Lumen.
///
/// IMPORTANT: This type MUST NEVER hold a password, <c>.ROBLOSECURITY</c> cookie, session
/// token, or any authentication secret. Lumen does not collect or import those. Any permitted
/// authentication material is stored only in the OS credential vault via
/// <see cref="Abstractions.ISecureCredentialStore"/>, never in this model and never in JSON.
/// </summary>
public sealed class Account
{
    /// <summary>Locally-generated identifier (a GUID). Not a Roblox identifier.</summary>
    public required string Id { get; init; }

    /// <summary>User-chosen nickname shown in the UI.</summary>
    public string? Nickname { get; set; }

    public string? Username { get; set; }

    public string? DisplayName { get; set; }

    /// <summary>Public Roblox user id, if known. Public information, not a secret.</summary>
    public long? UserId { get; set; }

    /// <summary>Public avatar image URL, if known.</summary>
    public string? AvatarUrl { get; set; }

    public DateTimeOffset? LastUsedUtc { get; set; }

    /// <summary>Best display label for the UI, preferring nickname then display name then username.</summary>
    public string DisplayLabel =>
        !string.IsNullOrWhiteSpace(Nickname) ? Nickname!
        : !string.IsNullOrWhiteSpace(DisplayName) ? DisplayName!
        : !string.IsNullOrWhiteSpace(Username) ? Username!
        : "Unnamed account";
}
