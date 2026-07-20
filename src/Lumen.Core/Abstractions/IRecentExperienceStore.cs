using Lumen.Core.Models;

namespace Lumen.Core.Abstractions;

/// <summary>
/// Tracks, locally, the experiences the user has launched through Lumen (most-recent first). This
/// is Lumen's own history — it is never uploaded and never read from Roblox's private account data.
/// </summary>
public interface IRecentExperienceStore
{
    IReadOnlyList<RecentExperience> Recent { get; }

    Task LoadAsync(CancellationToken cancellationToken = default);

    Task RecordAsync(long placeId, string? name, CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}
