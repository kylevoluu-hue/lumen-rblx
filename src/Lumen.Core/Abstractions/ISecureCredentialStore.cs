using Lumen.Core.Common;

namespace Lumen.Core.Abstractions;

/// <summary>
/// Stores permitted, non-plaintext credential material in an OS-protected vault (Windows
/// Credential Manager / DPAPI). Used ONLY for information a supported authentication flow is
/// allowed to persist. Lumen never stores Roblox passwords or <c>.ROBLOSECURITY</c> cookies
/// here or anywhere else. On platforms without a supported vault, <see cref="IsAvailable"/> is
/// false and all operations return a failure result rather than falling back to insecure storage.
/// </summary>
public interface ISecureCredentialStore
{
    /// <summary>True when a hardware/OS-backed secure store is available on this platform.</summary>
    bool IsAvailable { get; }

    Task<Result> StoreAsync(string key, ReadOnlyMemory<byte> secret, CancellationToken cancellationToken = default);

    Task<Result<byte[]>> RetrieveAsync(string key, CancellationToken cancellationToken = default);

    Task<Result> RemoveAsync(string key, CancellationToken cancellationToken = default);
}
