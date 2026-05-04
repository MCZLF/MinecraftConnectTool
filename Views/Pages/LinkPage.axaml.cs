using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MinecraftConnectTool.ExtensionUI;
using MinecraftConnectTool.ViewModels.Pages;
using MinecraftConnectTool.Views;

namespace MinecraftConnectTool.Views.Pages;

public partial class LinkPage : UserControl
{
    private ScrollViewer? _logScrollViewer;
    private TextBlock? _logTextBlock;
    private readonly LinkPageViewModel? _viewModel;

    public LinkPage()
    {
        InitializeComponent();
        _viewModel = new LinkPageViewModel();
        DataContext = _viewModel;

        // 订阅全局停止请求事件
        MainWindow.LinkStopRequested += OnLinkStopRequested;

        // 订阅端口输入请求事件
        _viewModel.RequestPortInput += OnRequestPortInput;

        // 订阅打开玩家管理面板事件
        _viewModel.OpenPlayerManagerRequested += OnOpenPlayerManagerRequested;

        // 控件卸载时取消订阅
        Unloaded += (s, e) =>
        {
            MainWindow.LinkStopRequested -= OnLinkStopRequested;
            if (_viewModel != null)
            {
                _viewModel.RequestPortInput -= OnRequestPortInput;
                _viewModel.OpenPlayerManagerRequested -= OnOpenPlayerManagerRequested;
            }
        };
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        // 获取日志控件引用
        _logScrollViewer = this.FindControl<ScrollViewer>("LogScrollViewer");
        _logTextBlock = this.FindControl<TextBlock>("LogTextBlock");

        // 监听日志文本变化，自动滚动到底部
        if (_logTextBlock != null)
        {
            _logTextBlock.PropertyChanged += (s, e) =>
            {
                if (e.Property.Name == "Text" && _logScrollViewer != null)
                {
                    // 使用Dispatcher确保在UI线程执行
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        _logScrollViewer.ScrollToEnd();
                    });
                }
            };
        }
        
        // 绑定复制按钮点击事件
        var copyButton = this.FindControl<Button>("CopyButton");
        if (copyButton != null && _viewModel != null)
        {
            copyButton.Click += async (s, e) => await _viewModel.CopyPromptCodeAsync();
        }
    }

    /// <summary>
    /// 处理全局停止请求
    /// </summary>
    private void OnLinkStopRequested(object? sender, EventArgs e)
    {
        _viewModel?.StopLink();
    }

    /// <summary>
    /// 处理端口输入请求
    /// </summary>
    private async Task<string?> OnRequestPortInput(string defaultPort)
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
    /// 调试选项按钮点击事件 - 显示/隐藏发送命令区域
    /// </summary>
    private void DebugButton_Click(object? sender, RoutedEventArgs e)
    {
        // 切换命令输入区域的可见性
        if (_viewModel != null)
        {
            _viewModel.IsCommandInputVisible = !_viewModel.IsCommandInputVisible;
        }
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
