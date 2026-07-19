namespace Lumen.Core.Common;

/// <inheritdoc />
public sealed class LumenPaths : ILumenPaths
{
    public LumenPaths(string root, bool isPortable)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new ArgumentException("Root path must be provided.", nameof(root));
        }

        Root = Path.GetFullPath(root);
        IsPortable = isPortable;
    }

    public string Root { get; }

    public bool IsPortable { get; }

    public string Config => Path.Combine(Root, "config");

    public string Logs => Path.Combine(Root, "logs");

    public string Profiles => Path.Combine(Root, "profiles");

    public string Mods => Path.Combine(Root, "mods");

    public string Backups => Path.Combine(Root, "backups");

    public string Accounts => Path.Combine(Root, "accounts");

    public string Cache => Path.Combine(Root, "cache");

    /// <summary>
    /// Standard installation: data lives in the per-user application-data folder.
    /// On Windows this resolves to <c>%APPDATA%\Lumen</c>.
    /// </summary>
    public static LumenPaths CreateDefault() =>
        new(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            LumenInfo.Name), isPortable: false);

    /// <summary>Portable installation: data lives in a <c>LumenData</c> folder next to the executable.</summary>
    public static LumenPaths CreatePortable() =>
        new(Path.Combine(AppContext.BaseDirectory, "LumenData"), isPortable: true);

    public void EnsureCreated()
    {
        foreach (var dir in new[] { Root, Config, Logs, Profiles, Mods, Backups, Accounts, Cache })
        {
            Directory.CreateDirectory(dir);
        }
    }
}
