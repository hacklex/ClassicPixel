using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace PixelEditor;

public class EnumToIsCheckedConverter : IValueConverter
{
    public static EnumToIsCheckedConverter Instance { get; } = new();
        
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return AvaloniaProperty.UnsetValue;
        var valueType = value.GetType();
        if (!valueType.IsEnum) return AvaloniaProperty.UnsetValue;
        if (parameter?.GetType() == targetType) return Equals(value, parameter);
        if (parameter is not string enumValueString) return AvaloniaProperty.UnsetValue;
        return Enum.TryParse(valueType, enumValueString, false, out object? result) 
            ? Equals(result, value)
            : AvaloniaProperty.UnsetValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not true) return AvaloniaProperty.UnsetValue;

        if (parameter?.GetType() == targetType) return parameter;
        if (parameter is not string enumValueString) return AvaloniaProperty.UnsetValue;
        return Enum.TryParse(targetType, enumValueString, false, out object? result) 
            ? result 
            : AvaloniaProperty.UnsetValue;
    }
}