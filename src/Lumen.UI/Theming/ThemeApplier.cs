using Avalonia;
using Avalonia.Media;

namespace Lumen.UI.Theming;

/// <summary>Applies cosmetic theme choices (currently the accent colour) to the live app resources.</summary>
public static class ThemeApplier
{
    public static void ApplyAccent(string? hex)
    {
        if (Application.Current is null || string.IsNullOrWhiteSpace(hex) || !Color.TryParse(hex, out var color))
        {
            return;
        }

        var resources = Application.Current.Resources;
        resources["LumenAccentBrush"] = new SolidColorBrush(color);
        resources["LumenAccentHoverBrush"] = new SolidColorBrush(Adjust(color, 0.12));
        resources["LumenAccentPressedBrush"] = new SolidColorBrush(Adjust(color, -0.10));
        resources["SystemAccentColor"] = color;
    }

    private static Color Adjust(Color color, double factor)
    {
        // Positive factor lightens toward white, negative darkens toward black.
        static byte Clamp(double v) => (byte)Math.Clamp(v, 0, 255);

        if (factor >= 0)
        {
            return Color.FromRgb(
                Clamp(color.R + ((255 - color.R) * factor)),
                Clamp(color.G + ((255 - color.G) * factor)),
                Clamp(color.B + ((255 - color.B) * factor)));
        }

        var f = 1 + factor;
        return Color.FromRgb(Clamp(color.R * f), Clamp(color.G * f), Clamp(color.B * f));
    }
}
