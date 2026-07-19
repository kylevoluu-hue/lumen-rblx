using Lumen.Core.Abstractions;
using Lumen.Core.Common;

namespace Lumen.Accounts;

/// <summary>
/// A credential store used on platforms without an OS-backed secure vault. It never persists
/// secrets to an insecure location; instead every operation fails clearly. This keeps the
/// "no insecure fallback" guarantee explicit rather than implicit.
/// </summary>
public sealed class UnavailableCredentialStore : ISecureCredentialStore
{
    private const string Message = "Secure credential storage requires Windows (DPAPI / Credential Manager).";

    public bool IsAvailable => false;

    public Task<Result> StoreAsync(string key, ReadOnlyMemory<byte> secret, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Failure(Message));

    public Task<Result<byte[]>> RetrieveAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Failure<byte[]>(Message));

    public Task<Result> RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success());
}
