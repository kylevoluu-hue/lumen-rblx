using Lumen.Core.Common;

namespace Lumen.Core.Configuration;

/// <inheritdoc />
public sealed class SettingsService : ISettingsService, IDisposable
{
    private readonly IVersionedJsonStore _store;
    private readonly ILumenPaths _paths;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private LumenSettings _current = new();

    public SettingsService(IVersionedJsonStore store, ILumenPaths paths)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
    }

    public LumenSettings Current => _current;

    private string FilePath => Path.Combine(_paths.Config, "settings.json");

    public async Task<Result> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var result = await _store.LoadAsync(FilePath, static () => new LumenSettings(), cancellationToken)
                .ConfigureAwait(false);
            if (result.IsFailure)
            {
                return Result.Failure(result.Error!);
            }

            _current = Migrate(result.Value!);
            return Result.Success();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<Result> SaveAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await _store.SaveAsync(FilePath, _current, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<Result> UpdateAsync(Action<LumenSettings> mutate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mutate);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            mutate(_current);
            return await _store.SaveAsync(FilePath, _current, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Brings an older settings document up to the current schema version. Migrations are
    /// additive and forgiving so a downgrade never destroys the user's configuration.
    /// </summary>
    private static LumenSettings Migrate(LumenSettings settings)
    {
        if (settings.SchemaVersion < LumenSettings.CurrentSchemaVersion)
        {
            // No breaking migrations exist yet; future versions add their steps here.
            settings.SchemaVersion = LumenSettings.CurrentSchemaVersion;
        }

        return settings;
    }

    public void Dispose() => _gate.Dispose();
}
