using System.Text.Json;
using Lumen.Core.Abstractions;
using Lumen.Core.Common;
using Lumen.Core.Models;
using Lumen.Storage;

namespace Lumen.Roblox;

/// <summary>
/// Writes the supported Roblox <c>ClientAppSettings.json</c> override file for allowlisted
/// FastFlags. Values are validated and range-checked, the exact file content is previewable, and
/// writes are atomic and backed up. This is the same supported override mechanism the wider
/// community uses — Lumen never patches Roblox memory and never offers unsafe flags.
/// </summary>
public sealed class FastFlagManager : IFastFlagManager
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly IRobloxInstallationLocator _locator;

    public FastFlagManager(IRobloxInstallationLocator locator)
    {
        _locator = locator ?? throw new ArgumentNullException(nameof(locator));
    }

    public IReadOnlyList<FastFlagDefinition> Allowlist => FastFlagAllowlist.All;

    public Result<string> BuildPreview(IReadOnlyDictionary<string, string> values)
    {
        var built = BuildValidated(values);
        return built.IsFailure
            ? Result.Failure<string>(built.Error!)
            : Result.Success(JsonSerializer.Serialize(built.Value!, JsonOptions));
    }

    public async Task<Result<string>> ApplyAsync(
        IReadOnlyDictionary<string, string> values, CancellationToken cancellationToken = default)
    {
        var built = BuildValidated(values);
        if (built.IsFailure)
        {
            return Result.Failure<string>(built.Error!);
        }

        var path = await ResolveSettingsPathAsync(cancellationToken).ConfigureAwait(false);
        if (path.IsFailure)
        {
            return Result.Failure<string>(path.Error!);
        }

        try
        {
            var json = JsonSerializer.Serialize(built.Value!, JsonOptions);
            await AtomicFile.WriteAllTextAsync(path.Value!, json, backup: true, cancellationToken).ConfigureAwait(false);
            return Result.Success(path.Value!);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Result.Failure<string>($"Could not write FastFlags: {ex.Message}");
        }
    }

    public async Task<Result<Dictionary<string, string>>> ReadCurrentAsync(CancellationToken cancellationToken = default)
    {
        var path = await ResolveSettingsPathAsync(cancellationToken).ConfigureAwait(false);
        if (path.IsFailure)
        {
            return Result.Failure<Dictionary<string, string>>(path.Error!);
        }

        var current = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!File.Exists(path.Value!))
        {
            return Result.Success(current);
        }

        try
        {
            var json = await File.ReadAllTextAsync(path.Value!, cancellationToken).ConfigureAwait(false);
            var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (parsed is not null)
            {
                // Only surface flags we recognise (ignore anything else already in the file).
                foreach (var (name, value) in parsed)
                {
                    if (FastFlagAllowlist.Find(name) is not null)
                    {
                        current[name] = value;
                    }
                }
            }

            return Result.Success(current);
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            return Result.Failure<Dictionary<string, string>>($"Could not read current FastFlags: {ex.Message}");
        }
    }

    public async Task<Result> RestoreDefaultsAsync(CancellationToken cancellationToken = default)
    {
        var path = await ResolveSettingsPathAsync(cancellationToken).ConfigureAwait(false);
        if (path.IsFailure)
        {
            return Result.Failure(path.Error!);
        }

        try
        {
            await AtomicFile.WriteAllTextAsync(path.Value!, "{}", backup: true, cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Result.Failure($"Could not restore FastFlags: {ex.Message}");
        }
    }

    /// <summary>Validates every value against the allowlist; unknown names and out-of-range values are rejected.</summary>
    private static Result<SortedDictionary<string, string>> BuildValidated(IReadOnlyDictionary<string, string> values)
    {
        var normalized = new SortedDictionary<string, string>(StringComparer.Ordinal);

        foreach (var (name, rawValue) in values)
        {
            var definition = FastFlagAllowlist.Find(name);
            if (definition is null)
            {
                return Result.Failure<SortedDictionary<string, string>>($"'{name}' is not an allowlisted FastFlag.");
            }

            var value = definition.Normalize(rawValue);
            if (value.IsFailure)
            {
                return Result.Failure<SortedDictionary<string, string>>(value.Error!);
            }

            normalized[name] = value.Value!;
        }

        return Result.Success(normalized);
    }

    private async Task<Result<string>> ResolveSettingsPathAsync(CancellationToken cancellationToken)
    {
        var install = await _locator.LocateAsync(RobloxProduct.Player, cancellationToken).ConfigureAwait(false);
        if (!install.IsUsable || string.IsNullOrEmpty(install.InstallPath))
        {
            return Result.Failure<string>(
                "No usable Roblox installation was detected. FastFlags apply on Windows with Roblox installed.");
        }

        var versionDirectory = Path.GetDirectoryName(install.InstallPath);
        if (string.IsNullOrEmpty(versionDirectory))
        {
            return Result.Failure<string>("Could not resolve the Roblox installation folder.");
        }

        var clientSettings = Path.Combine(versionDirectory, "ClientSettings");
        Directory.CreateDirectory(clientSettings);
        return Result.Success(Path.Combine(clientSettings, "ClientAppSettings.json"));
    }
}
