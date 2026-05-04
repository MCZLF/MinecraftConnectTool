using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace MinecraftConnectTool.Converters;

/// <summary>
/// 布尔值转预设背景画刷
/// </summary>
public class BoolToPresetBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
        {
            // 选中状态使用主色调容器色
            return new SolidColorBrush(Color.Parse("#4F378B"));
        }
        // 未选中状态使用表面变体色
        return new SolidColorBrush(Color.Parse("#49454F"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 布尔值转预设边框画刷
/// </summary>
public class BoolToPresetBorderConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
        {
            // 选中状态使用主色调
            return new SolidColorBrush(Color.Parse("#D0BCFF"));
        }
        // 未选中状态使用透明
        return Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 布尔值转深色/亮色模式文本
/// </summary>
public class BoolToDarkModeTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isDarkMode)
        {
            return isDarkMode ? "深色模式" : "亮色模式";
        }
        return "深色模式";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 整数大于1转换器（用于显示Back按钮）
/// </summary>
public class IntGreaterThanOneConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intValue)
        {
            return intValue > 1;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
