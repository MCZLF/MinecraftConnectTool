using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Documents;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MinecraftConnectTool.Services;
using MinecraftConnectTool.ViewModels;
using MinecraftConnectTool.Views;

namespace MinecraftConnectTool;

public partial class App : Application, IDisposable
{
    private Timer? _gcTimer;
    private bool _isPerformanceModeEnabled;
    private Styles? _globalBoldStyles;

    public override void Initialize()
    {
        // 注册Avalonia UI线程异常处理
        Dispatcher.UIThread.UnhandledException += OnUIThreadException;
        
        AvaloniaXamlLoader.Load(this);
    }
    
    /// <summary>
    /// 处理UI线程未捕获异常
    /// </summary>
    private void OnUIThreadException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        CrashReportService.GenerateCrashReport(e.Exception, "UI线程未处理异常", true);
        e.Handled = true; // 标记为已处理
        
        // 退出程序
        Environment.Exit(1);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // 初始化主题服务
        ThemeService.Instance.Initialize();

        // 订阅全局文字加粗设置变更事件
        ThemeService.Instance.GlobalBoldTextChanged += OnGlobalBoldTextChanged;

        // 应用初始全局文字加粗样式
        ApplyGlobalBoldTextStyle();

        // 检查是否是首次启动（同时检查全局开关EnableFirstLaunchWizard）
        bool alreadyFirstGuild = ConfigService.Read<bool>("AlreadyFirstGuild", false);
        bool enableFirstLaunchWizard = MainWindow.EnableFirstLaunchWizard;

