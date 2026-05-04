using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace MinecraftConnectTool.Converters;

/// <summary>
/// 整数到布尔值转换器 - 用于RadioButton等控件
/// </summary>
public class IntToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intValue && parameter is string paramStr)
        {
            if (int.TryParse(paramStr, out int paramValue))
            {
                return intValue == paramValue;
            }
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && boolValue && parameter is string paramStr)
        {
            if (int.TryParse(paramStr, out int paramValue))
            {
                return paramValue;
            }
        }
        return 0;
    }
}
