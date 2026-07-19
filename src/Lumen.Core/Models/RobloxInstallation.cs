namespace Lumen.Core.Models;

public enum RobloxProduct
{
    Player = 0,
    Studio = 1,
}

public enum RobloxInstallationState
{
    /// <summary>Detection could not run on this operating system (Lumen targets Windows for launching).</summary>
    PlatformUnsupported = 0,

    /// <summary>No official Roblox installation was found.</summary>
    NotDetected = 1,

    Healthy = 2,

    /// <summary>Installation found but files are missing or corrupt.</summary>
    Broken = 3,

    UpdateAvailable = 4,
}

/// <summary>
/// Describes a detected official Roblox installation. Lumen never bundles or downloads Roblox
/// binaries; it only locates and launches an existing official install.
/// </summary>
public sealed record RobloxInstallation(
    RobloxProduct Product,
    RobloxInstallationState State,
    string? Version,
    string? InstallPath,
    string? Detail)
{
    public bool IsUsable => State is RobloxInstallationState.Healthy or RobloxInstallationState.UpdateAvailable;

    public static RobloxInstallation Unsupported(RobloxProduct product) =>
        new(product, RobloxInstallationState.PlatformUnsupported, null, null,
            "Installation detection and launching are only available on Windows.");

    public static RobloxInstallation NotFound(RobloxProduct product) =>
        new(product, RobloxInstallationState.NotDetected, null, null, "No official Roblox installation detected.");
}
