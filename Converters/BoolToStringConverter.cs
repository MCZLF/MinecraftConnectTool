using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace MinecraftConnectTool.Converters;

/// <summary>
/// 布尔值到字符串转换器 - 用于根据布尔值显示不同文本
/// </summary>
public class BoolToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string paramStr)
        {
            // 支持多种分隔符: ":" 或 ","
            char[] separators = { ':', ',' };
            var parts = paramStr.Split(separators, 2);
            if (parts.Length >= 2)
            {
                return boolValue ? parts[0] : parts[1];
            }
        }
        return "";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // 单向转换，不需要实现
        throw new NotImplementedException();
    }
}
