using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MinecraftConnectTool.Services;
using MinecraftConnectTool.ViewModels;
using MinecraftConnectTool.Views;

namespace MinecraftConnectTool;

public partial class App : Application, IDisposable
{
    private Timer? _gcTimer;
    private bool _isPerformanceModeEnabled;

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
        Dispose();
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
