using System.Text.Json;
using System.Text.Json.Serialization;
using Lumen.Core.Common;
using Lumen.Core.Configuration;

namespace Lumen.Storage;

/// <summary>
/// <see cref="IVersionedJsonStore"/> backed by human-readable JSON files with atomic writes,
/// automatic backups, and corruption recovery. If a file is missing, a default is created; if a
/// file is present but unparseable, it is quarantined (never deleted) and a default is returned,
/// so the application always starts from a valid state and the user never loses data silently.
/// </summary>
public sealed class JsonFileStore : IVersionedJsonStore
{
    public static readonly JsonSerializerOptions SerializerOptions = CreateOptions();

    public async Task<Result<T>> LoadAsync<T>(
        string filePath, Func<T> createDefault, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(createDefault);

        try
        {
            if (!File.Exists(filePath))
            {
                var created = createDefault();
                var saved = await SaveAsync(filePath, created, cancellationToken).ConfigureAwait(false);
                return saved.IsSuccess ? Result.Success(created) : Result.Failure<T>(saved.Error!);
            }

            var json = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);

            T? value = null;
            try
            {
                value = JsonSerializer.Deserialize<T>(json, SerializerOptions);
            }
            catch (JsonException)
            {
                // Fall through to recovery below.
            }

            if (value is null)
            {
                QuarantineCorruptFile(filePath);
                var recovered = createDefault();
                await SaveAsync(filePath, recovered, cancellationToken).ConfigureAwait(false);
                return Result.Success(recovered);
            }

            return Result.Success(value);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Result.Failure<T>($"Could not read '{Path.GetFileName(filePath)}': {ex.Message}");
        }
    }

    public async Task<Result> SaveAsync<T>(string filePath, T value, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(value);

        try
        {
            var json = JsonSerializer.Serialize(value, SerializerOptions);
            await AtomicFile.WriteAllTextAsync(filePath, json, backup: true, cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return Result.Failure($"Could not save '{Path.GetFileName(filePath)}': {ex.Message}");
        }
    }

    /// <summary>Moves an unparseable file aside so it is preserved for inspection but not reused.</summary>
    private static void QuarantineCorruptFile(string filePath)
    {
        var quarantinePath = $"{filePath}.corrupt-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}";
        try
        {
            File.Move(filePath, quarantinePath, overwrite: false);
        }
        catch (IOException)
        {
            // If the move fails we still return a default; the corrupt file will be overwritten
            // by the subsequent atomic save (which backs it up first).
        }
    }

    private static JsonSerializerOptions CreateOptions() => new(JsonSerializerDefaults.General)
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };
}
