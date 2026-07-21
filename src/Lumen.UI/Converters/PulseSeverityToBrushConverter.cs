using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Lumen.Core.Models;

namespace Lumen.UI.Converters;

/// <summary>Maps a <see cref="PulseSeverity"/> to a status colour for the Pulse view.</summary>
public sealed class PulseSeverityToBrushConverter : IValueConverter
{
    public static readonly PulseSeverityToBrushConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        new SolidColorBrush(value is PulseSeverity severity
            ? severity switch
            {
                PulseSeverity.Ok => Color.Parse("#3FB984"),
                PulseSeverity.Info => Color.Parse("#6C8CFF"),
                PulseSeverity.Warning => Color.Parse("#E9A23B"),
                PulseSeverity.Critical => Color.Parse("#F0668A"),
                _ => Color.Parse("#8595B2"),
            }
            : Color.Parse("#8595B2"));

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
