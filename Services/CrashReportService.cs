using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Runtime;
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
    private const string ReportHost = "mctservice.mczlf.loft.games";
    private const int ReportPort = 17600;
    private const int ReportTimeout = 4000;
    private const int FatalExitTimeout = 5000;
    private const int MaxReportBytes = 1024 * 256;
    private const int MaxCrashReportBuilderCapacity = 512 * 1024;
    private static readonly string CrashReportsDirectory;
    private static readonly object LockObject = new();
    private static int _fatalExitWatchdogStarted;

    static CrashReportService()
    {
        CrashReportsDirectory = GetExecutableDirectory();
    }

    public static bool TryRunCrashReportUploader(string[] args)
    {
        if (args.Length < 2 || args[0] != "--upload-crash-report") return false;

        try
        {
            var reportPath = Encoding.UTF8.GetString(Convert.FromBase64String(args[1]));
            if (!File.Exists(reportPath)) return true;

            var report = File.ReadAllText(reportPath, Encoding.UTF8);
            UploadCrashReportAsync(report, Path.GetFileName(reportPath)).Wait(ReportTimeout + 1000);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[崩溃报告] 独立上传失败: {ex.Message}");
        }

        return true;
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
        StartFatalExitWatchdog();
        var exception = e.ExceptionObject as Exception;
        var isTerminating = e.IsTerminating;
        
        GenerateCrashReport(exception, "未处理异常", isTerminating);
        Environment.Exit(1);
    }

    /// <summary>
    /// 处理未观察到的任务异常
    /// </summary>
    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        // 忽略 Linux DBus 相关异常（如 AppMenu.Registrar 服务不存在）
        if (IsDBusException(e.Exception) || IsBenignNetworkCancellationException(e.Exception))
        {
            e.SetObserved();
            return;
        }
        
        GenerateCrashReport(e.Exception, "未观察到的任务异常", false);
        e.SetObserved(); // 标记为已观察，防止进程终止
    }
    
    /// <summary>
    /// 检查是否为 DBus 相关异常
    /// </summary>
    private static bool IsDBusException(AggregateException exception)
    {
        foreach (var inner in exception.Flatten().InnerExceptions)
        {
            var typeName = inner.GetType().FullName ?? "";
            if (typeName.Contains("DBus") || 
                inner.Message.Contains("DBus") ||
                inner.Message.Contains("org.freedesktop"))
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsBenignNetworkCancellationException(AggregateException exception)
    {
        foreach (var inner in exception.Flatten().InnerExceptions)
        {
            if (inner is OperationCanceledException || inner is IOException)
                return true;

            if (inner is System.Net.Sockets.SocketException socketException &&
                (socketException.SocketErrorCode == System.Net.Sockets.SocketError.OperationAborted ||
                 socketException.SocketErrorCode == System.Net.Sockets.SocketError.Interrupted))
            {
                return true;
            }

            if (inner.Message.Contains("已中止 I/O 操作") ||
                inner.Message.Contains("aborted", StringComparison.OrdinalIgnoreCase) ||
                inner.Message.Contains("operation was canceled", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetExecutableDirectory()
    {
        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(processPath))
        {
            var directory = Path.GetDirectoryName(processPath);
            if (!string.IsNullOrWhiteSpace(directory))
                return directory;
        }

        return Directory.GetCurrentDirectory();
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

                StartCrashReportUploaderIfEnabled(filePath);
            }
        }
        catch (Exception ex)
        {
            // 如果连崩溃报告都生成失败，只能输出到调试控制台
            Debug.WriteLine($"[崩溃报告] 生成崩溃报告失败: {ex.Message}");
            Debug.WriteLine($"[崩溃报告] 原始异常: {exception?.ToString()}");
        }
    }

    public static void StartFatalExitWatchdog()
    {
        if (Interlocked.Exchange(ref _fatalExitWatchdogStarted, 1) == 1) return;

        var watchdog = new Thread(() =>
        {
            Thread.Sleep(FatalExitTimeout);
            Environment.Exit(1);
        })
        {
            IsBackground = true
        };
        watchdog.Start();
    }

    private static void StartCrashReportUploaderIfEnabled(string reportPath)
    {
        try
        {
            if (!ConfigService.Read<bool>("AutoReportCrashLog", true)) return;

            var processPath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(processPath)) return;

            var encodedPath = Convert.ToBase64String(Encoding.UTF8.GetBytes(reportPath));
            Process.Start(new ProcessStartInfo
            {
                FileName = processPath,
                Arguments = $"--upload-crash-report {encodedPath}",
                WorkingDirectory = Environment.CurrentDirectory,
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[崩溃报告] 启动独立上传进程失败: {ex.Message}");
        }
    }

    private static async Task UploadCrashReportAsync(string report, string fileName)
    {
        var reportBytes = Encoding.UTF8.GetBytes(report);
        if (reportBytes.Length > MaxReportBytes)
        {
            report = Encoding.UTF8.GetString(reportBytes, 0, MaxReportBytes) + Environment.NewLine + "[报告已截断]";
        }

        var body = $@"====CrashReport====
Version = {MinecraftConnectTool.Views.MainWindow.version}
Time = {DateTime.Now:yyyy-MM-dd HH:mm:ss}
FileName = {fileName}
ContentLength = {Encoding.UTF8.GetByteCount(report)}

{report}";
        var data = Encoding.UTF8.GetBytes(body);

        using var client = new TcpClient();
        using var timeoutCts = new CancellationTokenSource(ReportTimeout);
        await client.ConnectAsync(ReportHost, ReportPort, timeoutCts.Token);

        var stream = client.GetStream();
        stream.WriteTimeout = ReportTimeout;
        stream.ReadTimeout = ReportTimeout;

        await stream.WriteAsync(data, timeoutCts.Token);
        await stream.FlushAsync(timeoutCts.Token);
        await Task.Delay(500, timeoutCts.Token);
    }

    /// <summary>
    /// 构建崩溃报告内容
    /// </summary>
    private static string BuildCrashReport(Exception? exception, string crashType, bool isFatal)
    {
        var sb = new StringBuilder(32 * 1024);
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
        sb.AppendLine($"  崩溃UTC时间: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}Z");
        sb.AppendLine($"  崩溃类型: {crashType}");
        sb.AppendLine($"  是否致命: {(isFatal ? "是" : "否")}");
        sb.AppendLine($"  程序版本: {version}");
        sb.AppendLine($"  报告ID: {Guid.NewGuid():N}");
        sb.AppendLine($"  进程启动时间: {GetSafeValue(() => Process.GetCurrentProcess().StartTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))}");
        sb.AppendLine($"  进程运行时长: {GetSafeValue(() => FormatDuration(DateTime.Now - Process.GetCurrentProcess().StartTime))}");
        sb.AppendLine($"  命令行: {MaskSensitiveText(Environment.CommandLine)}");
        sb.AppendLine($"  程序路径: {Environment.ProcessPath}");
        sb.AppendLine();

        AppendSafeSection(sb, "系统信息", AppendSystemInfo);
        AppendSafeSection(sb, "进程信息", AppendProcessInfo);
        AppendSafeSection(sb, "运行时信息", AppendRuntimeInfo);
        AppendSafeSection(sb, "内存信息", AppendMemoryInfo);
        AppendSafeSection(sb, "存储路径", AppendStorageInfo);

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

        AppendSafeSection(sb, "线程信息", AppendThreadInfo);
        AppendSafeSection(sb, "最近应用日志", AppendRecentAppLog);
        AppendSafeSection(sb, "最近页面运行日志", AppendRecentPageLogs);

        TrimReportBuilderIfNeeded(sb);

        // ========== 结尾 ==========
        sb.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
        sb.AppendLine("请将此报告提交给开发者以帮助修复问题。");
        sb.AppendLine($"崩溃报告保存位置: {CrashReportsDirectory}");
        sb.AppendLine("═══════════════════════════════════════════════════════════════════════════════");

        return sb.ToString();
    }

    private static void AppendSafeSection(StringBuilder sb, string title, Action<StringBuilder> appendAction)
    {
        sb.AppendLine($"【{title}】");
        try
        {
            appendAction(sb);
        }
        catch (Exception ex)
        {
            sb.AppendLine($"  收集失败: {ex.GetType().FullName}: {ex.Message}");
        }
        sb.AppendLine();
    }

    private static void AppendSystemInfo(StringBuilder sb)
    {
        sb.AppendLine($"  操作系统: {RuntimeInformation.OSDescription}");
        sb.AppendLine($"  OS版本: {Environment.OSVersion}");
        sb.AppendLine($"  系统架构: {RuntimeInformation.OSArchitecture}");
        sb.AppendLine($"  进程架构: {RuntimeInformation.ProcessArchitecture}");
        sb.AppendLine($"  64位系统: {Environment.Is64BitOperatingSystem}");
        sb.AppendLine($"  64位进程: {Environment.Is64BitProcess}");
        sb.AppendLine($"  处理器数量: {Environment.ProcessorCount}");
    }

    private static void AppendProcessInfo(StringBuilder sb)
    {
        using var proc = Process.GetCurrentProcess();
        sb.AppendLine($"  进程ID: {Environment.ProcessId}");
        sb.AppendLine($"  进程名称: {proc.ProcessName}");
        sb.AppendLine($"  主模块: {GetSafeValue(() => proc.MainModule?.FileName ?? string.Empty)}");
        sb.AppendLine($"  工作目录: {Environment.CurrentDirectory}");
        sb.AppendLine($"  基础目录: {AppContext.BaseDirectory}");
        sb.AppendLine($"  当前托管线程ID: {Environment.CurrentManagedThreadId}");
        sb.AppendLine($"  线程数: {GetSafeValue(() => proc.Threads.Count.ToString(CultureInfo.InvariantCulture))}");
    }

    private static void AppendRuntimeInfo(StringBuilder sb)
    {
        sb.AppendLine($"  运行时版本: {RuntimeInformation.FrameworkDescription}");
        sb.AppendLine($"  Environment.Version: {Environment.Version}");
        sb.AppendLine($"  GC延迟模式: {GCSettings.LatencyMode}");
        sb.AppendLine($"  GC是否服务器模式: {GCSettings.IsServerGC}");
        sb.AppendLine($"  GC已用代数: {GC.MaxGeneration}");
        sb.AppendLine($"  当前托管内存: {FormatBytes(GC.GetTotalMemory(false))}");
    }

    private static void AppendMemoryInfo(StringBuilder sb)
    {
        using var proc = Process.GetCurrentProcess();
        var gcInfo = GC.GetGCMemoryInfo();
        sb.AppendLine($"  工作集内存: {FormatBytes(proc.WorkingSet64)}");
        sb.AppendLine($"  峰值工作集: {FormatBytes(proc.PeakWorkingSet64)}");
        sb.AppendLine($"  私有内存: {FormatBytes(proc.PrivateMemorySize64)}");
        sb.AppendLine($"  虚拟内存: {FormatBytes(proc.VirtualMemorySize64)}");
        sb.AppendLine($"  峰值虚拟内存: {FormatBytes(proc.PeakVirtualMemorySize64)}");
        sb.AppendLine($"  GC总内存: {FormatBytes(GC.GetTotalMemory(false))}");
        sb.AppendLine($"  GC堆大小: {FormatBytes(gcInfo.HeapSizeBytes)}");
        sb.AppendLine($"  GC负载字节: {FormatBytes(gcInfo.MemoryLoadBytes)}");
        sb.AppendLine($"  GC高内存阈值: {FormatBytes(gcInfo.HighMemoryLoadThresholdBytes)}");
        sb.AppendLine($"  已碎片化内存: {FormatBytes(gcInfo.FragmentedBytes)}");
        sb.AppendLine($"  Gen0回收次数: {GC.CollectionCount(0)}");
        sb.AppendLine($"  Gen1回收次数: {GC.CollectionCount(1)}");
        sb.AppendLine($"  Gen2回收次数: {GC.CollectionCount(2)}");
    }

    private static void AppendStorageInfo(StringBuilder sb)
    {
        sb.AppendLine($"  崩溃报告目录: {CrashReportsDirectory}");
        sb.AppendLine($"  应用根目录: {LocalStorageService.AppRootDirectory}");
        sb.AppendLine($"  临时目录: {LocalStorageService.TempDirectory}");
        sb.AppendLine($"  配置文件: {LocalStorageService.ConfigFilePath}");
        sb.AppendLine($"  应用日志: {LocalStorageService.AppLogPath}");
        sb.AppendLine($"  存储模式: {LocalStorageService.StorageMode}");
        AppendFileInfo(sb, "配置文件状态", LocalStorageService.ConfigFilePath);
        AppendFileInfo(sb, "应用日志状态", LocalStorageService.AppLogPath);
    }

    private static void AppendThreadInfo(StringBuilder sb)
    {
        sb.AppendLine($"  当前线程ID: {Environment.CurrentManagedThreadId}");
        sb.AppendLine($"  线程池线程: {Thread.CurrentThread.IsThreadPoolThread}");
        sb.AppendLine($"  后台线程: {Thread.CurrentThread.IsBackground}");
        sb.AppendLine($"  线程优先级: {Thread.CurrentThread.Priority}");
        ThreadPool.GetAvailableThreads(out var workerThreads, out var completionPortThreads);
        ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxCompletionPortThreads);
        sb.AppendLine($"  线程池可用工作线程: {workerThreads}/{maxWorkerThreads}");
        sb.AppendLine($"  线程池可用IO线程: {completionPortThreads}/{maxCompletionPortThreads}");
    }

    private static void AppendRecentAppLog(StringBuilder sb)
    {
        AppendLogStats(sb, "APPLog.ini", LocalStorageService.AppLogPath);
    }

    private static void AppendRecentPageLogs(StringBuilder sb)
    {
        AppendPageLogStats(sb, "Link模式", Path.Combine(LocalStorageService.TempDirectory, "TempRunLog", "Link.log"));
        AppendPageLogStats(sb, "P2P模式", Path.Combine(LocalStorageService.TempDirectory, "TempRunLog", "P2P.log"));
        AppendPageLogStats(sb, "ET模式", Path.Combine(LocalStorageService.TempDirectory, "TempRunLog", "ET.log"));
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

    private static void AppendFileInfo(StringBuilder sb, string name, string path)
    {
        if (!File.Exists(path))
        {
            sb.AppendLine($"  {name}: 不存在 ({path})");
            return;
        }

        var fileInfo = new FileInfo(path);
        sb.AppendLine($"  {name}: 存在, 大小 {FormatBytes(fileInfo.Length)}, 修改时间 {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}, 路径 {path}");
    }

    private static void AppendLogStats(StringBuilder sb, string name, string path)
    {
        sb.AppendLine($"  ---- {name} ----");
        if (!File.Exists(path))
        {
            sb.AppendLine($"  日志不存在: {path}");
            return;
        }

        var fileInfo = new FileInfo(path);
        var lineCount = CountFileLines(path);
        var charCount = CountFileChars(path);
        sb.AppendLine($"  路径: {path}");
        sb.AppendLine($"  大小: {FormatBytes(fileInfo.Length)}");
        sb.AppendLine($"  字符数: {charCount}");
        sb.AppendLine($"  行数: {lineCount}");
        sb.AppendLine($"  修改时间: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
    }

    private static void AppendPageLogStats(StringBuilder sb, string name, string path)
    {
        sb.AppendLine($"  ---- {name} ----");
        if (!File.Exists(path))
        {
            sb.AppendLine($"  日志不存在: {path}");
            return;
        }

        var fileInfo = new FileInfo(path);
        var charCount = CountFileChars(path);
        sb.AppendLine($"  大小/字符数: {FormatBytes(fileInfo.Length)}/{charCount}字符");
    }

    private static long CountFileLines(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var buffer = new byte[8192];
        long lineCount = 0;
        var hasAnyByte = false;
        var lastByte = (byte)0;

        while (true)
        {
            var read = stream.Read(buffer, 0, buffer.Length);
            if (read == 0)
                break;

            hasAnyByte = true;
            for (var i = 0; i < read; i++)
            {
                if (buffer[i] == (byte)'\n')
                    lineCount++;
            }
            lastByte = buffer[read - 1];
        }

        if (hasAnyByte && lastByte != (byte)'\n')
            lineCount++;

        return lineCount;
    }

    private static long CountFileChars(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 8192);
        var buffer = new char[8192];
        long charCount = 0;

        while (true)
        {
            var read = reader.Read(buffer, 0, buffer.Length);
            if (read == 0)
                break;

            charCount += read;
        }

        return charCount;
    }

    private static string MaskSensitiveText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var sensitiveWords = new[] { "token", "password", "passwd", "secret", "apikey", "api_key", "authorization", "cookie", "session", "accesskey", "privatekey" };
        foreach (var word in sensitiveWords)
        {
            if (text.Contains(word, StringComparison.OrdinalIgnoreCase))
                return "[已隐藏疑似敏感内容]";
        }

        return text;
    }

    private static string GetSafeValue(Func<string> valueFactory)
    {
        try
        {
            return valueFactory();
        }
        catch (Exception ex)
        {
            return $"收集失败: {ex.GetType().Name}";
        }
    }

    private static string FormatDuration(TimeSpan duration)
    {
        return $"{(int)duration.TotalDays}天 {duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
    }

    private static void TrimReportBuilderIfNeeded(StringBuilder sb)
    {
        if (sb.Length <= MaxCrashReportBuilderCapacity)
            return;

        var keepTailLength = MaxCrashReportBuilderCapacity / 2;
        var tail = sb.ToString(sb.Length - keepTailLength, keepTailLength);
        sb.Clear();
        sb.AppendLine("[崩溃报告内容过长，已保留关键头部并截断中间内容]");
        sb.AppendLine(tail);
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
