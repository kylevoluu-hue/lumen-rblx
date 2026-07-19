using System.Text.Json.Serialization;

namespace Lumen.Core.Models;

public enum ExperienceLinkKind
{
    PlaceId = 0,
    ExperienceUrl = 1,
    PrivateServer = 2,
}

/// <summary>
/// A validated Roblox launch target produced by an experience-link validator.
///
/// Private-server access codes are treated as sensitive: they are held in
/// <see cref="PrivateServerCode"/> (excluded from serialization) and never appear in
/// <see cref="ToString"/>, logs, Discord status, or exports.
/// </summary>
public sealed record ExperienceTarget
{
    public required ExperienceLinkKind Kind { get; init; }

    public long? PlaceId { get; init; }

    /// <summary>Sensitive private-server code. Never logged or serialized.</summary>
    [JsonIgnore]
    public string? PrivateServerCode { get; init; }

    public bool HasPrivateServerCode => !string.IsNullOrEmpty(PrivateServerCode);

    /// <summary>Safe, log-friendly description. Never contains the private-server code.</summary>
    public string SafeDescription => Kind switch
    {
        ExperienceLinkKind.PlaceId => $"Place {PlaceId}",
        ExperienceLinkKind.ExperienceUrl => $"Experience {PlaceId}",
        ExperienceLinkKind.PrivateServer => $"Place {PlaceId} (private server)",
        _ => "Unknown target",
    };

    public override string ToString() => SafeDescription;
}
