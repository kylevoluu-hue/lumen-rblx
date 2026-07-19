using System.Security.Cryptography;
using System.Text;
using Lumen.Core.Common;

namespace Lumen.Security;

/// <summary>SHA-256 hashing and constant-time verification for file integrity checks.</summary>
public static class FileHashing
{
    public static async Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(
            filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string ComputeSha256(ReadOnlySpan<byte> data) =>
        Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();

    /// <summary>Case-insensitive, length-checked, constant-time comparison of two hex digests.</summary>
    public static bool HashesEqual(string? expectedHex, string? actualHex)
    {
        if (expectedHex is null || actualHex is null)
        {
            return false;
        }

        var expected = Encoding.ASCII.GetBytes(expectedHex.Trim().ToLowerInvariant());
        var actual = Encoding.ASCII.GetBytes(actualHex.Trim().ToLowerInvariant());

        // FixedTimeEquals requires equal lengths; different lengths are simply not equal.
        return expected.Length == actual.Length && CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    public static async Task<Result> VerifyFileAsync(
        string filePath, string expectedSha256, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            return Result.Failure($"File not found: '{Path.GetFileName(filePath)}'.");
        }

        var actual = await ComputeSha256Async(filePath, cancellationToken).ConfigureAwait(false);
        return HashesEqual(expectedSha256, actual)
            ? Result.Success()
            : Result.Failure($"Hash mismatch for '{Path.GetFileName(filePath)}'.");
    }
}
