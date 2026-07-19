using Lumen.Core.Common;

namespace Lumen.Resolution;

public enum DisplayMode
{
    Windowed,
    Borderless,
    Fullscreen,
}

/// <summary>A display resolution with helpers for aspect-ratio reasoning and validation.</summary>
public sealed record DisplayResolution(int Width, int Height)
{
    public double AspectRatio => Height == 0 ? 0 : (double)Width / Height;

    public bool IsValid => Width is >= 640 and <= 15360 && Height is >= 480 and <= 8640;

    public override string ToString() => $"{Width}×{Height}";
}

/// <summary>The common resolution presets from spec §12. Real, validated data.</summary>
public static class ResolutionPresets
{
    public static IReadOnlyList<DisplayResolution> Common { get; } = new[]
    {
        new DisplayResolution(1024, 768),
        new DisplayResolution(1280, 720),
        new DisplayResolution(1280, 800),
        new DisplayResolution(1366, 768),
        new DisplayResolution(1440, 900),
        new DisplayResolution(1600, 900),
        new DisplayResolution(1680, 1050),
        new DisplayResolution(1920, 1080),
        new DisplayResolution(1920, 1200),
        new DisplayResolution(2560, 1080),
        new DisplayResolution(2560, 1440),
        new DisplayResolution(3440, 1440),
        new DisplayResolution(3840, 2160),
    };

    public static Result<DisplayResolution> Validate(int width, int height)
    {
        var resolution = new DisplayResolution(width, height);
        return resolution.IsValid
            ? Result.Success(resolution)
            : Result.Failure<DisplayResolution>("Resolution is outside the supported range.");
    }
}

/// <summary>
/// Detects monitors and applies windowed/borderless/fullscreen sizing and positioning (spec §12).
///
/// PHASE 5 (roadmap): monitor enumeration and window placement are Windows-runtime work, deferred
/// from this foundation milestone. The preset data and validation above are fully implemented.
/// </summary>
public interface IResolutionManager
{
    IReadOnlyList<DisplayResolution> DetectMonitors();
}
