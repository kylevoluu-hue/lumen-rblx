using Lumen.Core.Common;

namespace Lumen.Core.Configuration;

/// <summary>High-level access to <see cref="LumenSettings"/> with safe load/save semantics.</summary>
public interface ISettingsService
{
    /// <summary>The current in-memory settings. Always valid (never null), even before the first load.</summary>
    LumenSettings Current { get; }

    /// <summary>Loads settings from disk, recovering to defaults if the file is missing or corrupt.</summary>
    Task<Result> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists the current settings atomically.</summary>
    Task<Result> SaveAsync(CancellationToken cancellationToken = default);

    /// <summary>Mutates settings under a single load-modify-save operation and persists the result.</summary>
    Task<Result> UpdateAsync(Action<LumenSettings> mutate, CancellationToken cancellationToken = default);
}
