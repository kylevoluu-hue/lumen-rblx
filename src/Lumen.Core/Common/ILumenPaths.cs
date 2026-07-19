namespace Lumen.Core.Common;

/// <summary>
/// Resolves the well-known local directories Lumen uses. All Lumen data lives under a
/// single root so the Privacy Center can show, export, or delete everything in one place.
/// Nothing here is ever transmitted off the device.
/// </summary>
public interface ILumenPaths
{
    /// <summary>Root data directory (e.g. <c>%APPDATA%\Lumen</c>, or a portable folder next to the executable).</summary>
    string Root { get; }

    string Config { get; }

    string Logs { get; }

    string Profiles { get; }

    string Mods { get; }

    string Backups { get; }

    string Accounts { get; }

    string Cache { get; }

    /// <summary>True when running in Portable Mode (data stored beside the executable, no roaming profile writes).</summary>
    bool IsPortable { get; }

    /// <summary>Creates the directory tree if it does not exist. Idempotent.</summary>
    void EnsureCreated();
}