        if (!alreadyFirstGuild && enableFirstLaunchWizard)
        {
            // 首次启动且启用了向导，显示引导向导
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                DisableAvaloniaDataAnnotationValidation();
                var wizardWindow = new FirstLaunchWizardWindow();
                desktop.MainWindow = wizardWindow;
                wizardWindow.Show();
                desktop.Exit += OnApplicationExit;
            }
        }
        else
        {
            // 非首次启动，正常启动主窗口
            // 检查性能模式是否开启
            _isPerformanceModeEnabled = ConfigService.Read<bool>("EnablePerformanceMode", false);

            // 启动GC定时器（默认模式和性能模式都启用，但策略不同）
            StartGcTimer();

            // 发送 Probe 上报（静默执行，不阻塞启动）
            _ = SendProbeAsync();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };

                // 应用退出时清理资源
                desktop.Exit += OnApplicationExit;
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// 启动GC定时器（根据模式选择不同策略）
    /// </summary>
    private void StartGcTimer()
    {
        if (_isPerformanceModeEnabled)
        {
            // 性能模式：激进GC，每2秒执行一次
            _gcTimer = new Timer(_ =>
            {
                // 执行激进GC，释放内存
                for (int i = 0; i < 3; i++)
                {
                    GC.Collect(2, GCCollectionMode.Aggressive, true, true);
                    GC.WaitForPendingFinalizers();
                }
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(2, GCCollectionMode.Aggressive, true, true);
            }, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
        }
        else
        {
            // 默认模式：每10秒执行一次GC，保持内存低于100MB
            _gcTimer = new Timer(_ =>
            {
                // 检查内存使用，如果超过80MB则执行更积极的GC
                var proc = Process.GetCurrentProcess();
                proc.Refresh();
                var memoryMB = proc.WorkingSet64 / (1024 * 1024);

                if (memoryMB > 80)
                {
                    // 内存较高，执行积极GC
                    GC.Collect(2, GCCollectionMode.Optimized, true);
                    GC.WaitForPendingFinalizers();
                    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                    GC.Collect(2, GCCollectionMode.Optimized, true);
                }
                else
                {
                    // 内存正常，执行温和GC
                    GC.Collect(1, GCCollectionMode.Optimized, false);
                }
            }, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
        }
    }

    /// <summary>
    /// 发送 Probe 上报
    /// </summary>
    private async Task SendProbeAsync()
    {
        try
        {
            // 设置版本号
            ProbeService.Version = MainWindow.version;
            ProbeService.EnablePopup = false; // 静默模式

            // 发送 Probe
            await ProbeService.SendAsync();
        }
        catch
        {
            // 静默处理异常，不影响应用启动
        }
    }

    /// <summary>
    /// 应用退出时清理资源
    /// </summary>
    private void OnApplicationExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        // 取消订阅事件
        ThemeService.Instance.GlobalBoldTextChanged -= OnGlobalBoldTextChanged;
        Dispose();
    }

    /// <summary>
    /// 全局文字加粗设置变更处理
    /// </summary>
    private void OnGlobalBoldTextChanged(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            ApplyGlobalBoldTextStyle();
        });
    }

    /// <summary>
    /// 应用全局文字加粗样式 - 使用 Avalonia 样式系统，覆盖所有文本控件
    /// </summary>
    private void ApplyGlobalBoldTextStyle()
    {
        var enableBold = ThemeService.Instance.EnableGlobalBoldText;

        // 移除旧的样式
        if (_globalBoldStyles != null)
        {
            Styles.Remove(_globalBoldStyles);
            _globalBoldStyles = null;
        }

        if (enableBold)
        {
            // 创建全局加粗样式
            _globalBoldStyles = new Styles();

            // 基础文本控件
            AddBoldStyle<TextBlock>(_globalBoldStyles);
            AddBoldStyle<Button>(_globalBoldStyles);
            AddBoldStyle<Label>(_globalBoldStyles);
            AddBoldStyle<TextBox>(_globalBoldStyles);
            AddBoldStyle<ComboBox>(_globalBoldStyles);
            AddBoldStyle<ToggleSwitch>(_globalBoldStyles);
            AddBoldStyle<RadioButton>(_globalBoldStyles);
            AddBoldStyle<CheckBox>(_globalBoldStyles);

            // 列表和菜单控件
            AddBoldStyle<ListBox>(_globalBoldStyles);
            AddBoldStyle<ListBoxItem>(_globalBoldStyles);
            AddBoldStyle<MenuItem>(_globalBoldStyles);
            AddBoldStyle<TabItem>(_globalBoldStyles);
            AddBoldStyle<TreeViewItem>(_globalBoldStyles);

            // 选择控件
            AddBoldStyle<Slider>(_globalBoldStyles);
            AddBoldStyle<ProgressBar>(_globalBoldStyles);

            // 容器控件（如果它们有文本内容）
            AddBoldStyle<Expander>(_globalBoldStyles);

            // 添加到应用样式
            Styles.Add(_globalBoldStyles);

            // 强制应用到当前所有已加载的控件（覆盖内联设置）
            ForceApplyBoldToAllControls();
        }
        else
        {
            // 关闭加粗时，恢复所有控件的默认字体粗细
            ForceResetBoldToAllControls();
        }
    }

    /// <summary>
    /// 为指定控件类型添加加粗样式
    /// </summary>
    private void AddBoldStyle<T>(Styles styles) where T : Control
    {
        styles.Add(new Style(x => x.OfType<T>())
        {
            Setters = { new Setter(TextElement.FontWeightProperty, Avalonia.Media.FontWeight.Bold) }
        });
    }

    /// <summary>
    /// 强制将加粗样式应用到所有已加载的控件
    /// </summary>
    private void ForceApplyBoldToAllControls()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            foreach (var window in desktop.Windows)
            {
                ForceApplyBoldToVisual(window);
            }
        }
    }

    /// <summary>
    /// 递归强制应用加粗到视觉树中的所有文本控件
    /// </summary>
    private void ForceApplyBoldToVisual(Visual visual)
    {
        // 对 TextBlock 直接设置 FontWeight 属性，覆盖内联值
        if (visual is TextBlock textBlock)
        {
            textBlock.FontWeight = Avalonia.Media.FontWeight.Bold;
        }

        // 递归处理子控件
        foreach (var child in visual.GetVisualChildren())
        {
            ForceApplyBoldToVisual(child);
        }
    }

    /// <summary>
    /// 强制重置所有已加载控件的字体粗细为默认值
    /// </summary>
    private void ForceResetBoldToAllControls()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            foreach (var window in desktop.Windows)
            {
                ForceResetBoldToVisual(window);
            }
        }
    }

    /// <summary>
    /// 递归重置视觉树中所有文本控件的字体粗细为默认值
    /// </summary>
    private void ForceResetBoldToVisual(Visual visual)
    {
        // 对 TextBlock 重置 FontWeight 属性为 Normal
        if (visual is TextBlock textBlock)
        {
            textBlock.FontWeight = Avalonia.Media.FontWeight.Normal;
        }

        // 递归处理子控件
        foreach (var child in visual.GetVisualChildren())
        {
            ForceResetBoldToVisual(child);
        }
    }

    public void Dispose()
    {
        _gcTimer?.Dispose();
        _gcTimer = null;
        GC.SuppressFinalize(this);
    }

    private static void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
