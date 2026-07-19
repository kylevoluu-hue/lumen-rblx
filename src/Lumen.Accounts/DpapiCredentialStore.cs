using System.Security.Cryptography;
using System.Text;
using Lumen.Core.Abstractions;
using Lumen.Core.Common;
using Lumen.Security;

namespace Lumen.Accounts;

/// <summary>
/// Stores permitted credential material encrypted at rest with Windows DPAPI
/// (<see cref="DataProtectionScope.CurrentUser"/>), so only the current Windows user can decrypt
/// it. Used only for information a supported authentication flow is allowed to persist — never a
/// Roblox password or <c>.ROBLOSECURITY</c> cookie. On non-Windows platforms it reports
/// unavailable rather than falling back to insecure storage.
/// </summary>
public sealed class DpapiCredentialStore : ISecureCredentialStore
{
    private readonly string _directory;

    public DpapiCredentialStore(ILumenPaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        _directory = Path.Combine(paths.Accounts, "secure");
    }

    public bool IsAvailable => OperatingSystem.IsWindows();

    public Task<Result> StoreAsync(string key, ReadOnlyMemory<byte> secret, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult(Unavailable());
        }

        try
        {
            Directory.CreateDirectory(_directory);
            var protectedBytes = ProtectedData.Protect(secret.ToArray(), optionalEntropy: null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(FilePathFor(key), protectedBytes);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CryptographicException)
        {
            return Task.FromResult(Result.Failure($"Could not store credential: {ex.Message}"));
        }
    }

    public Task<Result<byte[]>> RetrieveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult(Result.Failure<byte[]>("Secure credential storage is not available on this platform."));
        }

        try
        {
            var path = FilePathFor(key);
            if (!File.Exists(path))
            {
                return Task.FromResult(Result.Failure<byte[]>("No stored credential for that key."));
            }

            var protectedBytes = File.ReadAllBytes(path);
            var plain = ProtectedData.Unprotect(protectedBytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
            return Task.FromResult(Result.Success(plain));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CryptographicException)
        {
            return Task.FromResult(Result.Failure<byte[]>($"Could not read credential: {ex.Message}"));
        }
    }

    public Task<Result> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var path = FilePathFor(key);
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Task.FromResult(Result.Failure($"Could not remove credential: {ex.Message}"));
        }
    }

    // Derive a safe, opaque file name from the key so it can never traverse the filesystem.
    private string FilePathFor(string key) =>
        Path.Combine(_directory, FileHashing.ComputeSha256(Encoding.UTF8.GetBytes(key)) + ".bin");

    private static Result Unavailable() =>
        Result.Failure("Secure credential storage is not available on this platform.");
}
