using Lumen.Core.Abstractions;
using Lumen.Core.Common;
using Lumen.Core.Configuration;
using Lumen.Core.Models;

namespace Lumen.Roblox;

/// <summary>Serializable list of recently launched experiences.</summary>
public sealed class RecentCollection
{
    public List<RecentExperience> Items { get; set; } = new();
}

/// <summary>
/// Local, most-recent-first history of experiences launched through Lumen. Persisted to disk,
/// never uploaded, and never sourced from Roblox's private account data.
/// </summary>
public sealed class RecentExperienceStore : IRecentExperienceStore
{
    private const int MaxItems = 24;

    private readonly IVersionedJsonStore _store;
    private readonly ILumenPaths _paths;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private RecentCollection _collection = new();

    public RecentExperienceStore(IVersionedJsonStore store, ILumenPaths paths)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
    }

    public IReadOnlyList<RecentExperience> Recent => _collection.Items;

    private string FilePath => Path.Combine(_paths.Root, "recent.json");

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var result = await _store.LoadAsync(FilePath, static () => new RecentCollection(), cancellationToken)
                .ConfigureAwait(false);
            if (result.IsSuccess)
            {
                _collection = result.Value!;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RecordAsync(long placeId, string? name, CancellationToken cancellationToken = default)
    {
        if (placeId <= 0)
        {
            return;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _collection.Items.RemoveAll(x => x.PlaceId == placeId);
            _collection.Items.Insert(0, new RecentExperience(placeId, name, DateTimeOffset.UtcNow));

            if (_collection.Items.Count > MaxItems)
            {
                _collection.Items.RemoveRange(MaxItems, _collection.Items.Count - MaxItems);
            }

            await _store.SaveAsync(FilePath, _collection, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _collection.Items.Clear();
            await _store.SaveAsync(FilePath, _collection, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }
}
