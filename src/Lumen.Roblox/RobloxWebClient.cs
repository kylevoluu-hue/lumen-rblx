using System.Net.Http.Json;
using System.Text.Json;
using Lumen.Core.Abstractions;
using Lumen.Core.Common;
using Lumen.Core.Models;
using Lumen.Security;

namespace Lumen.Roblox;

/// <summary>
/// Reads PUBLIC Roblox data via official roblox.com web APIs. Every URL is checked against the
/// HTTPS host allowlist before a request is made; no cookies or credentials are ever sent. Network
/// failures return a failure result rather than throwing.
/// </summary>
public sealed class RobloxWebClient : IRobloxWebClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;
    private readonly UrlValidator _urls;

    public RobloxWebClient(HttpClient http, UrlValidator urls)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _urls = urls ?? throw new ArgumentNullException(nameof(urls));
    }

    public async Task<Result<long>> ResolveUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return Result.Failure<long>("Enter a Roblox username.");
        }

        const string url = "https://users.roblox.com/v1/usernames/users";
        if (_urls.Validate(url).IsFailure)
        {
            return Result.Failure<long>("Blocked request to a non-allowlisted host.");
        }

        try
        {
            var payload = new { usernames = new[] { username.Trim() }, excludeBannedUsers = true };
            using var response = await _http.PostAsJsonAsync(url, payload, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return Result.Failure<long>($"Roblox returned {(int)response.StatusCode}.");
            }

            var body = await response.Content.ReadFromJsonAsync<UsernameLookup>(JsonOptions, cancellationToken).ConfigureAwait(false);
            var first = body?.Data?.FirstOrDefault();
            return first is null
                ? Result.Failure<long>("No Roblox user with that username.")
                : Result.Success(first.Id);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or NotSupportedException)
        {
            return Result.Failure<long>($"Could not reach Roblox: {ex.Message}");
        }
    }

    public async Task<Result<RobloxUser>> GetUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        var result = await GetJsonAsync<UserDto>($"https://users.roblox.com/v1/users/{userId}", cancellationToken)
            .ConfigureAwait(false);
        if (result.IsFailure)
        {
            return Result.Failure<RobloxUser>(result.Error!);
        }

        var dto = result.Value!;
        return Result.Success(new RobloxUser(
            dto.Id,
            dto.Name ?? string.Empty,
            string.IsNullOrEmpty(dto.DisplayName) ? dto.Name ?? string.Empty : dto.DisplayName!,
            dto.Description,
            dto.Created));
    }

    public async Task<Result<IReadOnlyList<RobloxFriend>>> GetFriendsAsync(long userId, CancellationToken cancellationToken = default)
    {
        var result = await GetJsonAsync<FriendList>($"https://friends.roblox.com/v1/users/{userId}/friends", cancellationToken)
            .ConfigureAwait(false);
        if (result.IsFailure)
        {
            return Result.Failure<IReadOnlyList<RobloxFriend>>(result.Error!);
        }

        var friends = (result.Value!.Data ?? new List<FriendDto>())
            .Select(f => new RobloxFriend(f.Id, f.Name ?? string.Empty,
                string.IsNullOrEmpty(f.DisplayName) ? f.Name ?? string.Empty : f.DisplayName!))
            .ToList();

        return Result.Success<IReadOnlyList<RobloxFriend>>(friends);
    }

    public async Task<Result<string>> GetAvatarHeadshotUrlAsync(long userId, CancellationToken cancellationToken = default)
    {
        var url = $"https://thumbnails.roblox.com/v1/users/avatar-headshot?userIds={userId}&size=150x150&format=Png&isCircular=false";
        var result = await GetJsonAsync<ThumbnailList>(url, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return Result.Failure<string>(result.Error!);
        }

        var imageUrl = result.Value!.Data?.FirstOrDefault()?.ImageUrl;
        return string.IsNullOrEmpty(imageUrl)
            ? Result.Failure<string>("No avatar available.")
            : Result.Success(imageUrl!);
    }

    public async Task<Result<string>> GetExperienceNameAsync(long placeId, CancellationToken cancellationToken = default)
    {
        var universe = await GetJsonAsync<UniverseDto>(
            $"https://apis.roblox.com/universes/v1/places/{placeId}/universe", cancellationToken).ConfigureAwait(false);
        if (universe.IsFailure)
        {
            return Result.Failure<string>(universe.Error!);
        }

        var games = await GetJsonAsync<GameList>(
            $"https://games.roblox.com/v1/games?universeIds={universe.Value!.UniverseId}", cancellationToken).ConfigureAwait(false);
        if (games.IsFailure)
        {
            return Result.Failure<string>(games.Error!);
        }

        var name = games.Value!.Data?.FirstOrDefault()?.Name;
        return string.IsNullOrEmpty(name)
            ? Result.Failure<string>("Experience name not found.")
            : Result.Success(name!);
    }

    public async Task<Result<byte[]>> GetImageAsync(string url, CancellationToken cancellationToken = default)
    {
        if (_urls.Validate(url).IsFailure)
        {
            return Result.Failure<byte[]>("Blocked image request to a non-allowlisted host.");
        }

        try
        {
            var bytes = await _http.GetByteArrayAsync(url, cancellationToken).ConfigureAwait(false);
            return Result.Success(bytes);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return Result.Failure<byte[]>($"Could not load image: {ex.Message}");
        }
    }

    private async Task<Result<T>> GetJsonAsync<T>(string url, CancellationToken cancellationToken)
    {
        if (_urls.Validate(url).IsFailure)
        {
            return Result.Failure<T>("Blocked request to a non-allowlisted host.");
        }

        try
        {
            using var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return Result.Failure<T>($"Roblox returned {(int)response.StatusCode}.");
            }

            var body = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken).ConfigureAwait(false);
            return body is null ? Result.Failure<T>("Empty response from Roblox.") : Result.Success(body);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or NotSupportedException)
        {
            return Result.Failure<T>($"Could not reach Roblox: {ex.Message}");
        }
    }

    // --- Internal DTOs for the Roblox JSON responses ---
    private sealed record UsernameLookup(List<UsernameEntry>? Data);

    private sealed record UsernameEntry(long Id, string? Name, string? DisplayName);

    private sealed record UserDto(long Id, string? Name, string? DisplayName, string? Description, DateTimeOffset? Created);

    private sealed record FriendList(List<FriendDto>? Data);

    private sealed record FriendDto(long Id, string? Name, string? DisplayName);

    private sealed record ThumbnailList(List<ThumbnailDto>? Data);

    private sealed record ThumbnailDto(long TargetId, string? State, string? ImageUrl);

    private sealed record UniverseDto(long UniverseId);

    private sealed record GameList(List<GameDto>? Data);

    private sealed record GameDto(string? Name);
}
