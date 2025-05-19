using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace PixelEditorApp;

public class ColorToBrushConverter : IValueConverter
{
    //Implement it with caching
    public static Dictionary<Color, Brush> BrushCache = [];
    public static Brush GetBrush(Color color) => 
        BrushCache.TryGetValue(color, out var brush) 
            ? brush
            : BrushCache[color] = new SolidColorBrush(color);
    
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Color color)
            return GetBrush(color);
        return AvaloniaProperty.UnsetValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SolidColorBrush brush)
            return brush.Color;
        return AvaloniaProperty.UnsetValue;
    }
}