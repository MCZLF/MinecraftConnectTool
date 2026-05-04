using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MinecraftConnectTool.ViewModels.Pages;
using MinecraftConnectTool.Services;

namespace MinecraftConnectTool.Views.Pages;

public partial class HelpPage : UserControl
{
    public HelpPage()
    {
        InitializeComponent();
        DataContext = new HelpPageViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnTutorialLinkClick(object? sender, PointerPressedEventArgs e)
    {
        OpenUrl("http://mcjavao.tttttttttt.top/");
    }

    private void OnVideoLinkClick(object? sender, PointerPressedEventArgs e)
    {
        OpenUrl("https://www.bilibili.com/video/BV1sBXyYgE1j/");
    }

    private void OnImageGuideClick(object? sender, RoutedEventArgs e)
    {
        // 显示一图解 - 打开图文教程页面
        OpenUrl("http://mcjavao.tttttttttt.top/");
    }

    private void OnBugReportClick(object? sender, RoutedEventArgs e)
    {
        // BUG反馈 - 打开QQ群链接
        OpenUrl("https://qm.qq.com/q/8NAoszhKqk");
    }

    private void OnCheckRenderingModeClick(object? sender, RoutedEventArgs e)
    {
        // 查询当前渲染模式
        var renderingMode = ThemeService.Instance.RenderingMode;
        var renderingModeText = this.FindControl<TextBlock>("RenderingModeText");
        var renderingModeDetail = this.FindControl<TextBlock>("RenderingModeDetail");
        var renderingModeBorder = this.FindControl<Border>("RenderingModeBorder");

        if (renderingModeText != null && renderingModeDetail != null && renderingModeBorder != null)
        {
            string modeName = renderingMode switch
            {
                RenderingMode.SystemDefault => "系统默认",
                RenderingMode.Gpu => "GPU 渲染",
                RenderingMode.Cpu => "CPU 渲染",
                _ => "未知"
            };

            string modeDescription = renderingMode switch
            {
                RenderingMode.SystemDefault => "使用系统默认的渲染方式，自动选择最佳方案",
                RenderingMode.Gpu => "使用硬件加速渲染，性能更好但内存占用较高",
                RenderingMode.Cpu => "使用软件渲染，兼容性更好且内存占用较低",
                _ => "无法获取渲染模式信息"
            };

            renderingModeText.Text = $"当前渲染模式: {modeName}";
            renderingModeDetail.Text = modeDescription;
            renderingModeBorder.IsVisible = true;
        }
    }

    private void OnPlaceholder2Click(object? sender, RoutedEventArgs e)
    {
        Debug.WriteLine("占位按钮2点击");
    }

    private void OnRestartClick(object? sender, RoutedEventArgs e)
    {
        // 重启应用程序
        var exePath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(exePath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true
            });
        }
        Environment.Exit(0);
    }

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"打开链接失败: {ex.Message}");
        }
    }
}
