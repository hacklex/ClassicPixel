using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using PixelEditor.ViewModels;

namespace PixelEditor;

public class ToolTypeConverter : IValueConverter
{
    public static ToolTypeConverter Instance { get; } = new();
        
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ToolType selectedTool || parameter is not string toolTypeString)
            return false;
            
        if (Enum.TryParse<ToolType>(toolTypeString, out var toolType))
        {
            return selectedTool == toolType;
        }
            
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool isSelected || !isSelected || parameter is not string toolTypeString)
            return null;

        if (!isSelected) return AvaloniaProperty.UnsetValue;
            
        if (Enum.TryParse<ToolType>(toolTypeString, out var toolType))
        {
            return toolType;
        }
            
        return null;
    }
}

public class GeometryDataConverter : IValueConverter
{
    public static GeometryDataConverter Instance { get; } = new();
        
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return AvaloniaProperty.UnsetValue;
        if (targetType == typeof(Geometry) && value is string str)
            return Geometry.Parse(str);

        if (value is Geometry g && targetType == typeof(string))
            return g.ToString() ?? AvaloniaProperty.UnsetValue;
        return AvaloniaProperty.UnsetValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str) return Geometry.Parse(str);
        if (value is Geometry g) return g.ToString();
        return AvaloniaProperty.UnsetValue;
    }
}