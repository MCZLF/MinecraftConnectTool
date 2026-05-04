using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MinecraftConnectTool.Services;

/// <summary>
/// 崩溃报告服务 - 捕获未处理异常并生成TXT崩溃报告
/// </summary>
public static class CrashReportService
{
    private static readonly string CrashReportsDirectory;
    private static readonly object LockObject = new();

    static CrashReportService()
    {
        // 崩溃报告保存到程序同目录下
        CrashReportsDirectory = AppContext.BaseDirectory;
    }

    /// <summary>
    /// 注册全局异常处理
    /// </summary>
    public static void RegisterGlobalExceptionHandlers()
    {
        // 捕获UI线程异常
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        
        // 捕获非UI线程异常
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        
        // 对于Avalonia应用，还需要处理特定异常
        // 注意：Avalonia的异常处理在App.axaml.cs中单独处理
    }

    /// <summary>
    /// 处理未捕获的异常
    /// </summary>
    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        var isTerminating = e.IsTerminating;
        
        GenerateCrashReport(exception, "未处理异常", isTerminating);
        
        // 给一点时间让日志写入，然后退出程序
        Task.Delay(500).Wait();
        Environment.Exit(1);
    }

    /// <summary>
    /// 处理未观察到的任务异常
    /// </summary>
    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        GenerateCrashReport(e.Exception, "未观察到的任务异常", false);
        e.SetObserved(); // 标记为已观察，防止进程终止
    }

    /// <summary>
    /// 生成崩溃报告
    /// </summary>
    public static void GenerateCrashReport(Exception? exception, string crashType = "未知错误", bool isFatal = true)
    {
        try
        {
            lock (LockObject)
            {
                // 确保目录存在
                if (!Directory.Exists(CrashReportsDirectory))
                {
                    Directory.CreateDirectory(CrashReportsDirectory);
                }

                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var fileName = $"MinecraftConnectTool崩溃报告_{timestamp}.txt";
                var filePath = Path.Combine(CrashReportsDirectory, fileName);

                var report = BuildCrashReport(exception, crashType, isFatal);
                File.WriteAllText(filePath, report, Encoding.UTF8);

                // 同时输出到调试控制台
                Debug.WriteLine($"[崩溃报告] 已生成崩溃报告: {filePath}");
                
                // 打开崩溃报告文件
                try
                {
                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                }
                catch
                {
                    // 忽略打开失败
                }
            }
        }
        catch (Exception ex)
        {
            // 如果连崩溃报告都生成失败，只能输出到调试控制台
            Debug.WriteLine($"[崩溃报告] 生成崩溃报告失败: {ex.Message}");
            Debug.WriteLine($"[崩溃报告] 原始异常: {exception?.ToString()}");
        }
    }

    /// <summary>
    /// 构建崩溃报告内容
    /// </summary>
    private static string BuildCrashReport(Exception? exception, string crashType, bool isFatal)
    {
        var sb = new StringBuilder();
        // 从 MainWindow 获取版本号
        var version = MinecraftConnectTool.Views.MainWindow.version;

        // ========== 标题 ==========
        sb.AppendLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║                    MinecraftConnectTool 崩溃报告                              ║");
        sb.AppendLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        sb.AppendLine();

        // ========== 基本信息 ==========
        sb.AppendLine("【基本信息】");
        sb.AppendLine($"  崩溃时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"  崩溃类型: {crashType}");
        sb.AppendLine($"  是否致命: {(isFatal ? "是" : "否")}");
        sb.AppendLine($"  程序版本: {version}");
        sb.AppendLine($"  报告ID: {Guid.NewGuid():N}");
        sb.AppendLine();

        // ========== 系统信息 ==========
        sb.AppendLine("【系统信息】");
        sb.AppendLine($"  操作系统: {RuntimeInformation.OSDescription}");
        sb.AppendLine($"  系统架构: {RuntimeInformation.OSArchitecture}");
        sb.AppendLine($"  进程架构: {RuntimeInformation.ProcessArchitecture}");
        sb.AppendLine($"  运行时版本: {RuntimeInformation.FrameworkDescription}");
        sb.AppendLine($"  机器名称: {Environment.MachineName}");
        sb.AppendLine($"  用户名: {Environment.UserName}");
        sb.AppendLine($"  进程ID: {Environment.ProcessId}");
        sb.AppendLine($"  工作目录: {Environment.CurrentDirectory}");
        sb.AppendLine();

        // ========== 内存信息 ==========
        sb.AppendLine("【内存信息】");
        var proc = Process.GetCurrentProcess();
        sb.AppendLine($"  工作集内存: {FormatBytes(proc.WorkingSet64)}");
        sb.AppendLine($"  私有内存: {FormatBytes(proc.PrivateMemorySize64)}");
        sb.AppendLine($"  虚拟内存: {FormatBytes(proc.VirtualMemorySize64)}");
        sb.AppendLine($"  GC总内存: {FormatBytes(GC.GetTotalMemory(false))}");
        sb.AppendLine($"  GC已用代数: {GC.MaxGeneration}");
        sb.AppendLine();

        // ========== 异常详情 ==========
        sb.AppendLine("【异常详情】");
        if (exception != null)
        {
            AppendExceptionDetails(sb, exception, 0);
        }
        else
        {
            sb.AppendLine("  (无异常信息)");
        }
        sb.AppendLine();

        // ========== 堆栈跟踪 ==========
        sb.AppendLine("【当前堆栈】");
        sb.AppendLine(Environment.StackTrace);
        sb.AppendLine();

        // ========== 线程信息 ==========
        sb.AppendLine("【线程信息】");
        sb.AppendLine($"  当前线程ID: {Environment.CurrentManagedThreadId}");
        sb.AppendLine($"  线程池线程: {Thread.CurrentThread.IsThreadPoolThread}");
        sb.AppendLine($"  后台线程: {Thread.CurrentThread.IsBackground}");
        sb.AppendLine($"  线程优先级: {Thread.CurrentThread.Priority}");
        sb.AppendLine();

        // ========== 环境变量 ==========
        sb.AppendLine("【环境变量】");
        sb.AppendLine($"  DOTNET_ROOT: {Environment.GetEnvironmentVariable("DOTNET_ROOT")}");
        sb.AppendLine($"  DOTNET_ENVIRONMENT: {Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}");
        sb.AppendLine($"  ASPNETCORE_ENVIRONMENT: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");
        sb.AppendLine();

        // ========== 结尾 ==========
        sb.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
        sb.AppendLine("请将此报告提交给开发者以帮助修复问题。");
        sb.AppendLine($"崩溃报告保存位置: {CrashReportsDirectory}");
        sb.AppendLine("═══════════════════════════════════════════════════════════════════════════════");

        return sb.ToString();
    }

    /// <summary>
    /// 递归追加异常详情
    /// </summary>
    private static void AppendExceptionDetails(StringBuilder sb, Exception exception, int level)
    {
        var indent = new string(' ', level * 2);
        
        sb.AppendLine($"{indent}异常类型: {exception.GetType().FullName}");
        sb.AppendLine($"{indent}异常消息: {exception.Message}");
        sb.AppendLine($"{indent}来源: {exception.Source}");
        sb.AppendLine($"{indent}HResult: 0x{exception.HResult:X8}");
        
        if (!string.IsNullOrEmpty(exception.StackTrace))
        {
            sb.AppendLine($"{indent}堆栈跟踪:");
            foreach (var line in exception.StackTrace.Split('\n'))
            {
                sb.AppendLine($"{indent}  {line.Trim()}");
            }
        }

        if (exception.InnerException != null)
        {
            sb.AppendLine();
            sb.AppendLine($"{indent}【内部异常】");
            AppendExceptionDetails(sb, exception.InnerException, level + 1);
        }

        // 记录Data集合
        if (exception.Data.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"{indent}【附加数据】");
            foreach (var key in exception.Data.Keys)
            {
                sb.AppendLine($"{indent}  {key}: {exception.Data[key]}");
            }
        }
    }

    /// <summary>
    /// 格式化字节大小
    /// </summary>
    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.00} {sizes[order]}";
    }

    /// <summary>
    /// 获取崩溃报告目录路径
    /// </summary>
    public static string GetCrashReportsDirectory()
    {
        return CrashReportsDirectory;
    }
}
