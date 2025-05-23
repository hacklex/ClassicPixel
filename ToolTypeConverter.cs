using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using PixelEditor.ViewModels;

namespace PixelEditor
{
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
}
