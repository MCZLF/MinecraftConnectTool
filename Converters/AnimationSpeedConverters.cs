using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using MinecraftConnectTool.Services;
using System;
using System.Globalization;

namespace MinecraftConnectTool.Converters;

/// <summary>
/// 动画速度到背景色转换器 - 用于显示当前选中的速度
/// 使用动态资源以支持亮暗色主题
/// </summary>
public class AnimationSpeedToBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is AnimationSpeed currentSpeed && parameter is AnimationSpeed targetSpeed)
        {
            if (currentSpeed == targetSpeed)
            {
                // 选中状态 - 使用主题主色容器色
                return new SolidColorBrush(GetThemeColor("MaterialPrimaryContainerBrush", Color.Parse("#EADDFF")));
            }
        }
        // 未选中状态 - 透明
        return new SolidColorBrush(Colors.Transparent);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static Color GetThemeColor(string resourceKey, Color defaultColor)
    {
        if (Application.Current?.Resources.TryGetValue(resourceKey, out var resource) == true)
        {
            if (resource is SolidColorBrush brush)
            {
                return brush.Color;
            }
            else if (resource is Color color)
            {
                return color;
            }
        }
        return defaultColor;
    }
}

/// <summary>
/// 动画速度到前景色转换器 - 用于显示当前选中的速度文字颜色
/// </summary>
public class AnimationSpeedToForegroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is AnimationSpeed currentSpeed && parameter is AnimationSpeed targetSpeed)
        {
            if (currentSpeed == targetSpeed)
            {
                // 选中状态 - 使用主题OnPrimaryContainer色（参考HomePage卡片设计）
                return new SolidColorBrush(GetThemeColor("MaterialOnPrimaryContainerBrush", Color.Parse("#21005D")));
            }
        }
        // 未选中状态 - 使用主题表面文字色
        return new SolidColorBrush(GetThemeColor("MaterialOnSurfaceBrush", Color.Parse("#E6E1E5")));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static Color GetThemeColor(string resourceKey, Color defaultColor)
    {
        if (Application.Current?.Resources.TryGetValue(resourceKey, out var resource) == true)
        {
            if (resource is SolidColorBrush brush)
            {
                return brush.Color;
            }
            else if (resource is Color color)
            {
                return color;
            }
        }
        return defaultColor;
    }
}

/// <summary>
/// 渲染方式到背景色转换器 - 用于显示当前选中的渲染方式
/// </summary>
public class RenderingModeToBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is RenderingMode currentMode && parameter is RenderingMode targetMode)
        {
            if (currentMode == targetMode)
            {
                // 选中状态 - 使用主题主色容器色（参考HomePage卡片设计）
                return new SolidColorBrush(GetThemeColor("MaterialPrimaryContainerBrush", Color.Parse("#EADDFF")));
            }
        }
        return new SolidColorBrush(Colors.Transparent);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static Color GetThemeColor(string resourceKey, Color defaultColor)
    {
        if (Application.Current?.Resources.TryGetValue(resourceKey, out var resource) == true)
        {
            if (resource is SolidColorBrush brush)
            {
                return brush.Color;
            }
            else if (resource is Color color)
            {
                return color;
            }
        }
        return defaultColor;
    }
}

/// <summary>
/// 渲染方式到前景色转换器 - 用于显示当前选中的渲染方式文字颜色
/// </summary>
public class RenderingModeToForegroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is RenderingMode currentMode && parameter is RenderingMode targetMode)
        {
            if (currentMode == targetMode)
            {
                // 选中状态 - 使用主题OnPrimaryContainer色（参考HomePage卡片设计）
                return new SolidColorBrush(GetThemeColor("MaterialOnPrimaryContainerBrush", Color.Parse("#21005D")));
            }
        }
        return new SolidColorBrush(GetThemeColor("MaterialOnSurfaceBrush", Color.Parse("#E6E1E5")));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static Color GetThemeColor(string resourceKey, Color defaultColor)
    {
        if (Application.Current?.Resources.TryGetValue(resourceKey, out var resource) == true)
        {
            if (resource is SolidColorBrush brush)
            {
                return brush.Color;
            }
            else if (resource is Color color)
            {
                return color;
            }
        }
        return defaultColor;
    }
}
