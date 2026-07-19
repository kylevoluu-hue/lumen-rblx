using Lumen.Core.Common;

namespace Lumen.Security;

/// <summary>
/// Validates outbound URLs against an HTTPS-only host allowlist. Every network request Lumen
/// makes must pass through here, so the Network Activity Viewer can guarantee no request goes
/// to an undisclosed destination. Rejects non-HTTPS schemes, embedded credentials, malformed
/// URLs, and hosts outside the allowlist.
/// </summary>
public sealed class UrlValidator
{
    private readonly HashSet<string> _allowedHosts;

    public UrlValidator(IEnumerable<string> allowedHosts)
    {
        _allowedHosts = new HashSet<string>(allowedHosts, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Default allowlist: official Roblox domains (site + avatar CDN) and the Lumen release host
    /// used for update checks. Subdomains of these are permitted; everything else is rejected.
    /// </summary>
    public static UrlValidator CreateDefault() => new(new[]
    {
        "roblox.com",
        "rbxcdn.com",
        "api.github.com",
        "github.com",
        "objects.githubusercontent.com",
    });

    public IReadOnlyCollection<string> AllowedHosts => _allowedHosts;

    public Result<Uri> Validate(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return Result.Failure<Uri>("URL is empty.");
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return Result.Failure<Uri>("URL is malformed.");
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal))
        {
            return Result.Failure<Uri>("Only HTTPS URLs are allowed.");
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            return Result.Failure<Uri>("URLs containing embedded credentials are not allowed.");
        }

        if (uri.HostNameType is not (UriHostNameType.Dns or UriHostNameType.IPv4 or UriHostNameType.IPv6))
        {
            return Result.Failure<Uri>("URL host is not a valid DNS name.");
        }

        if (!IsHostAllowed(uri.Host))
        {
            return Result.Failure<Uri>($"Host '{uri.Host}' is not on the allowlist.");
        }

        return Result.Success(uri);
    }

    public bool IsAllowed(string url) => Validate(url).IsSuccess;

    private bool IsHostAllowed(string host)
    {
        if (_allowedHosts.Contains(host))
        {
            return true;
        }

        // Allow subdomains of allowlisted registrable domains (e.g. "www.roblox.com").
        foreach (var allowed in _allowedHosts)
        {
            if (host.EndsWith("." + allowed, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
