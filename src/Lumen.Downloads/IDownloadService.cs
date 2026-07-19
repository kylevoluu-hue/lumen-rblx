using Lumen.Core.Common;

namespace Lumen.Downloads;

/// <summary>
/// Downloads files over HTTPS from allowlisted hosts only, verifying a SHA-256 hash before the
/// result is trusted. Every request is validated by the Security layer's URL allowlist so it can
/// be surfaced in the Network Activity Viewer; nothing is fetched from an undisclosed destination.
///
/// PHASE 7 (roadmap): the concrete downloader is deferred from this foundation milestone. The URL
/// allowlist (<c>UrlValidator</c>) and hash verification (<c>FileHashing</c>) it will use are fully
/// implemented in <c>Lumen.Security</c>.
/// </summary>
public interface IDownloadService
{
    Task<Result<string>> DownloadVerifiedAsync(
        string url,
        string expectedSha256,
        string destinationPath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}
