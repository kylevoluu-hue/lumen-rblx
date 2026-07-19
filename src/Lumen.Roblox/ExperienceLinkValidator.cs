using System.Text.RegularExpressions;
using Lumen.Core.Abstractions;
using Lumen.Core.Common;
using Lumen.Core.Models;
using Lumen.Security;

namespace Lumen.Roblox;

/// <summary>
/// Validates a place id, official experience URL, or private-server link into a safe
/// <see cref="ExperienceTarget"/>. Rejects control characters, non-Roblox hosts, non-HTTPS URLs,
/// and malformed codes. The private-server code is validated against a strict character set and
/// carried in a field that is never logged or serialized.
/// </summary>
public sealed partial class ExperienceLinkValidator : IExperienceLinkValidator
{
    // Roblox place ids are positive and comfortably fit in Int64; cap the digit count defensively.
    private const int MaxPlaceIdDigits = 19;

    public Result<ExperienceTarget> Validate(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Result.Failure<ExperienceTarget>("Enter a place id or experience link.");
        }

        input = input.Trim();

        if (ProcessArguments.HasControlCharacters(input))
        {
            return Result.Failure<ExperienceTarget>("The link contains invalid characters.");
        }

        // Bare numeric place id.
        if (input.All(char.IsDigit))
        {
            if (input.Length > MaxPlaceIdDigits || !long.TryParse(input, out var bareId) || bareId <= 0)
            {
                return Result.Failure<ExperienceTarget>("That is not a valid place id.");
            }

            return Result.Success(new ExperienceTarget { Kind = ExperienceLinkKind.PlaceId, PlaceId = bareId });
        }

        if (!Uri.TryCreate(input, UriKind.Absolute, out var uri))
        {
            return Result.Failure<ExperienceTarget>("Enter a numeric place id or a full https link.");
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal))
        {
            return Result.Failure<ExperienceTarget>("Only official https Roblox links are supported.");
        }

        if (!IsRobloxHost(uri.Host))
        {
            return Result.Failure<ExperienceTarget>("That link is not an official roblox.com link.");
        }

        long? placeId = TryExtractPlaceId(uri.AbsolutePath);
        var query = ParseQuery(uri.Query);

        if (query.TryGetValue("privateServerLinkCode", out var code) || query.TryGetValue("code", out code))
        {
            if (!PrivateCodeRegex().IsMatch(code))
            {
                return Result.Failure<ExperienceTarget>("The private-server code is malformed.");
            }

            return Result.Success(new ExperienceTarget
            {
                Kind = ExperienceLinkKind.PrivateServer,
                PlaceId = placeId,
                PrivateServerCode = code,
            });
        }

        if (placeId is null)
        {
            return Result.Failure<ExperienceTarget>("Could not find a place id in that link.");
        }

        return Result.Success(new ExperienceTarget { Kind = ExperienceLinkKind.ExperienceUrl, PlaceId = placeId });
    }

    private static bool IsRobloxHost(string host) =>
        host.Equals("roblox.com", StringComparison.OrdinalIgnoreCase) ||
        host.EndsWith(".roblox.com", StringComparison.OrdinalIgnoreCase);

    private static long? TryExtractPlaceId(string absolutePath)
    {
        var segments = absolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < segments.Length - 1; i++)
        {
            if (segments[i].Equals("games", StringComparison.OrdinalIgnoreCase) &&
                long.TryParse(segments[i + 1], out var id) && id > 0)
            {
                return id;
            }
        }

        return null;
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(query))
        {
            return result;
        }

        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = pair.IndexOf('=');
            if (eq <= 0)
            {
                continue;
            }

            var key = Uri.UnescapeDataString(pair[..eq]);
            var value = Uri.UnescapeDataString(pair[(eq + 1)..]);
            result[key] = value;
        }

        return result;
    }

    [GeneratedRegex(@"^[A-Za-z0-9_-]{1,128}$")]
    private static partial Regex PrivateCodeRegex();
}
