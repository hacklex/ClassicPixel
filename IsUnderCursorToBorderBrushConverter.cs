using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace PixelEditor;

public class IsUnderCursorToBorderBrushConverter : IValueConverter
{
    public static IsUnderCursorToBorderBrushConverter Instance { get; } = new();
    
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not (bool isActive and true))
            return AvaloniaProperty.UnsetValue;
        return Brushes.Blue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return AvaloniaProperty.UnsetValue;
    }
}