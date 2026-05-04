using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace MinecraftConnectTool.Converters;

/// <summary>
/// 颜色到画刷转换器 - 将 Color 转换为 SolidColorBrush
/// </summary>
public class ColorToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Color color)
        {
            return new SolidColorBrush(color);
        }
        return new SolidColorBrush(Colors.Transparent);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SolidColorBrush brush)
        {
            return brush.Color;
        }
        return Colors.Transparent;
    }
}
