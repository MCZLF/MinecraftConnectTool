using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MinecraftConnectTool.ViewModels.Pages;

namespace MinecraftConnectTool.Views.Pages;

public partial class P2PPage : UserControl
{
    private ScrollViewer? _logScrollViewer;
    private P2PPageViewModel? _viewModel;

    public P2PPage()
    {
        InitializeComponent();
        _viewModel = new P2PPageViewModel();
        DataContext = _viewModel;
        
        // 订阅日志变化事件，自动滚动到底部
        if (_viewModel != null)
        {
            _viewModel.LogTextChanged += OnLogTextChanged;
            _viewModel.OpenPlayerManagerRequested += OnOpenPlayerManagerRequested;
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _logScrollViewer = this.FindControl<ScrollViewer>("LogScrollViewer");
        
        // 绑定复制按钮点击事件
        var copyButton = this.FindControl<Button>("CopyButton");
        if (copyButton != null && _viewModel != null)
        {
            copyButton.Click += (s, e) => _viewModel.CopyInfoCommand.Execute(null);
        }
    }

    /// <summary>
    /// 日志文本变化时自动滚动到底部
    /// </summary>
    private void OnLogTextChanged(object? sender, EventArgs e)
    {
        // 使用Dispatcher确保在UI线程执行
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (_logScrollViewer != null)
            {
                _logScrollViewer.ScrollToEnd();
            }
        });
    }
    
    /// <summary>
    /// 打开玩家管理面板
    /// </summary>
    private void OnOpenPlayerManagerRequested(object? sender, EventArgs e)
    {
        // 获取主窗口并显示玩家管理面板
        if (Avalonia.Application.Current?.ApplicationLifetime 
            is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = desktop.MainWindow;
            if (mainWindow is MainWindow mw)
            {
                _ = mw.ShowPanelPlayerManagerAsync();
            }
        }
    }
}
