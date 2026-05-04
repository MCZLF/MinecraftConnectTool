using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Path = System.IO.Path;
using Material.Icons.Avalonia;
using MinecraftConnectTool.Services;
using MinecraftConnectTool.ViewModels;
using Material.Icons;

namespace MinecraftConnectTool.Views;

public partial class MainWindow : Window
{
    // 版本号
    public static readonly string version = "0.0.7Pre03(SP4)";

    // 版本代号
    public static readonly string designation = "我们终将重逢_摘自 漫画«有兽焉»_1000话";

    // 全局水印配置
    public static readonly bool EnableWatermark = true;
    public static readonly string WatermarkText = "这是一个测试版本😭";

    // 初次启动向导配置
    public static readonly bool EnableFirstLaunchWizard = true;
    
    
    // 复用的JsonSerializerOptions
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    
    private readonly PlatformService _platformService;
    private readonly P2PModeService _p2pService;
    private readonly MemoryMonitorService? _memoryMonitorService;
    private MainWindowViewModel? _viewModel;
    private bool _isPerformanceModeEnabled;

    public MainWindow()
    {
        paintLogo();
        _platformService = new PlatformService();
        _p2pService = new P2PModeService();

        // 检查性能模式是否开启
        _isPerformanceModeEnabled = ConfigService.Read<bool>("EnablePerformanceMode", false);

        // 性能模式下初始化内存监控服务
        if (_isPerformanceModeEnabled)
        {
            _memoryMonitorService = new MemoryMonitorService(TimeSpan.FromSeconds(2));
            _memoryMonitorService.MemoryInfoUpdated += OnMemoryInfoUpdated;
        }

        // 订阅P2P服务事件
        _p2pService.CoreStopped += OnP2PCoreStopped;

        // 订阅照片背景变更事件
        ThemeService.Instance.PhotoBackgroundChanged += OnPhotoBackgroundChanged;

        // 订阅动画速度变更事件
        ThemeService.Instance.AnimationSpeedChanged += OnAnimationSpeedChanged;

        InitializeComponent();
        
        // 根据平台设置窗口属性
        SetupWindowForPlatform();
        
        DataContextChanged += OnDataContextChanged;
        
        // 窗口加载完成后更新导航状态
        Loaded += OnWindowLoaded;
        
        // 监听窗口状态变化，更新最大化按钮图标
        PropertyChanged += OnWindowPropertyChanged;
    }
    
    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == WindowStateProperty)
        {
            UpdateMaximizeIcon();
        }
    }
    
    private void UpdateMaximizeIcon()
    {
        var maximizeIcon = this.FindControl<MaterialIcon>("MaximizeIcon");
        if (maximizeIcon != null)
        {
            maximizeIcon.Kind = WindowState == WindowState.Maximized 
                ? Material.Icons.MaterialIconKind.WindowRestore 
                : Material.Icons.MaterialIconKind.WindowMaximize;
        }
    }

    private void OnWindowLoaded(object? sender, System.EventArgs e)
    {
        // 激活窗口并置于前台
        Activate();
        Topmost = true;
        Topmost = false;
        
        // 初始化剪贴板帮助类
        ClipboardHelper.Initialize(this);
        
        if (_viewModel != null)
        {
            UpdateNavigationButtonState(_viewModel);
        }
        UpdateMaximizeIcon();
        
        // 设置标题栏版本号
        var versionTextBlock = this.FindControl<TextBlock>("VersionTextBlock");
        if (versionTextBlock != null)
        {
            versionTextBlock.Text = version;
        }
        
        // 设置水印显示
        SetupWatermark();
        
        // 设置悬浮停止按钮事件
        SetupFloatingStopButton();
        
        // 显示问候语（如果未启用静默启动）
        bool nonotifywhenstart = ConfigService.Read<bool>("nonotifywhenstart", false);
        if (!nonotifywhenstart)
        {
            ShowGreeting();
        }
        
        // 检查公告
        _ = CheckAnnouncementAsync();
        
        // 检查版本更新
        _ = CheckVersionAsync();
        
        // 检查核心是否已在运行 - 参考P2P页面逻辑
        CheckCoreRunningStatus();

        // 初始化内存监控UI（仅在性能模式开启时）
        InitializeMemoryMonitorUI();

        // 初始化照片背景
        InitializePhotoBackground();

        // 初始化页面切换动画时长
        UpdatePageTransitionDuration();
    }

    /// <summary>
    /// 初始化照片背景
    /// </summary>
    private void InitializePhotoBackground()
    {
        var backgroundImage = this.FindControl<Image>("BackgroundImage");
        var backgroundOverlay = this.FindControl<Avalonia.Controls.Shapes.Rectangle>("BackgroundOverlay");
        var leftNavBackground = this.FindControl<Border>("LeftNavBackground");
        var leftNavBorder = this.FindControl<Border>("LeftNavBorder");
        var rightContentBackground = this.FindControl<Border>("RightContentBackground");
        var rightContentBorder = this.FindControl<Border>("RightContentBorder");

        if (backgroundImage == null || backgroundOverlay == null) return;

        // 检查是否启用了照片背景
        bool enablePhotoBackground = ThemeService.Instance.EnablePhotoBackground;
        string? photoPath = ThemeService.Instance.PhotoBackgroundPath;
        double opacity = ThemeService.Instance.BackgroundOpacity;
        double controlOpacity = ThemeService.Instance.ControlOpacity;

        // 根据当前主题调整透明度（亮色模式需要更不透明以保证可读性）
        bool isDarkMode = ThemeService.Instance.IsDarkMode;
        
        // 文字额外固定+10%不透明度
        double textExtraOpacity = 0.10;
        
        // 左侧导航背景透明度（让照片显示出来）
        double navBgOpacity = isDarkMode ? opacity : Math.Min(opacity + 0.15, 0.95);
        // 左侧导航控件层不透明度（让按钮等控件更清晰）
        double navControlOpacity = isDarkMode 
            ? Math.Min(opacity + controlOpacity, 1.0) 
            : Math.Min(opacity + controlOpacity + 0.15, 1.0);
        // 左侧导航文字额外不透明度
        double navTextOpacity = Math.Min(navControlOpacity + textExtraOpacity, 1.0);
        
        // 右侧内容区域背景透明度（让照片显示出来）
        double contentBgOpacity = isDarkMode ? opacity : Math.Min(opacity + 0.10, 0.95);
        // 右侧内容区域控件层不透明度（让控件更清晰）
        double contentControlOpacity = isDarkMode 
            ? Math.Min(opacity + controlOpacity, 1.0) 
            : Math.Min(opacity + controlOpacity + 0.15, 1.0);
        // 右侧内容区域文字额外不透明度
        double contentTextOpacity = Math.Min(contentControlOpacity + textExtraOpacity, 1.0);

        if (enablePhotoBackground && !string.IsNullOrEmpty(photoPath) && File.Exists(photoPath))
        {
            try
            {
                // 加载图片
                var bitmap = new Bitmap(photoPath);
                backgroundImage.Source = bitmap;
                backgroundImage.IsVisible = true;
                backgroundImage.Opacity = 1;
                
                // 禁用图片的高质量渲染选项，使用低质量快速渲染
                // 这样图片在动画期间不会触发昂贵的重采样
                RenderOptions.SetBitmapInterpolationMode(backgroundImage, BitmapInterpolationMode.LowQuality);
                RenderOptions.SetBitmapBlendingMode(backgroundImage, BitmapBlendingMode.Unspecified);

                // 应用透明度设置
                backgroundOverlay.Opacity = opacity;
                
                // 左侧导航栏：背景层显示照片，控件层让按钮更清晰，文字额外+10%
                if (leftNavBackground != null) leftNavBackground.Opacity = navBgOpacity;
                if (leftNavBorder != null) leftNavBorder.Opacity = navControlOpacity;
                
                // 右侧内容区域：背景层显示照片，内容层让控件更清晰，文字额外+10%
                if (rightContentBackground != null) rightContentBackground.Opacity = contentBgOpacity;
                if (rightContentBorder != null) rightContentBorder.Opacity = contentControlOpacity;
                
                // 应用文字额外不透明度
                ApplyTextOpacity(navTextOpacity, contentTextOpacity);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载背景图片失败: {ex.Message}");
                ResetToOpaqueBackground(backgroundImage, backgroundOverlay, leftNavBackground, leftNavBorder, rightContentBackground, rightContentBorder);
            }
        }
        else
        {
            // 未启用照片背景，保持不透明
            ResetToOpaqueBackground(backgroundImage, backgroundOverlay, leftNavBackground, leftNavBorder, rightContentBackground, rightContentBorder);
        }
    }

    /// <summary>
    /// 应用文字额外不透明度
    /// </summary>
    private void ApplyTextOpacity(double navTextOpacity, double contentTextOpacity)
    {
        // 左侧导航文字
        var homeText = this.FindControl<TextBlock>("HomeText");
        var p2pText = this.FindControl<TextBlock>("P2PText");
        var linkText = this.FindControl<TextBlock>("LinkText");
        var playText = this.FindControl<TextBlock>("PlayText");
        var updateText = this.FindControl<TextBlock>("UpdateText");
        var settingsText = this.FindControl<TextBlock>("SettingsText");
        
        if (homeText != null) homeText.Opacity = navTextOpacity;
        if (p2pText != null) p2pText.Opacity = navTextOpacity;
        if (linkText != null) linkText.Opacity = navTextOpacity;
        if (playText != null) playText.Opacity = navTextOpacity;
        if (updateText != null) updateText.Opacity = navTextOpacity;
        if (settingsText != null) settingsText.Opacity = navTextOpacity;
    }

    /// <summary>
    /// 重置为不透明背景
    /// </summary>
    private void ResetToOpaqueBackground(Image backgroundImage, Avalonia.Controls.Shapes.Rectangle backgroundOverlay, Border? leftNavBackground, Border? leftNavBorder, Border? rightContentBackground, Border? rightContentBorder)
    {
        backgroundImage.IsVisible = false;
        backgroundOverlay.Opacity = 1;
        if (leftNavBackground != null) leftNavBackground.Opacity = 1;
        if (leftNavBorder != null) leftNavBorder.Opacity = 1;
        if (rightContentBackground != null) rightContentBackground.Opacity = 1;
        if (rightContentBorder != null) rightContentBorder.Opacity = 1;
        
        // 重置文字不透明度为默认值
        ApplyTextOpacity(1.0, 1.0);
    }

    /// <summary>
    /// 照片背景设置变更事件处理
    /// </summary>
    private void OnPhotoBackgroundChanged(object? sender, EventArgs e)
    {
        // 在UI线程上更新背景
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            InitializePhotoBackground();
        });
    }

    /// <summary>
    /// 动画速度变更处理 - 更新页面切换动画时长
    /// </summary>
    private void OnAnimationSpeedChanged(object? sender, EventArgs e)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            UpdatePageTransitionDuration();
        });
    }

    /// <summary>
    /// 更新页面切换动画时长
    /// </summary>
    private void UpdatePageTransitionDuration()
    {
        var durationMs = ThemeService.Instance.AnimationDurationMs;
        
        // 查找 TransitioningContentControl 并更新页面切换动画
        var transitioningControl = this.FindControl<TransitioningContentControl>("ContentControl");
        if (transitioningControl != null)
        {
            // 如果时长为0，禁用页面切换动画
            if (durationMs <= 0)
            {
                transitioningControl.PageTransition = null;
            }
            else
            {
                // 更新 PageTransition 动画时长
                transitioningControl.PageTransition = new CrossFade(TimeSpan.FromMilliseconds(durationMs));
            }
        }
    }

    /// <summary>
    /// 初始化内存监控UI - 仅在性能模式开启时显示
    /// </summary>
    private void InitializeMemoryMonitorUI()
    {
        if (!_isPerformanceModeEnabled)
            return;

        // 显示内存监控面板
        var memoryMonitorCanvas = this.FindControl<Canvas>("MemoryMonitorCanvas");
        if (memoryMonitorCanvas != null)
        {
            memoryMonitorCanvas.IsVisible = true;
        }

        // 设置拖动功能
        SetupMemoryMonitorDrag();
    }

    private bool _isDraggingMemoryMonitor;
    private Point _dragStartPoint;
    private double _memoryMonitorLeft = 24;
    private double _memoryMonitorBottom = 24;

    /// <summary>
    /// 设置内存监控器拖动功能
    /// </summary>
    private void SetupMemoryMonitorDrag()
    {
        var memoryBorder = this.FindControl<Border>("MemoryMonitorBorder");
        if (memoryBorder == null) return;

        memoryBorder.PointerPressed += (s, e) =>
        {
            _isDraggingMemoryMonitor = true;
            _dragStartPoint = e.GetPosition(this);
            memoryBorder.Cursor = new Cursor(StandardCursorType.DragMove);
        };

        memoryBorder.PointerMoved += (s, e) =>
        {
            if (!_isDraggingMemoryMonitor) return;

            var currentPoint = e.GetPosition(this);
            var deltaX = currentPoint.X - _dragStartPoint.X;
            var deltaY = currentPoint.Y - _dragStartPoint.Y;

            _memoryMonitorLeft += deltaX;
            _memoryMonitorBottom -= deltaY;

            // 边界限制
            var canvas = this.FindControl<Canvas>("MemoryMonitorCanvas");
            if (canvas != null)
            {
                var maxLeft = canvas.Bounds.Width - memoryBorder.Bounds.Width - 10;
                var maxBottom = canvas.Bounds.Height - memoryBorder.Bounds.Height - 10;

                _memoryMonitorLeft = Math.Clamp(_memoryMonitorLeft, 10, maxLeft);
                _memoryMonitorBottom = Math.Clamp(_memoryMonitorBottom, 10, maxBottom);
            }

            Canvas.SetLeft(memoryBorder, _memoryMonitorLeft);
            Canvas.SetBottom(memoryBorder, _memoryMonitorBottom);

            _dragStartPoint = currentPoint;
        };

        memoryBorder.PointerReleased += (s, e) =>
        {
            _isDraggingMemoryMonitor = false;
            memoryBorder.Cursor = new Cursor(StandardCursorType.Hand);
        };
    }

    /// <summary>
    /// 内存信息更新回调
    /// </summary>
    private void OnMemoryInfoUpdated(MemoryInfo info)
    {
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            UpdateMemoryDisplay(info);
        });
    }

    /// <summary>
    /// 更新内存显示（使用跨平台内存监控服务的数据）
    /// </summary>
    private void UpdateMemoryDisplay(MemoryInfo info)
    {
        try
        {
            var memoryText = this.FindControl<TextBlock>("MemoryText");
            var memoryIcon = this.FindControl<MaterialIcon>("MemoryIcon");
            var memoryBorder = this.FindControl<Border>("MemoryMonitorBorder");

            if (memoryText != null)
            {
                // 显示专用工作集内存（与任务管理器默认显示一致）
                memoryText.Text = $"{info.PrivateWorkingSetMB} MB";
            }

            // 超过100MB显示红色警告
            if (memoryIcon != null && memoryText != null)
            {
                if (info.PrivateWorkingSetMB > 100)
                {
                    memoryIcon.Foreground = new SolidColorBrush(Colors.Red);
                    memoryText.Foreground = new SolidColorBrush(Colors.Red);
                }
                else if (info.PrivateWorkingSetMB > 70)
                {
                    memoryIcon.Foreground = new SolidColorBrush(Colors.Orange);
                    memoryText.Foreground = new SolidColorBrush(Colors.Orange);
                }
                else
                {
                    memoryIcon.Foreground = this.FindResource("MaterialOnSurfaceBrush") as IBrush
                        ?? new SolidColorBrush(Colors.Gray);
                    memoryText.Foreground = this.FindResource("MaterialOnSurfaceBrush") as IBrush
                        ?? new SolidColorBrush(Colors.Gray);
                }
            }
        }
        catch { }
    }
    
    /// <summary>
    /// 检查核心运行状态 - 同时检查P2P(main)和Link核心
    /// </summary>
    private void CheckCoreRunningStatus()
    {
        bool isMainRunning = System.Diagnostics.Process.GetProcessesByName("main").Length > 0;
        bool isLinkRunning = System.Diagnostics.Process.GetProcessesByName("link").Length > 0;
        
        if (_viewModel != null)
        {
            if (isMainRunning || isLinkRunning)
            {
                // 有核心正在运行，设置状态以显示FloatButton
                _viewModel.IsP2PRunning = true;
                
                // 根据运行的核心类型设置模式
                if (isLinkRunning)
                {
                    P2PStateService.SetRunning(true, CoreMode.Link);
                }
                else
                {
                    P2PStateService.SetRunning(true, CoreMode.P2P);
                }
            }
        }
    }
    
    /// <summary>
    /// 显示问候语
    /// </summary>
    private async void ShowGreeting()
    {
        var greeting = GetGreetingMessage();
        
        var greetingBorder = this.FindControl<Border>("GreetingBorder");
        var greetingText = this.FindControl<TextBlock>("GreetingText");
        
        if (greetingBorder == null || greetingText == null) return;
        
        // 设置问候语文本
        greetingText.Text = greeting;
        
        // 显示问候语（带动画）
        greetingBorder.IsVisible = true;
        
        // 淡入动画
        for (double i = 0; i <= 1; i += 0.1)
        {
            greetingBorder.Opacity = i;
            await Task.Delay(30);
        }
        
        // 显示3秒
        await Task.Delay(3000);
        
        // 淡出动画
        for (double i = 1; i >= 0; i -= 0.1)
        {
            greetingBorder.Opacity = i;
            await Task.Delay(30);
        }
        
        greetingBorder.IsVisible = false;
    }
    
    /// <summary>
    /// 根据时间获取问候语
    /// </summary>
    private static string GetGreetingMessage()
    {
        var hour = DateTime.Now.Hour;
        var userName = Environment.UserName;
        
        return hour switch
        {
            >= 6 and < 12 => $"上午好, {userName}",
            >= 12 and < 14 => $"中午好, {userName}",
            >= 14 and < 18 => $"下午好, {userName}",
            _ => $"晚上好, {userName}"
        };
    }
    
    /// <summary>
    /// 检查公告 - 严格按照Form1实现
    /// </summary>
    private async Task CheckAnnouncementAsync()
    {
        const string CloudConfigUrl = "https://api.mct.mczlf.loft.games/PanelAlert";
        try
        {
            using var httpClient = new HttpClient();
            string jsonContent = await httpClient.GetStringAsync(CloudConfigUrl);
            var cloudConfig = JsonNode.Parse(jsonContent);
            bool show = cloudConfig?["Show"]?.GetValue<bool>() ?? false;
            string tagId = cloudConfig?["TagID"]?.ToString() ?? "";
            string text = cloudConfig?["Text"]?.ToString() ?? "";

            // 如果Show为false，直接返回（不显示任何内容）
            if (!show)
            {
                return;
            }

            // 验证TagID格式
            if (string.IsNullOrEmpty(tagId) || tagId.Length != 6)
            {
                Console.WriteLine("云端TagID格式错误");
                return;
            }

            // 读取本地存储的历史TagID
            string localTagId = ConfigService.Read("PanelAlertID", "");

            // 获取button2控件
            var button2 = this.FindControl<Button>("button2");
            if (button2 == null) return;

            // 二. 判断是否是首次查看
            if (localTagId == tagId)
            {
                // 已查看过，只显示按钮
                button2.IsVisible = true;
            }
            else
            {
                // 首次查看，显示PanelAlert并显示按钮
                ShowPanelAlert();
                button2.IsVisible = true;
                ConfigService.Write("PanelAlertID", tagId);
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"获取云端配置失败: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"公告检查异常: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 检查版本更新和支持状态
    /// </summary>
    private async Task CheckVersionAsync()
    {
        bool enableCheck = ConfigService.Read("EnableVersionCheck", true);
        if (!enableCheck) return;
        
        try
        {
            // 检查版本支持状态
            await CheckVersionSupportAsync();
            Debug.WriteLine("版本检查完成");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"版本检查失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 检查版本是否在支持列表中
    /// </summary>
    private async Task CheckVersionSupportAsync()
    {
        try
        {
            using var client = new HttpClient();
            var supportList = await client.GetStringAsync("https://api.mct.mczlf.loft.games/007/SupportVer");
            var versions = supportList.Split('\n');
            
            foreach (var v in versions)
            {
                if (v.Trim() == version)
                {
                    // 版本在支持列表中
                    LogSupportStatus("SupportVersion");
                    return;
                }
            }
            
            // 版本不在支持列表中
            await ShowNotSupportWarningAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"检查版本支持状态失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 显示版本不支持警告
    /// </summary>
    private async Task ShowNotSupportWarningAsync()
    {
        try
        {
            using var client = new HttpClient();
            string versionUrl = $"https://api.mct.mczlf.loft.games/007/{GetPlatformName()}_{GetArchitectureName()}/version";
            var cloudVersion = (await client.GetStringAsync(versionUrl)).Trim();

            LogSupportStatus("NotSupportNow");

            // 显示警告对话框
            string message = $"当前版本不在支持列表内，请检查更新是否可用\n若发现有Bug，请勿反馈\n\n当前版本: {version}\n云版本: {cloudVersion}";
            await ExtensionUI.MD3MessageDialog.ShowAsync(this, message, "版本不支持", Material.Icons.MaterialIconKind.AlertCircle);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"显示不支持警告失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取平台名称
    /// </summary>
    private static string GetPlatformName()
    {
        if (OperatingSystem.IsWindows()) return "Win";
        if (OperatingSystem.IsLinux()) return "Linux";
        if (OperatingSystem.IsMacOS()) return "MacOS";
        return "Win";
    }

    /// <summary>
    /// 获取架构名称
    /// </summary>
    private static string GetArchitectureName()
    {
        return RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X86 => "X86",
            Architecture.X64 => "X64",
            Architecture.Arm => "Arm",
            Architecture.Arm64 => "Arm64",
            _ => "X64"
        };
    }
    
    /// <summary>
    /// 记录支持状态到日志
    /// </summary>
    private void LogSupportStatus(string status)
    {
        try
        {
            string logPath = Path.Combine(Path.GetTempPath(), "MCZLFAPP", "Temp", "APPLog.ini");
            File.AppendAllText(logPath, status + Environment.NewLine);
        }
        catch { }
    }
    
    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        // 清理配置
        ConfigService.Delete("Server");
        ConfigService.Write("EnableRelay", false);

        // 停止多播服务
        try
        {
            Server_Post.Stop_Post();
            Debug.WriteLine("多播服务已停止");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"停止多播服务异常: {ex.Message}");
        }

        // 释放内存监控服务
        _memoryMonitorService?.Dispose();
    }
    
    /// <summary>
    /// 设置悬浮停止按钮
    /// </summary>
    private void SetupFloatingStopButton()
    {
        var floatingBtn = this.FindControl<Controls.FloatingStopButton>("FloatingStopBtn");
        if (floatingBtn == null) return;
        
        // 监听停止请求事件
        floatingBtn.StopRequested += OnFloatingStopRequested;
        
        // 窗口关闭时停止监测
        Closing += OnWindowClosing;
    }
    
    /// <summary>
    /// 处理悬浮按钮停止请求 - 关闭P2P或Link核心
    /// </summary>
    private void OnFloatingStopRequested(object? sender, EventArgs e)
    {
        // 根据当前模式停止对应的核心
        if (P2PStateService.CurrentMode == CoreMode.Link)
        {
            // Link模式 - 通过事件通知Link页面停止
            LinkStopRequested?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            // P2P模式
            _ = _p2pService.stopp2p();
        }
    }

    /// <summary>
    /// Link模式停止请求事件
    /// </summary>
    public static event EventHandler? LinkStopRequested;
    
    /// <summary>
    /// P2P核心停止事件处理 - 更新状态隐藏FloatButton
    /// </summary>
    private void OnP2PCoreStopped(object? sender, EventArgs e)
    {
        // 核心已停止，更新状态以隐藏FloatButton
        if (_viewModel != null)
        {
            _viewModel.IsP2PRunning = false;
        }
        // 通知全局状态服务，让P2P页面也能更新状态
        P2PStateService.SetRunning(false);
    }
    
    private void SetupWatermark()
    {
        var watermarkBorder = this.FindControl<Border>("WatermarkBorder");
        if (watermarkBorder == null) return;
        
        // 根据配置显示/隐藏水印
        watermarkBorder.IsVisible = EnableWatermark;
        
        if (!EnableWatermark) return;
        
        // 更新水印文本
        var line1 = this.FindControl<TextBlock>("WatermarkLine1");
        var line2 = this.FindControl<TextBlock>("WatermarkLine2");
        
        if (line1 != null) line1.Text = WatermarkText;
        if (line2 != null) line2.Text = "若遇到问题请及时反馈ISSUE或Q群内";
    }

    private void SetupWindowForPlatform()
    {
        // macOS需要特殊处理：禁用扩展客户端区域以显示原生标题栏按钮
        if (_platformService.IsMacOS)
        {
            // macOS：禁用扩展客户端区域，使用系统原生标题栏（包含红黄绿按钮）
            ExtendClientAreaToDecorationsHint = false;

            // 隐藏自定义标题栏
            var titleBar = this.FindControl<Grid>("TitleBar");
            if (titleBar != null)
            {
                titleBar.IsVisible = false;
            }

            // 调整主内容区域边距
            var mainContentGrid = this.FindControl<Grid>("MainContentGrid");
            if (mainContentGrid != null)
            {
                mainContentGrid.Margin = new Thickness(0);
            }
        }
        else if (_platformService.IsLinux)
        {
            // Linux：禁用系统标题栏，只使用自定义标题栏
            SystemDecorations = SystemDecorations.None;
        }
        // Windows平台保持默认设置（自定义标题栏+Mica效果）
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            _viewModel = vm;
            vm.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(MainWindowViewModel.CurrentPageKey))
                {
                    UpdateNavigationButtonState(vm);
                }
            };
            UpdateNavigationButtonState(vm);
        }
    }

    private void OnNavigationButtonClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string pageKey)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.NavigateToCommand.Execute(pageKey);
            }
        }
    }

    private void UpdateNavigationButtonState(MainWindowViewModel vm)
    {
        var pageKey = vm.CurrentPageKey;

        // 获取所有导航控件
        var homeIndicator = this.FindControl<Border>("HomeIndicator");
        var homeIcon = this.FindControl<MaterialIcon>("HomeIcon");
        var homeText = this.FindControl<TextBlock>("HomeText");
        var p2pIndicator = this.FindControl<Border>("P2PIndicator");
        var p2pIcon = this.FindControl<MaterialIcon>("P2PIcon");
        var p2pText = this.FindControl<TextBlock>("P2PText");
        var linkIndicator = this.FindControl<Border>("LinkIndicator");
        var linkIcon = this.FindControl<MaterialIcon>("LinkIcon");
        var linkText = this.FindControl<TextBlock>("LinkText");
        var etIndicator = this.FindControl<Border>("ETIndicator");
        var etIcon = this.FindControl<MaterialIcon>("ETIcon");
        var etText = this.FindControl<TextBlock>("ETText");
        var optimizeIndicator = this.FindControl<Border>("OptimizeIndicator");
        var optimizeIcon = this.FindControl<MaterialIcon>("OptimizeIcon");
        var optimizeText = this.FindControl<TextBlock>("OptimizeText");
        var updateIndicator = this.FindControl<Border>("UpdateIndicator");
        var updateIcon = this.FindControl<MaterialIcon>("UpdateIcon");
        var updateText = this.FindControl<TextBlock>("UpdateText");
        var settingsIndicator = this.FindControl<Border>("SettingsIndicator");
        var settingsIcon = this.FindControl<MaterialIcon>("SettingsIcon");
        var settingsText = this.FindControl<TextBlock>("SettingsText");

        // Reset all buttons
        ResetButtonStyle(homeIndicator, homeIcon, homeText);
        ResetButtonStyle(p2pIndicator, p2pIcon, p2pText);
        ResetButtonStyle(linkIndicator, linkIcon, linkText);
        ResetButtonStyle(etIndicator, etIcon, etText);
        ResetButtonStyle(optimizeIndicator, optimizeIcon, optimizeText);
        ResetButtonStyle(updateIndicator, updateIcon, updateText);
        ResetButtonStyle(settingsIndicator, settingsIcon, settingsText);

        // Highlight selected button
        (Border? indicator, MaterialIcon? icon, TextBlock? text) = pageKey switch
        {
            "Home" => (homeIndicator, homeIcon, homeText),
            "P2P" => (p2pIndicator, p2pIcon, p2pText),
            "Link" => (linkIndicator, linkIcon, linkText),
            "ET" => (etIndicator, etIcon, etText),
            "Optimize" => (optimizeIndicator, optimizeIcon, optimizeText),
            "Update" => (updateIndicator, updateIcon, updateText),
            "Settings" => (settingsIndicator, settingsIcon, settingsText),
            _ => (null, null, null)
        };

        if (indicator != null && icon != null && text != null)
        {
            SetButtonSelectedStyle(indicator, icon, text);
        }
    }

    private void ResetButtonStyle(Border? indicator, MaterialIcon? icon, TextBlock? text)
    {
        if (indicator != null)
            indicator.Background = new SolidColorBrush(Colors.Transparent);
        if (icon != null)
            icon.Foreground = this.FindResource("MaterialOnSurfaceBrush") as IBrush;
        if (text != null)
        {
            text.Foreground = this.FindResource("MaterialOnSurfaceBrush") as IBrush;
            text.Opacity = 0.7;
            text.FontWeight = FontWeight.Normal;
        }
    }

    private void SetButtonSelectedStyle(Border indicator, MaterialIcon icon, TextBlock text)
    {
        indicator.Background = this.FindResource("MaterialSecondaryContainerBrush") as IBrush;
        icon.Foreground = this.FindResource("MaterialOnSecondaryContainerBrush") as IBrush;
        text.Foreground = this.FindResource("MaterialOnSurfaceBrush") as IBrush;
        text.Opacity = 1.0;
        text.FontWeight = FontWeight.Medium;
    }

    // 窗口拖动功能
    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    // 窗口控制按钮事件处理
    private void OnMinimizeButtonClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnMaximizeButtonClick(object? sender, RoutedEventArgs e)
    {
        // 获取最大化图标控件
        var maximizeIcon = this.FindControl<MaterialIcon>("MaximizeIcon");
        
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            if (maximizeIcon != null)
                maximizeIcon.Kind = Material.Icons.MaterialIconKind.WindowMaximize;
        }
        else
        {
            WindowState = WindowState.Maximized;
            if (maximizeIcon != null)
                maximizeIcon.Kind = Material.Icons.MaterialIconKind.WindowRestore;
        }
    }

    private void OnCloseButtonClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    // 标题栏功能按钮事件处理 - 保持与Form1相同的变量名
    private void buttonSZ_Click(object? sender, RoutedEventArgs e)
    {
        // 导航到设置页面
        if (DataContext is MainWindowViewModel vm)
        {
            vm.NavigateToCommand.Execute("Settings");
        }
    }

    private void button_color_Click(object? sender, RoutedEventArgs e)
    {
        // 导航到帮助页面
        if (DataContext is MainWindowViewModel vm)
        {
            vm.NavigateToCommand.Execute("Help");
        }
    }

    private async void button1_Click(object? sender, RoutedEventArgs e)
    {
        // 刷新当前页面 - 严格按照Form1实现
        try
        {
            if (DataContext is MainWindowViewModel vm)
            {
                // 使用 NavigationService 刷新当前页面
                vm.RefreshCurrentPage();
            }
        }
        catch (Exception ex)
        {
            await ShowMessageBoxAsync($"重载失败：{ex.Message}", "确定", "错误");
        }
    }

    private async void button_mclogs_Click(object? sender, RoutedEventArgs e)
    {
        // 严格按照Form1实现云日志上传
        // button_mclogs.Loading = true; // Avalonia没有内置Loading属性
        const string api = "https://api.mclo.gs/1/log";
        try
        {
            string logPath = Path.Combine(
                Environment.GetEnvironmentVariable("TEMP") ?? Path.GetTempPath(),
                "MCZLFAPP", "Temp", "APPLog.ini");
            string configPath = Path.Combine(
                Environment.GetEnvironmentVariable("TEMP") ?? Path.GetTempPath(),
                "MCZLFAPP", "Temp", "config.json");
            string AppconfigPath = Path.Combine(
                Environment.GetEnvironmentVariable("TEMP") ?? Path.GetTempPath(),
                "MCZLFAPP", "Temp", "APPconfig.json");

            // GetNetVersion
            string Netversion = Environment.Version.ToString();
            // GetSysVersion
            string SystemEvVersion = Environment.OSVersion.VersionString + " " + (Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit");

            if (!File.Exists(logPath) || !File.Exists(configPath) || !File.Exists(AppconfigPath))
            {
                await ShowMessageBoxAsync("日志文件不存在！", "确定", "云日志上传");
                return;
            }

            string logContent = File.ReadAllText(logPath, Encoding.UTF8);
            string logAppConfig = File.ReadAllText(AppconfigPath, Encoding.UTF8);
            string logConfig = File.ReadAllText(configPath, Encoding.UTF8);
            var j = JsonNode.Parse(logConfig);
            var apps = j?["apps"]?.AsArray();
            var app = apps?.Count > 0 ? apps[0] : null;
            
            var configObj = new
            {
                Node = j?["network"]?["Node"]?.ToString(),
                SrcPort = app?["SrcPort"]?.ToString(),
                PeerNode = app?["PeerNode"]?.ToString(),
                DstPort = app?["DstPort"]?.ToString(),
                RelayNode = app?["RelayNode"]?.ToString(),
                Enabled = app?["Enabled"]?.ToString(),
                App = app
            };
            string configJson = JsonSerializer.Serialize(configObj, JsonOptions);
            
            string body = $"UploadTime: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                $"MachineName: {Environment.MachineName}\n" +
                $"APPVersion: {version}\n" +
                $"NetVersion:{Netversion}\n" +
                $"SysVersion:{SystemEvVersion}\n\n" +
                $"=========P2PConfig==============\n" +
                $"{configJson}\n\n" +
                $"========APPConfig.ini===========\n" +
                $"{logAppConfig}\n\n" +
                $"===Full_APPLog.ini===\n" +
                $"{logContent}";
            
            body = body
                   .Replace("tokenNormal", "MinecraftConnectTool")
                   .Replace("16947733", "690625244")
                   .Replace("openp2p.cn@gmail.com", "admin@mczlf.xyz")
                   .Replace("openp2p start", "Powered by OpenP2P");
            
            using var hc = new HttpClient();
            var content = new StringContent("content=" + Uri.EscapeDataString(body),
                                            Encoding.UTF8,
                                            "application/x-www-form-urlencoded");

            string resp = await hc.PostAsync(api, content)
                                  .ContinueWith(t => t.Result.Content.ReadAsStringAsync().Result);

            var json = JsonNode.Parse(resp);
            bool success = json?["success"]?.GetValue<bool>() ?? false;
            
            if (!success)
            {
                string error = json?["error"]?.ToString() ?? "未知错误";
                await ShowMessageBoxAsync($"上传失败：{error}", "确定", "云日志上传");
                return;
            }

            string url = json?["url"]?.ToString() ?? "";
            // 复制到剪贴板
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.Clipboard != null)
            {
                await topLevel.Clipboard.SetTextAsync(url);
            }
            await ShowMessageBoxAsync($"上传成功！\n日志地址：{url}\n\n已复制入剪切板中，可直接粘贴哦", "确定", "云日志上传");
        }
        catch (Exception ex)
        {
            await ShowMessageBoxAsync($"上传失败：{ex.Message}", "确定", "云日志上传");
        }
    }

    /// <summary>
    /// 显示简单消息框 - 沉浸式标题栏风格，支持亮色/暗色主题
    /// </summary>
    /// <param name="message">消息内容</param>
    /// <param name="buttonText">按钮文本</param>
    /// <param name="title">标题，默认为"增强提醒"</param>
    private async Task ShowMessageBoxAsync(string message, string buttonText, string title = "增强提醒")
    {
        // 先创建窗口引用（用于关闭按钮）
        Window? dialogWindow = null;

        // 获取当前主题是否为暗色
        bool isDarkTheme = ActualThemeVariant == ThemeVariant.Dark;

        // 创建主内容区域 - 使用动态资源适配主题
        var mainContent = new Grid
        {
            RowDefinitions = new RowDefinitions
            {
                new RowDefinition(GridLength.Auto),    // 标题栏
                new RowDefinition(GridLength.Star),    // 内容
                new RowDefinition(GridLength.Auto)     // 按钮
            },
            Margin = new Thickness(0),
            Background = new SolidColorBrush(isDarkTheme ? new Color(255, 45, 45, 45) : new Color(255, 255, 255, 255))
        };

        // 创建关闭按钮
        var closeButton = new Button
        {
            Width = 32,
            Height = 32,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Content = new MaterialIcon
            {
                Kind = MaterialIconKind.Close,
                Width = 16,
                Height = 16,
                Foreground = new SolidColorBrush(isDarkTheme ? Colors.White : Colors.Black)
            }
        };

        // 沉浸式标题栏（5%透明度）- 根据主题调整
        var titleBar = new Border
        {
            Background = new SolidColorBrush(isDarkTheme 
                ? new Color(13, 255, 255, 255)  // 暗色：5%透明白色
                : new Color(13, 0, 0, 0)),      // 亮色：5%透明黑色
            Height = 40,
            Padding = new Thickness(16, 0, 8, 0),
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                },
                Children =
                {
                    new TextBlock
                    {
                        Text = title,
                        FontSize = 14,
                        FontWeight = FontWeight.Medium,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                        Foreground = new SolidColorBrush(isDarkTheme ? Colors.White : Colors.Black)
                    },
                    closeButton
                }
            }
        };
        Grid.SetRow(titleBar, 0);
        mainContent.Children.Add(titleBar);

        // 消息内容
        var messageBlock = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14,
            LineHeight = 22,
            Margin = new Thickness(24, 20, 24, 20),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Foreground = new SolidColorBrush(isDarkTheme ? Colors.White : Colors.Black)
        };
        Grid.SetRow(messageBlock, 1);
        mainContent.Children.Add(messageBlock);

        // 确定按钮
        var okButton = new Button
        {
            Content = buttonText,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Width = 80,
            Height = 32,
            Margin = new Thickness(0, 0, 24, 20)
        };
        Grid.SetRow(okButton, 2);
        mainContent.Children.Add(okButton);

        // 创建窗口（沉浸式，无原生标题栏）
        dialogWindow = new Window
        {
            Width = 420,
            Height = 220,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false,
            SystemDecorations = SystemDecorations.None, // 移除系统装饰（原生标题栏）
            Content = mainContent
        };

        // 设置按钮点击事件
        closeButton.Click += (s, e) => dialogWindow.Close();
        okButton.Click += (s, e) => dialogWindow.Close();

        await dialogWindow.ShowDialog(this);
    }

    private void button2_Click(object? sender, RoutedEventArgs e)
    {
        // 显示云公告 - 使用PanelAlert页面
        ShowPanelAlert();
    }

    /// <summary>
    /// 显示右侧Drawer - 通用方法，供所有RightPanel使用
    /// </summary>
    /// <param name="content">Drawer内容控件</param>
    /// <param name="width">Drawer宽度，默认350</param>
    /// <returns>Drawer窗口实例，可用于手动关闭</returns>
    private async Task<Window> ShowRightDrawerAsync(Control content, double width = 350)
    {
        // 创建遮罩层
        var overlayGrid = new Grid
        {
            Background = new SolidColorBrush(new Color(128, 0, 0, 0)), // 半透明遮罩
            Opacity = 0
        };

        // 创建遮罩点击关闭区域
        var maskBorder = new Border
        {
            Background = Brushes.Transparent,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch
        };
        overlayGrid.Children.Add(maskBorder);

        // Drawer内容容器
        var drawerBorder = new Border
        {
            Width = width,
            Background = this.FindResource("MaterialSurfaceBrush") as IBrush,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch
        };

        // 初始位置在屏幕右侧外
        drawerBorder.RenderTransform = new TranslateTransform((int)width, 0);
        drawerBorder.Child = content;
        overlayGrid.Children.Add(drawerBorder);

        // 创建Popup窗口 - 与主窗口完全重合
        var popupWindow = new Window
        {
            Width = this.Width,
            Height = this.Height, // 全覆盖高度
            WindowStartupLocation = WindowStartupLocation.Manual,
            Position = new PixelPoint(
                (int)this.Position.X,
                (int)this.Position.Y
            ),
            CanResize = false,
            ShowInTaskbar = false,
            SystemDecorations = SystemDecorations.None,
            Background = Brushes.Transparent,
            Content = overlayGrid
        };

        // 点击遮罩关闭
        maskBorder.PointerPressed += (s, e) =>
        {
            _ = CloseRightDrawerAsync(popupWindow, overlayGrid, drawerBorder, (int)width);
        };

        // 显示窗口
        popupWindow.Show(this);

        // 动画：遮罩淡入 + Drawer滑入
        await Task.Delay(10);

        // 遮罩淡入
        for (double i = 0; i <= 1; i += 0.1)
        {
            overlayGrid.Opacity = i;
            await Task.Delay(20);
        }

        // Drawer滑入动画
        int step = (int)(width / 15);
        for (int x = (int)width; x >= 0; x -= step)
        {
            drawerBorder.RenderTransform = new TranslateTransform(x, 0);
            await Task.Delay(10);
        }
        drawerBorder.RenderTransform = new TranslateTransform(0, 0);

        return popupWindow;
    }

    /// <summary>
    /// 关闭右侧Drawer
    /// </summary>
    private async Task CloseRightDrawerAsync(Window popupWindow, Grid overlayGrid, Border drawerBorder, int width)
    {
        // Drawer滑出动画
        int step = width / 15;
        for (int x = 0; x <= width; x += step)
        {
            drawerBorder.RenderTransform = new TranslateTransform(x, 0);
            await Task.Delay(10);
        }

        // 遮罩淡出
        for (double i = 1; i >= 0; i -= 0.1)
        {
            overlayGrid.Opacity = i;
            await Task.Delay(20);
        }

        popupWindow.Close();
    }

    /// <summary>
    /// 显示云公告PanelAlert
    /// </summary>
    private async void ShowPanelAlert()
    {
        var panelAlert = new RightPage.PanelAlert();
        var drawerWindow = await ShowRightDrawerAsync(panelAlert, 350);

        // 绑定关闭事件
        panelAlert.CloseRequested += async (s, e) =>
        {
            if (drawerWindow.Content is Grid grid &&
                grid.Children.Count > 1 &&
                grid.Children[1] is Border border)
            {
                await CloseRightDrawerAsync(drawerWindow, grid, border, 350);
            }
        };
    }

    /// <summary>
    /// 显示自定义邀请信息PanelInviteEdit
    /// </summary>
    public async Task ShowPanelInviteEditAsync()
    {
        var panelInviteEdit = new RightPage.PanelInviteEdit();
        var drawerWindow = await ShowRightDrawerAsync(panelInviteEdit, 350);

        // 绑定关闭事件
        panelInviteEdit.CloseRequested += async (s, e) =>
        {
            if (drawerWindow.Content is Grid grid &&
                grid.Children.Count > 1 &&
                grid.Children[1] is Border border)
            {
                await CloseRightDrawerAsync(drawerWindow, grid, border, 350);
            }
        };
    }

    /// <summary>
    /// 显示玩家管理面板PanelPlayerManager
    /// </summary>
    public async Task ShowPanelPlayerManagerAsync()
    {
        var panelPlayerManager = new RightPage.PanelPlayerManager();
        var drawerWindow = await ShowRightDrawerAsync(panelPlayerManager, 380);

        // 绑定关闭事件
        panelPlayerManager.CloseRequested += async (s, e) =>
        {
            if (drawerWindow.Content is Grid grid &&
                grid.Children.Count > 1 &&
                grid.Children[1] is Border border)
            {
                await CloseRightDrawerAsync(drawerWindow, grid, border, 380);
            }
        };
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void paintLogo()
    {
        Console.WriteLine(@" _____ ______   ___  ________   _______   ________  ________  ________  ________ _________  ________  ________  ________   ________   _______   ________ _________  _________  ________  ________  ___          
|\   _ \  _   \|\  \|\   ___  \|\  ___ \ |\   ____\|\   __  \|\   __  \|\  _____\\___   ___\\   ____\|\   __  \|\   ___  \|\   ___  \|\  ___ \ |\   ____\\___   ___\\___   ___\\   __  \|\   __  \|\  \         
\ \  \\\__\ \  \ \  \ \  \\ \  \ \   __/|\ \  \___|\ \  \|\  \ \  \|\  \ \  \__/\|___ \  \_\ \  \___|\ \  \|\  \ \  \\ \  \ \  \\ \  \ \   __/|\ \  \___\|___ \  \_\|___ \  \_\ \  \|\  \ \  \|\  \ \  \        
 \ \  \\|__| \  \ \  \ \  \\ \  \ \  \_|/_\ \  \    \ \   _  _\ \   __  \ \   __\    \ \  \ \ \  \    \ \  \\\  \ \  \\ \  \ \  \\ \  \ \  \_|/_\ \  \       \ \  \     \ \  \ \ \  \\\  \ \  \\\  \ \  \       
  \ \  \    \ \  \ \  \ \  \\ \  \ \  \_|\ \ \  \____\ \  \\  \\ \  \ \  \ \  \_|     \ \  \ \ \  \____\ \  \\\  \ \  \\ \  \ \  \\ \  \ \  \_|\ \ \  \____   \ \  \     \ \  \ \ \  \\\  \ \  \\\  \ \  \____  
   \ \__\    \ \__\ \__\ \__\\ \__\ \_______\ \_______\ \__\\ _\\ \__\ \__\ \__\       \ \__\ \ \_______\ \_______\ \__\\ \__\ \__\\ \__\ \_______\ \_______\  \ \__\     \ \__\ \ \_______\ \_______\ \_______\
    \|__|     \|__|\|__|\|__| \|__|\|_______|\|_______|\|__|\|__|\|__|\|__|\|__|        \|__|  \|_______|\|_______|\|__| \|__|\|__| \|__|\|_______|\|_______|   \|__|      \|__|  \|_______|\|_______|\|_______|
                                                                                                                                                                                                                
                                                                                                                                                                                                                
                                                                                                                                                                                                                
");
    }
}
