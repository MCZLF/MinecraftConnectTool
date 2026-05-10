using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Material.Icons;
using MinecraftConnectTool.ViewModels.Pages;

namespace MinecraftConnectTool.Converters;

/// <summary>
/// 指示灯状态转换为背景色
/// </summary>
public class BadgeStateToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // 处理 P2PPageViewModel.BadgeState
        if (value is P2PPageViewModel.BadgeState p2pState)
        {
            return p2pState switch
            {
                P2PPageViewModel.BadgeState.Success => new SolidColorBrush(Color.Parse("#4CAF50")),
                P2PPageViewModel.BadgeState.Waiting => new SolidColorBrush(Color.Parse("#dfc092")),
                P2PPageViewModel.BadgeState.Warn => new SolidColorBrush(Color.Parse("#dfc092ff")),
                P2PPageViewModel.BadgeState.Error => new SolidColorBrush(Color.Parse("#F44336")),
                P2PPageViewModel.BadgeState.Processing => new SolidColorBrush(Color.Parse("#2196F3")),
                _ => new SolidColorBrush(Color.Parse("#9E9E9E"))
            };
        }
        
        // 处理 LinkPageViewModel.BadgeState
        if (value is BadgeState linkState)
        {
            return linkState switch
            {
                BadgeState.Success => new SolidColorBrush(Color.Parse("#4CAF50")),
                BadgeState.Waiting => new SolidColorBrush(Color.Parse("#dfc092")),
                BadgeState.Warning => new SolidColorBrush(Color.Parse("#FF9800")),
                BadgeState.Error => new SolidColorBrush(Color.Parse("#F44336")),
                BadgeState.Info => new SolidColorBrush(Color.Parse("#2196F3")),
                _ => new SolidColorBrush(Color.Parse("#9E9E9E"))
            };
        }
        
        return new SolidColorBrush(Color.Parse("#9E9E9E"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 指示灯状态转换为图标
/// </summary>
public class BadgeStateToIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // 处理 P2PPageViewModel.BadgeState
        if (value is P2PPageViewModel.BadgeState p2pState)
        {
            return p2pState switch
            {
                P2PPageViewModel.BadgeState.Success => MaterialIconKind.CheckCircle,
                P2PPageViewModel.BadgeState.Waiting => MaterialIconKind.ProgressClock,
                P2PPageViewModel.BadgeState.Warn => MaterialIconKind.Alert,
                P2PPageViewModel.BadgeState.Error => MaterialIconKind.CloseCircle,
                P2PPageViewModel.BadgeState.Processing => MaterialIconKind.ProgressClock,
                _ => MaterialIconKind.InformationCircle
            };
        }
        
        // 处理 LinkPageViewModel.BadgeState
        if (value is BadgeState linkState)
        {
            return linkState switch
            {
                BadgeState.Success => MaterialIconKind.CheckCircle,
                BadgeState.Waiting => MaterialIconKind.ProgressClock,
                BadgeState.Warning => MaterialIconKind.Alert,
                BadgeState.Error => MaterialIconKind.CloseCircle,
                BadgeState.Info => MaterialIconKind.InformationCircle,
                _ => MaterialIconKind.InformationCircle
            };
        }
        
        return MaterialIconKind.InformationCircle;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
