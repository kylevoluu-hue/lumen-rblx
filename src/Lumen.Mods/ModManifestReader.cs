using System.Text.Json;
using System.Text.RegularExpressions;
using Lumen.Core.Common;
using Lumen.Core.Models;
using Lumen.Storage;

namespace Lumen.Mods;

/// <summary>
/// Parses and validates a <c>manifest.json</c>. The manifest is pure declarative data; parsing
/// never executes anything. Validation enforces a supported format version, a well-formed id and
/// version, and required fields, so a malformed manifest is rejected before a package is trusted.
/// </summary>
public static partial class ModManifestReader
{
    public const int SupportedFormatVersion = 1;

    public static Result<ModManifest> Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Result.Failure<ModManifest>("Manifest is empty.");
        }

        ModManifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<ModManifest>(json, JsonFileStore.SerializerOptions);
        }
        catch (JsonException ex)
        {
            return Result.Failure<ModManifest>($"Manifest is not valid JSON: {ex.Message}");
        }

        if (manifest is null)
        {
            return Result.Failure<ModManifest>("Manifest could not be read.");
        }

        return Validate(manifest);
    }

    public static async Task<Result<ModManifest>> ReadFromFileAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
        {
            return Result.Failure<ModManifest>("Package is missing manifest.json.");
        }

        var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        return Parse(json);
    }

    public static Result<ModManifest> Validate(ModManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        if (manifest.FormatVersion != SupportedFormatVersion)
        {
            return Result.Failure<ModManifest>(
                $"Unsupported manifest format version {manifest.FormatVersion} (expected {SupportedFormatVersion}).");
        }

        if (string.IsNullOrWhiteSpace(manifest.Id) || !IdRegex().IsMatch(manifest.Id))
        {
            return Result.Failure<ModManifest>("Manifest id is missing or malformed (expected e.g. 'author.mod-name').");
        }

        if (string.IsNullOrWhiteSpace(manifest.Name) || manifest.Name.Length > 100)
        {
            return Result.Failure<ModManifest>("Manifest name is missing or too long.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Author))
        {
            return Result.Failure<ModManifest>("Manifest author is required.");
        }

        if (!VersionRegex().IsMatch(manifest.Version))
        {
            return Result.Failure<ModManifest>("Manifest version must be semantic (e.g. '1.0.0').");
        }

        return Result.Success(manifest);
    }

    [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9._-]{1,99}$")]
    private static partial Regex IdRegex();

    [GeneratedRegex(@"^\d+\.\d+\.\d+([.-][0-9A-Za-z.-]+)?$")]
    private static partial Regex VersionRegex();
}
