using Lumen.Core.Common;
using Lumen.Core.Models;

namespace Lumen.Core.Abstractions;

/// <summary>
/// Manages Lumen profiles and their import/export. Exported profiles are always sanitized:
/// they never contain account credentials or secrets.
/// </summary>
public interface IProfileService
{
    IReadOnlyList<LumenProfile> Profiles { get; }

    Task<Result> LoadAsync(CancellationToken cancellationToken = default);

    Task<Result<LumenProfile>> CreateAsync(string name, CancellationToken cancellationToken = default);

    Task<Result> SaveAsync(LumenProfile profile, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Exports a profile to a portable file, guaranteed free of secrets.</summary>
    Task<Result<string>> ExportAsync(string id, string destinationPath, CancellationToken cancellationToken = default);

    Task<Result<LumenProfile>> ImportAsync(string sourcePath, CancellationToken cancellationToken = default);
}
