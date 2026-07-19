using Lumen.Core.Common;

namespace Lumen.Core.Configuration;

/// <summary>
/// Persists strongly-typed documents as versioned JSON with atomic writes, automatic
/// backups, and corruption recovery. Implemented by the Storage layer. Callers depend only
/// on this abstraction so the IO strategy can evolve without touching business logic.
/// </summary>
public interface IVersionedJsonStore
{
    /// <summary>
    /// Loads a document. If the file is missing, a default is created and returned. If the
    /// file exists but is corrupt, the corrupt file is quarantined (backed up) and a default
    /// is returned so the application always starts in a valid state.
    /// </summary>
    Task<Result<T>> LoadAsync<T>(string filePath, Func<T> createDefault, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Atomically writes a document, backing up any previous version first.</summary>
    Task<Result> SaveAsync<T>(string filePath, T value, CancellationToken cancellationToken = default)
        where T : class;
}
