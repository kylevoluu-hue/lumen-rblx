namespace Lumen.UI.Theming;

/// <summary>
/// Applies cosmetic theme choices (currently the accent colour) to the active UI head. Each UI
/// framework provides its own implementation; view-models depend only on this abstraction, so the
/// same view-models drive the Avalonia and WinUI heads.
/// </summary>
public interface IThemeApplier
{
    void ApplyAccent(string? hex);
}
