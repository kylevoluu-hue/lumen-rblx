using Lumen.UI.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Lumen.WinUI.Theming;

/// <summary>WinUI 3 implementation of <see cref="IThemeApplier"/>: updates live app resources.</summary>
public sealed class WinUIThemeApplier : IThemeApplier
{
    public void ApplyAccent(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex) || Application.Current?.Resources is not { } resources || !TryParseHex(hex, out var color))
        {
            return;
        }

        resources["SystemAccentColor"] = color;
        resources["LumenAccentBrush"] = new SolidColorBrush(color);
    }

    private static bool TryParseHex(string hex, out Color color)
    {
        color = default;
        var value = hex.TrimStart('#');
        if (value.Length != 6)
        {
            return false;
        }

        try
        {
            color = Color.FromArgb(
                255,
                System.Convert.ToByte(value.Substring(0, 2), 16),
                System.Convert.ToByte(value.Substring(2, 2), 16),
                System.Convert.ToByte(value.Substring(4, 2), 16));
            return true;
        }
        catch (System.FormatException)
        {
            return false;
        }
    }
}
