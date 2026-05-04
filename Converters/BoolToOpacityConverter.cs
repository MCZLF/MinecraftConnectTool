using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace MinecraftConnectTool.Converters;

/// <summary>
/// 布尔值到透明度转换器 - 用于根据布尔值设置控件透明度
/// </summary>
public class BoolToOpacityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            // 参数格式: "True透明度:False透明度" 或 "True透明度,False透明度"
            // 例如: "1.0:0.5" 表示 true 时透明度 1.0，false 时透明度 0.5
            double trueOpacity = 1.0;
            double falseOpacity = 0.5;
            
            if (parameter is string paramStr)
            {
                char[] separators = { ':', ',' };
                var parts = paramStr.Split(separators, 2);
                if (parts.Length >= 2)
                {
                    if (double.TryParse(parts[0], System.Globalization.NumberStyles.Float, culture, out double t))
                    {
                        trueOpacity = t;
                    }
                    if (double.TryParse(parts[1], System.Globalization.NumberStyles.Float, culture, out double f))
                    {
                        falseOpacity = f;
                    }
                }
            }
            
            return boolValue ? trueOpacity : falseOpacity;
        }
        return 1.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // 单向转换，不需要实现
        throw new NotImplementedException();
    }
}
