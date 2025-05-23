using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace PixelEditor;

public class IsActiveToBgBrushConverter : IValueConverter
{
    public static IsActiveToBgBrushConverter Instance { get; } = new();
    
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool isActive)
            return AvaloniaProperty.UnsetValue;
        return !isActive ? Brushes.Gray : Brushes.DarkBlue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return AvaloniaProperty.UnsetValue;
    }
}