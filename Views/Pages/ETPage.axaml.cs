using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MinecraftConnectTool.ExtensionUI;
using MinecraftConnectTool.ViewModels.Pages;

namespace MinecraftConnectTool.Views.Pages;

public partial class ETPage : UserControl
{
    private ScrollViewer? _logScrollViewer;
    private ETPageViewModel? _viewModel;

    public ETPage()
    {
        InitializeComponent();
        _viewModel = new ETPageViewModel();
        DataContext = _viewModel;

        // 订阅日志变化事件，自动滚动到底部
        _viewModel.LogTextChanged += OnLogTextChanged;
        // 订阅端口输入请求事件
        _viewModel.RequestPortInput += OnRequestPortInput;
        // 订阅打开玩家管理面板事件
        _viewModel.OpenPlayerManagerRequested += OnOpenPlayerManagerRequested;

        // 页面卸载时取消订阅
        this.Unloaded += (_, _) =>
        {
            if (_viewModel != null)
            {
                _viewModel.LogTextChanged -= OnLogTextChanged;
                _viewModel.RequestPortInput -= OnRequestPortInput;
                _viewModel.OpenPlayerManagerRequested -= OnOpenPlayerManagerRequested;
            }
        };
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _logScrollViewer = this.FindControl<ScrollViewer>("LogScrollViewer");
    }

    /// <summary>
    /// 日志文本变化时自动滚动到底部
    /// </summary>
    private void OnLogTextChanged(object? sender, EventArgs e)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (_logScrollViewer != null)
            {
                _logScrollViewer.ScrollToEnd();
            }
        });
    }

    /// <summary>
    /// 处理端口输入请求 - 与Link模式共享对话框
    /// </summary>
    private async System.Threading.Tasks.Task<string?> OnRequestPortInput(string defaultPort)
    {
        if (this.VisualRoot is Window parentWindow)
        {
            var result = await MD3InputDialog.ShowAsync(
                parentWindow,
                "请输入游戏内监听端口(例如25565)：",
                "端口输入",
                "在此输入端口号...",
                ""  // 不设置默认值
            );
            return result;
        }
        return null;
    }

    /// <summary>
    /// 打开ET房间列表面板
    /// </summary>
    private void OnOpenPlayerManagerRequested(object? sender, EventArgs e)
    {
        if (Avalonia.Application.Current?.ApplicationLifetime
            is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = desktop.MainWindow;
            if (mainWindow is MainWindow mw)
            {
                _ = mw.ShowETRoomListAsync();
            }
        }
    }
}
