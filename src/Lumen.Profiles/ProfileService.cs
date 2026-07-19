using System.Text.Json;
using Lumen.Core.Abstractions;
using Lumen.Core.Common;
using Lumen.Core.Configuration;
using Lumen.Core.Models;
using Lumen.Storage;

namespace Lumen.Profiles;

/// <summary>Serializable container for the user's profiles.</summary>
public sealed class ProfileCollection
{
    public int SchemaVersion { get; set; } = 1;

    public List<LumenProfile> Profiles { get; set; } = new();
}

/// <summary>
/// Manages profiles with sanitized import/export. Exported profiles never carry account linkage
/// or secrets (there are none in the model, and account references are stripped), so a profile
/// can be shared safely.
/// </summary>
public sealed class ProfileService : IProfileService, IDisposable
{
    private readonly IVersionedJsonStore _store;
    private readonly ILumenPaths _paths;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private ProfileCollection _collection = new();

    public ProfileService(IVersionedJsonStore store, ILumenPaths paths)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
    }

    public IReadOnlyList<LumenProfile> Profiles => _collection.Profiles;

    private string FilePath => Path.Combine(_paths.Profiles, "profiles.json");

    public async Task<Result> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var result = await _store.LoadAsync(FilePath, static () => new ProfileCollection(), cancellationToken)
                .ConfigureAwait(false);
            if (result.IsFailure)
            {
                return Result.Failure(result.Error!);
            }

            _collection = result.Value!;
            return Result.Success();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<Result<LumenProfile>> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<LumenProfile>("Enter a profile name.");
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var profile = new LumenProfile { Id = Guid.NewGuid().ToString("N"), Name = name.Trim() };
            _collection.Profiles.Add(profile);
            var saved = await SaveInternalAsync(cancellationToken).ConfigureAwait(false);
            return saved.IsSuccess ? Result.Success(profile) : Result.Failure<LumenProfile>(saved.Error!);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<Result> SaveAsync(LumenProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var index = _collection.Profiles.FindIndex(p => p.Id == profile.Id);
            if (index < 0)
            {
                _collection.Profiles.Add(profile);
            }
            else
            {
                _collection.Profiles[index] = profile;
            }

            return await SaveInternalAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return _collection.Profiles.RemoveAll(p => p.Id == id) > 0
                ? await SaveInternalAsync(cancellationToken).ConfigureAwait(false)
                : Result.Failure("Profile not found.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<Result<string>> ExportAsync(string id, string destinationPath, CancellationToken cancellationToken = default)
    {
        var profile = _collection.Profiles.FirstOrDefault(p => p.Id == id);
        if (profile is null)
        {
            return Result.Failure<string>("Profile not found.");
        }

        // Sanitize: strip account linkage. The model contains no secrets by design.
        var sanitized = Sanitize(profile);

        try
        {
            var json = JsonSerializer.Serialize(sanitized, JsonFileStore.SerializerOptions);
            await AtomicFile.WriteAllTextAsync(destinationPath, json, backup: false, cancellationToken).ConfigureAwait(false);
            return Result.Success(destinationPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Result.Failure<string>($"Could not export profile: {ex.Message}");
        }
    }

    public async Task<Result<LumenProfile>> ImportAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sourcePath))
        {
            return Result.Failure<LumenProfile>("Profile file not found.");
        }

        LumenProfile? incoming;
        try
        {
            var json = await File.ReadAllTextAsync(sourcePath, cancellationToken).ConfigureAwait(false);
            incoming = JsonSerializer.Deserialize<LumenProfile>(json, JsonFileStore.SerializerOptions);
        }
        catch (JsonException ex)
        {
            return Result.Failure<LumenProfile>($"Profile file is not valid: {ex.Message}");
        }

        if (incoming is null || string.IsNullOrWhiteSpace(incoming.Name))
        {
            return Result.Failure<LumenProfile>("Profile file is missing required fields.");
        }

        // Assign a fresh id, drop any account linkage and default flag from the imported copy.
        var imported = Sanitize(incoming, newId: Guid.NewGuid().ToString("N"));
        imported.IsDefault = false;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _collection.Profiles.Add(imported);
            var saved = await SaveInternalAsync(cancellationToken).ConfigureAwait(false);
            return saved.IsSuccess ? Result.Success(imported) : Result.Failure<LumenProfile>(saved.Error!);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Produces a copy with account linkage removed (and optionally a new id).</summary>
    private static LumenProfile Sanitize(LumenProfile source, string? newId = null) => new()
    {
        Id = newId ?? source.Id,
        Name = source.Name,
        Description = source.Description,
        AccountId = null, // never export account linkage
        ResolutionId = source.ResolutionId,
        FpsLimit = source.FpsLimit,
        GraphicsPresetId = source.GraphicsPresetId,
        PerformancePresetId = source.PerformancePresetId,
        VisualFilterId = source.VisualFilterId,
        CursorId = source.CursorId,
        CrosshairId = source.CrosshairId,
        ModPackId = source.ModPackId,
        OverlayLayoutId = source.OverlayLayoutId,
        ThemeId = source.ThemeId,
        IsDefault = source.IsDefault,
    };

    private Task<Result> SaveInternalAsync(CancellationToken cancellationToken) =>
        _store.SaveAsync(FilePath, _collection, cancellationToken);

    public void Dispose() => _gate.Dispose();
}
