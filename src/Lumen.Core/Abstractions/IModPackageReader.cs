using Lumen.Core.Common;
using Lumen.Core.Models;

namespace Lumen.Core.Abstractions;

/// <summary>
/// Opens and validates a <c>.lumenmod</c> package: safely extracts it (guarding against
/// zip-slip and decompression bombs), parses and validates the manifest, runs the safety
/// scanner, and verifies declared hashes. Returns a failure — never a partially-trusted
/// package — if any check fails.
/// </summary>
public interface IModPackageReader
{
    Task<Result<ModPackage>> OpenAsync(
        string packagePath,
        string stagingDirectory,
        CancellationToken cancellationToken = default);
}
