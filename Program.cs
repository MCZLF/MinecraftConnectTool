using Avalonia;
using Avalonia.Platform;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using MinecraftConnectTool.Services;

namespace MinecraftConnectTool;

sealed class Program
{
    // 管理员状态: 0=未尝试, 1=已是管理员, 2=用户拒绝UAC
    public static int AdminStatus { get; private set; } = 0;

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // 注册全局异常处理（必须在最前面）
        CrashReportService.RegisterGlobalExceptionHandlers();

        try
        {
            // Windows 平台尝试以管理员权限启动
            if (OperatingSystem.IsWindows() && !args.Contains("--admin"))
            {
                TryRestartAsAdmin();
            }
            else if (args.Contains("--admin"))
            {
                AdminStatus = 1;
            }

            // 在启动Avalonia之前执行性能优化
            ApplyPerformanceOptimizations();

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            // 捕获启动过程中的任何异常
            CrashReportService.GenerateCrashReport(ex, "程序启动异常", true);
            throw;
        }
    }

    /// <summary>
    /// 尝试以管理员权限重新启动（仅Windows）
    /// </summary>
    private static void TryRestartAsAdmin()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = Environment.ProcessPath ?? System.Reflection.Assembly.GetExecutingAssembly().Location,
                Arguments = "--admin",
                Verb = "runas" // 请求管理员权限
            };

            using (var proc = Process.Start(startInfo))
            {
                if (proc != null)
                {
                    // 等待新进程启动后退出当前进程
                    Environment.Exit(0);
                }
            }
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // 用户拒绝UAC，继续以普通权限运行
            AdminStatus = 2;
        }
        catch
        {
            // 其他异常，继续以普通权限运行
            AdminStatus = 0;
        }
    }

    /// <summary>
    /// 应用性能优化（仅在性能模式开启时）
    /// </summary>
    private static void ApplyPerformanceOptimizations()
    {
        if (!IsPerformanceModeEnabled())
            return;

        // 设置GC模式为Batch（低内存占用）
        GCSettings.LatencyMode = GCLatencyMode.Batch;
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;

        // 启动时执行三重GC，清理初始化过程中的临时对象
        for (int i = 0; i < 3; i++)
        {
            GC.Collect(2, GCCollectionMode.Aggressive, true, true);
            GC.WaitForPendingFinalizers();
        }
    }

    /// <summary>
    /// 检查性能模式是否开启（直接读取配置文件，不依赖服务）
    /// </summary>
    private static bool IsPerformanceModeEnabled()
    {
        try
        {
            var tempPath = Environment.GetEnvironmentVariable("TEMP") ?? Path.GetTempPath();
            var configPath = Path.Combine(tempPath, "MCZLFAPP", "Temp", "APPconfig.json");

            if (!File.Exists(configPath))
                return false;

            var json = File.ReadAllText(configPath);
            var config = JsonNode.Parse(json)?.AsObject();

            if (config?.TryGetPropertyValue("EnablePerformanceMode", out var value) == true)
            {
                return value?.GetValue<bool>() ?? false;
            }
        }
        catch
        {
            // 读取失败时默认关闭性能模式
        }
        return false;
    }

    /// <summary>
    /// 获取渲染方式设置
    /// </summary>
    private static RenderingMode GetRenderingMode()
    {
        try
        {
            var tempPath = Environment.GetEnvironmentVariable("TEMP") ?? Path.GetTempPath();
            var configPath = Path.Combine(tempPath, "MCZLFAPP", "Temp", "APPconfig.json");

            if (!File.Exists(configPath))
                return RenderingMode.SystemDefault;

            var json = File.ReadAllText(configPath);
            var config = JsonNode.Parse(json)?.AsObject();

            if (config?.TryGetPropertyValue("RenderingMode", out var value) == true)
            {
                var modeStr = value?.GetValue<string>() ?? "SystemDefault";
                if (Enum.TryParse<RenderingMode>(modeStr, out var mode))
                {
                    return mode;
                }
            }
        }
        catch
        {
            // 读取失败时使用系统默认
        }
        return RenderingMode.SystemDefault;
    }

    /// <summary>
    /// 获取 Win32 渲染模式列表
    /// </summary>
    private static Win32RenderingMode[] GetWin32RenderingModes()
    {
        // 性能模式强制使用软件渲染
        if (IsPerformanceModeEnabled())
        {
            return [Win32RenderingMode.Software];
        }

        var renderingMode = GetRenderingMode();
        return renderingMode switch
        {
            RenderingMode.Gpu => [Win32RenderingMode.AngleEgl],
            RenderingMode.Cpu => [Win32RenderingMode.Software],
            _ => [Win32RenderingMode.AngleEgl, Win32RenderingMode.Software] // SystemDefault
        };
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .With(new Win32PlatformOptions
            {
                RenderingMode = GetWin32RenderingModes()
            });
}
