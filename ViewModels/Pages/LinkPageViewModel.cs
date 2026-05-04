using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinecraftConnectTool.Services;

namespace MinecraftConnectTool.ViewModels.Pages;

public partial class LinkPageViewModel : ViewModelBase
{
    // ========== 基础字段 ==========
    public string role = "0"; // 0=未开启  1=房主 2=加入方
    private Process? _linkProcess;
    private readonly List<object> _floatButtons = new List<object>();
    private string? _pendingClipboardText;

    // 玩家列表服务
    private readonly PlayerListService _playerListService;

    // ========== 事件 ==========
    /// <summary>
    /// 请求端口输入事件，参数为默认端口，返回用户输入的端口（null表示取消）
    /// </summary>
    public event Func<string, Task<string?>>? RequestPortInput;

    /// <summary>
    /// 打开玩家管理面板请求事件
    /// </summary>
    public event EventHandler? OpenPlayerManagerRequested;

    // ========== 可观察属性 ==========
    [ObservableProperty]
    private string _pageText = "这是 Link 模式页面 - 用于链接模式连接";

    [ObservableProperty]
    private string _connectionInfo = "连接信息: 未配置";

    [ObservableProperty]
    private string _logText = "";

    [ObservableProperty]
    private string _promptCode = "";

    [ObservableProperty]
    private string _joinCode = "";

    [ObservableProperty]
    private bool _isStatusBadgeVisible = false;

    [ObservableProperty]
    private string _statusBadgeText = "Null";

    [ObservableProperty]
    private BadgeState _statusBadgeState = BadgeState.Default;

    [ObservableProperty]
    private bool _isProgressVisible = false;

    [ObservableProperty]
    private int _progressValue = 0;

    [ObservableProperty]
    private bool _isAlertVisible = false;

    [ObservableProperty]
    private string _alertText = "";

    [ObservableProperty]
    private bool _isInfoButtonVisible = false;

    [ObservableProperty]
    private string _infoButtonText = "";

    [ObservableProperty]
    private bool _isCommandInputVisible = false;

    [ObservableProperty]
    private string _commandText = "";

    [ObservableProperty]
    private bool _isLabelVisible = false;

    [ObservableProperty]
    private string _labelText = "";

    // 工作模式badge属性
    [ObservableProperty]
    private bool _isWorkModeBadgeVisible = false;

    [ObservableProperty]
    private string _workModeBadgeText = "";

    // 错误提示窗属性
    [ObservableProperty]
    private bool _isErrorToastVisible = false;

    [ObservableProperty]
    private string _errorToastText = "";

    [ObservableProperty]
    private double _errorToastOpacity = 0;

    private System.Threading.CancellationTokenSource? _toastCancellationTokenSource;

    [ObservableProperty]
    private bool _isCustomPortEnabled = false;

    [ObservableProperty]
    private bool _isMulticastEnabled = false;

    // 玩家列表相关属性
    [ObservableProperty]
    private bool _isPlayerListConnected = false;

    // ========== 轮询相关 ==========
    private System.Timers.Timer? _statusTimer;
    private string statusCommandFeedback = string.Empty;

    // ========== 平台检测 ==========
    private static bool IsWindows => OperatingSystem.IsWindows();
    private static bool IsLinux => OperatingSystem.IsLinux();
    private static bool IsMacOS => OperatingSystem.IsMacOS();
    private static bool IsArm64 => RuntimeInformation.ProcessArchitecture == Architecture.Arm64;
    private static bool IsX64 => RuntimeInformation.ProcessArchitecture == Architecture.X64;

    // ========== 平台相关配置 ==========
    private static string GetCoreFileName()
    {
        if (IsWindows) return "link.exe";
        return "link"; // Linux 和 Mac 没有 .exe 后缀
    }

    private static string GetCoreDownloadUrl()
    {
        if (IsWindows)
        {
            return IsArm64
                ? "https://mczlf.loft.games/API/winArm64.exe"
                : "https://mczlf.loft.games/API/link.exe";
        }
        if (IsLinux)
        {
            return IsArm64
                ? "https://mczlf.loft.games/API/linuxArm64"
                : "https://mczlf.loft.games/API/linuxAmd64";
        }
        if (IsMacOS)
        {
            return IsArm64
                ? "https://mczlf.loft.games/API/drawinArm64"
                : "https://mczlf.loft.games/API/drawinAmd64";
        }
        return "https://mczlf.loft.games/API/link.exe"; // 默认回退
    }

    private static string GetProcessName() => "link";

    private static string GetCoreMd5()
    {
        // Windows AMD64: 559a28f9d51dcbec970d2dbc7f2fd8aa
        // Windows ARM64: 970d6429ea28519b9a928c7917f9901e
        // Linux ARM64: 984fad280d713a13fbce44d51d6bb92e
        // Linux AMD64: e340c94c42919753792dd18b8e3a94be
        // macOS ARM64: e63fbb6ce10fd5c3570318ae44386361
        // macOS AMD64: 0034c82f6ac39932c6d172698e9a81fd
        if (IsWindows) return IsArm64 ? "970d6429ea28519b9a928c7917f9901e" : "559a28f9d51dcbec970d2dbc7f2fd8aa";
        if (IsLinux) return IsArm64 ? "984fad280d713a13fbce44d51d6bb92e" : "e340c94c42919753792dd18b8e3a94be";
        if (IsMacOS) return IsArm64 ? "e63fbb6ce10fd5c3570318ae44386361" : "0034c82f6ac39932c6d172698e9a81fd";
        return "559a28f9d51dcbec970d2dbc7f2fd8aa";
    }

    // ========== 常量 ==========
    private const string WindowsLinkUrl = "http://mczlf.loft.games/API/link.exe";
    private const string WindowsFileMd5 = "559a28f9d51dcbec970d2dbc7f2fd8aa";

    public LinkPageViewModel()
    {
        _playerListService = PlayerListService.Instance;

        // 订阅玩家列表服务事件
        _playerListService.Kicked += OnPlayerKicked;
        _playerListService.LogMessage += OnPlayerListLogMessage;

        InitializeLog();
    }
    
    /// <summary>
    /// 处理玩家列表服务的日志消息
    /// </summary>
    private void OnPlayerListLogMessage(object? sender, string message)
    {
        log(message);
    }

    /// <summary>
    /// 被踢出房间处理 - 模拟按下floatbutton停止所有
    /// </summary>
    private void OnPlayerKicked(object? sender, string reason)
    {
        log($"被踢出房间: {reason}");
        log("正在停止Link核心...");

        // 停止Link核心
        StopLink();
    }

    private void InitializeLog()
    {
        log("MinecraftConnectTool Network Core | Tunneling via MCILM-Link | Adapted by MCZLF_Studio");
        string gonggao = $"感谢您使用Minecraft Connect Tool\n群聊 690625244       《欢迎加入ヾ(≧▽≦*)o\n仅供Minecraft联机及其他合法用途拓展使用,违法使用作者不负任何责任\n========================================================\nLinkMode功能目前可能尚不完善,若有Bug可以反馈";
        log(gonggao);
    }

    // ========== 日志系统 ==========
    public void log(string message)
    {
        if ("中转模式|P2P模式".Contains(message)) return;

        var replacements = new Dictionary<string, string>
        {
            { "连接服务器成功，分享联机码后联机！", "连接服务器成功，将提示码分享给好友即可快速开始联机" },
            { "分享码", "提示码" },
            { "联机码", "提示码" },
            { "openp2p start", "Powered by OpenP2P" }
        };

        foreach (var pair in replacements)
        {
            message = Regex.Replace(message, @"\b" + Regex.Escape(pair.Key) + @"\b", pair.Value);
        }

        string logFilePath = Path.Combine(Environment.GetEnvironmentVariable("TEMP") ?? Path.GetTempPath(), "MCZLFAPP", "Temp", "APPLog.ini");
        Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);
        string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}";
        File.AppendAllText(logFilePath, logMessage);

        // 处理消息并添加到日志
        ProcessLogMessage(message);
    }

    private void ProcessLogMessage(string message)
    {
        // 定义字符与方法的映射关系
        Dictionary<string, Action> methodMap = new()
        {
            { "提示码", async () => {
                IsAlertVisible = true;
                IsInfoButtonVisible = true;
                string code = ExtractPromptCode(message);
                InfoButtonText = code;
                AlertText = $"提示码: {code}";
                // 尝试复制到剪贴板
                try
                {
                    // 使用Avalonia的剪贴板API需要在UI线程上执行
                    // 这里先记录要复制的内容
                    _pendingClipboardText = $"邀请你加入我的Minecraft联机房间！\n提示码为 {code}\n复制时请勿带上前面的中文哦";
                }
                catch { }

                // 创建玩家管理房间
                if (!string.IsNullOrEmpty(code))
                {
                    var playerListSuccess = await _playerListService.CreateRoomAsync(code);
                    IsPlayerListConnected = playerListSuccess;
                    if (playerListSuccess)
                    {
                        log("玩家管理房间已创建");
                    }
                    else
                    {
                        log("玩家管理房间创建失败，但不影响联机功能");
                    }
                }
            }},
            { "未通过MC流量检测，程序终止", () => {
                StatusBadgeState = BadgeState.Error;
                StatusBadgeText = "请先启动游戏后再开启房间";
                ShowErrorToast("请先启动游戏再启动房间");
                stoplink();
            }},
            { "解析响应失败", () => {
                log("与服务器的连接受阻，可能是服务器被攻击造成离线，建议再试试呢");
                stoplink();
            }},
            { "NAT detect error", () => log("NAT类型探测失败 i/o timeout") },
            { "LISTEN ON PORT", () => {
                log("Success:成功在本地创建监听端口");
                StatusBadgeState = BadgeState.Success;
                StatusBadgeText = "已连接";
            }},
            { "sdwan init ok", () => {
                StatusBadgeState = BadgeState.Warning;
                StatusBadgeText = role == "1" ? "正在等待被连接..." : "正在尝试连接...";
            }},
            { "connection ok", () => {
                StatusBadgeState = role == "1" ? BadgeState.Success : BadgeState.Warning;
                StatusBadgeText = "已连接";
            }},
            { "handShakeC2C ok", () => {
                StatusBadgeState = role == "1" ? BadgeState.Success : BadgeState.Warning;
                StatusBadgeText = "已连接";
            }},
            { "i/o timeout", () => {
                log("i/o超时,可能是端口无法连接,也可能是运营商动手脚了,建议检查一下端口是否有误并继续等待");
            }},
            { "no such host", () => {
                // Program.alerterror("程序未能够连接到HOST,可能是防火墙拦截,或是根本没有授予网络访问权限");
            }},
            { "it will auto reconnect when peer node online", () => {
                // Program.alerterror("房间不在线,请检查是否有输入错误,或好友是否正确的启动了房间");
            }},
            { "房间不存在，联机失败", () => {
                StatusBadgeState = BadgeState.Error;
                StatusBadgeText = "对方不在线";
            }},
            { "Usage:", () => {
                stoplink();
                StatusBadgeState = BadgeState.Error;
                StatusBadgeText = "触发Usage_Bug";
            }},
        };

        foreach (var pair in methodMap)
        {
            if (message.Contains(pair.Key))
            {
                pair.Value?.Invoke();
            }
        }

        // 在输出到日志区前进行最终替换
        var finalReplacements = new Dictionary<string, string>
        {
            { "联机码", "提示码" },
            { "分享码", "提示码" }
        };

        string displayMessage = message;
        foreach (var pair in finalReplacements)
        {
            displayMessage = displayMessage.Replace(pair.Key, pair.Value);
        }

        // 追加到日志文本
        LogText += displayMessage + Environment.NewLine;
    }

    private string ExtractPromptCode(string fullText)
    {
        if (string.IsNullOrWhiteSpace(fullText))
            return string.Empty;

        int lastColonIndex = fullText.LastIndexOfAny(['：', ':']);
        if (lastColonIndex == -1)
            return string.Empty;

        return fullText[(lastColonIndex + 1)..].Trim();
    }

    // ========== MD5校验 ==========
    public static string? GetFileMD5Hash(string filePath)
    {
        try
        {
            using FileStream stream = File.OpenRead(filePath);
            MD5 md5 = MD5.Create();
            byte[] hashValue = md5.ComputeHash(stream);
            StringBuilder hex = new(hashValue.Length * 2);
            foreach (byte b in hashValue)
            {
                hex.AppendFormat("{0:x2}", b);
            }
            return hex.ToString();
        }
        catch (Exception)
        {
            return null;
        }
    }

    // ========== 核心检查和下载 ==========
    public async Task<bool> CheckAndDownloadCoreAsync()
    {
        log("检查LinkMode核心中..");

        string tempDirectory = Path.GetTempPath();
        string customDirectory = Path.Combine(tempDirectory, "MCZLFAPP", "Temp");
        string coreFileName = GetCoreFileName();
        string fileName = Path.Combine(customDirectory, coreFileName);
        string downloadUrl = GetCoreDownloadUrl();
        string expectedMd5 = GetCoreMd5();

        // 记录平台信息
        string platformInfo = $"平台: {(IsWindows ? "Windows" : IsLinux ? "Linux" : IsMacOS ? "macOS" : "Unknown")} " +
                              $"架构: {(IsArm64 ? "ARM64" : IsX64 ? "x64" : "Unknown")}";
        log(platformInfo);
        log($"核心文件: {coreFileName}");

        bool needsDownload = false;

        if (File.Exists(fileName))
        {
            string? md5Hash = GetFileMD5Hash(fileName);

            if (md5Hash == expectedMd5)
            {
                log("核心已存在且安全校验通过");
            }
            else
            {
                if (md5Hash == null)
                {
                    log("出现错误，进程终止");
                    role = "0";
                    IsStatusBadgeVisible = false;
                    StatusBadgeState = BadgeState.Default;
                    StatusBadgeText = "Null";
                    return false;
                }
                else
                {
                    log("核心不存在或安全校验不通过,重新Download中");
                    needsDownload = true;
                }
            }
        }
        else
        {
            log("核心不存在或安全校验不通过,重新Download中");
            needsDownload = true;
        }

        if (needsDownload)
        {
            IsProgressVisible = true;
            ProgressValue = 0;

            log("Downloader启动");
            Console.Write($"下载地址: {downloadUrl}");

            using var unityClient = new HttpClient();
            using var response = await unityClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            long totalBytes = response.Content.Headers.ContentLength ?? 0;
            long downloadedBytes = 0;

            using var httpStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.Write, bufferSize: 8192, useAsync: true);
            byte[] buffer = new byte[8192];
            int bytesRead;

            while ((bytesRead = await httpStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                downloadedBytes += bytesRead;

                if (totalBytes > 0)
                {
                    ProgressValue = (int)((downloadedBytes * 100) / totalBytes);
                }
            }

            log("Task:下载核心中..");

            // Linux/Mac 需要添加执行权限
            if (IsLinux || IsMacOS)
            {
                try
                {
                    Process chmodProcess = new Process();
                    chmodProcess.StartInfo.FileName = "chmod";
                    chmodProcess.StartInfo.Arguments = $"+x \"{fileName}\"";
                    chmodProcess.StartInfo.UseShellExecute = false;
                    chmodProcess.StartInfo.RedirectStandardOutput = true;
                    chmodProcess.StartInfo.RedirectStandardError = true;
                    chmodProcess.StartInfo.CreateNoWindow = true;
                    chmodProcess.Start();
                    await chmodProcess.WaitForExitAsync();
                    log("已添加执行权限");
                }
                catch (Exception ex)
                {
                    log($"添加执行权限失败: {ex.Message}");
                }
            }
        }

        IsProgressVisible = false;
        ProgressValue = 0;

        return true;
    }

    // ========== 开启联机房间 ==========
    [RelayCommand]
    public async Task StartRoomAsync()
    {
        role = "1";
        string processName = GetProcessName();

        if (Process.GetProcessesByName(processName).Length > 0)
        {
            AlreadyCore();
            return;
        }

        if (!await CheckAndDownloadCoreAsync())
            return;

        log("构造启动参数中..");
        // 通过对话框获取端口，默认25565
        string? port = null;
        if (RequestPortInput != null)
        {
            port = await RequestPortInput.Invoke("25565");
        }

        if (string.IsNullOrWhiteSpace(port))
        {
            log("用户取消输入端口，启动终止");
            role = "0";
            IsStatusBadgeVisible = false;
            StatusBadgeState = BadgeState.Default;
            StatusBadgeText = "Null";
            return;
        }

        log($"选定端口：{port}，准备启动核心…");

        string tempDirectory = Path.GetTempPath();
        string customDirectory = Path.Combine(tempDirectory, "MCZLFAPP", "Temp");
        string coreFileName = GetCoreFileName();
        string fileName = Path.Combine(customDirectory, coreFileName);
        string arguments = $"-s {port}";

        try
        {
            Process process = new Process();
            process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
            
            // Linux/Mac 使用 bash 来启动，避免参数解析问题
            if (IsLinux || IsMacOS)
            {
                process.StartInfo.FileName = "/bin/bash";
                process.StartInfo.Arguments = $"-c \"{fileName} {arguments}\"";
            }
            else
            {
                process.StartInfo.FileName = fileName;
                process.StartInfo.Arguments = arguments;
            }
            
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            // JSON 日志解析
            void OnOutputLine(string line)
            {
                if (string.IsNullOrWhiteSpace(line)) return;
                try
                {
                    var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(line);
                    if (obj != null && obj["msg"] != null)
                        log(obj["msg"]!.ToString());
                    else
                        log(line);
                }
                catch { log(line); }
            }

            process.OutputDataReceived += (s, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data)) OnOutputLine(args.Data);
            };
            process.ErrorDataReceived += (s, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data)) OnOutputLine(args.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            _linkProcess = process;

            log($"{coreFileName} 启动成功~");
            IsStatusBadgeVisible = true;
            StatusBadgeState = BadgeState.Success;
            StatusBadgeText = "Online";

            // 创建玩家管理房间（使用端口作为房间代码）
            var roomCode = port;
            if (!string.IsNullOrEmpty(roomCode))
            {
                var playerListSuccess = await _playerListService.CreateRoomAsync(roomCode);
                IsPlayerListConnected = playerListSuccess;
                if (playerListSuccess)
                {
                    log("玩家管理房间已创建");
                }
                else
                {
                    log("玩家管理房间创建失败，但不影响联机功能");
                }
            }

            // 更新全局状态
            P2PStateService.SetRunning(true, CoreMode.Link);
        }
        catch (Exception ex)
        {
            log($"启动失败：{ex.Message}");
            role = "0";
            IsStatusBadgeVisible = false;
            StatusBadgeState = BadgeState.Default;
            StatusBadgeText = "Null";
        }
    }

    // ========== 加入联机房间 ==========
    [RelayCommand]
    public async Task JoinRoomAsync()
    {
        string user = JoinCode;
        role = "2";
        string processName = GetProcessName();

        if (Process.GetProcessesByName(processName).Length > 0)
        {
            AlreadyCore();
            return;
        }

        // 获取昵称：优先从配置文件读取，如果没有则弹出对话框
        string? nickname = null;
        
        // 先尝试从配置文件读取Username
        string configuredUsername = ConfigService.Read<string>("Username", "");
        if (!string.IsNullOrWhiteSpace(configuredUsername))
        {
            nickname = configuredUsername;
            log($"使用已配置的用户名: {nickname}");
        }
        else
        {
            // 配置文件中没有，弹出对话框请求输入
            try
            {
                var mainWindow = Avalonia.Application.Current?.ApplicationLifetime
                    is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow : null;

                if (mainWindow != null)
                {
                    nickname = await ExtensionUI.NicknameInputDialog.ShowAsync(mainWindow);
                }
            }
            catch (Exception ex)
            {
                log($"显示昵称输入对话框失败: {ex.Message}");
            }

            if (string.IsNullOrWhiteSpace(nickname))
            {
                log("已取消加入房间（未输入昵称）");
                return;
            }

            // 保存输入的昵称到配置文件
            ConfigService.Write("Username", nickname);
            log("昵称已保存到设置");
        }

        if (!await CheckAndDownloadCoreAsync())
            return;

        log("构造启动参数中..");
        log($"昵称: {nickname}");

        // 显示infobar提示
        IsAlertVisible = true;
        AlertText = "请在局域网世界中查看";

        string tempDirectory = Path.GetTempPath();
        string customDirectory = Path.Combine(tempDirectory, "MCZLFAPP", "Temp");
        string coreFileName = GetCoreFileName();
        string fileName = Path.Combine(customDirectory, coreFileName);
        string arguments = $"-c {user}";

        try
        {
            Process process = new Process();
            process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
            
            // Linux/Mac 使用 bash 来启动，避免参数解析问题
            if (IsLinux || IsMacOS)
            {
                process.StartInfo.FileName = "/bin/bash";
                process.StartInfo.Arguments = $"-c \"{fileName} {arguments}\"";
            }
            else
            {
                process.StartInfo.FileName = fileName;
                process.StartInfo.Arguments = arguments;
            }
            
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            // JSON 日志解析
            void OnOutputLine(string line)
            {
                if (string.IsNullOrWhiteSpace(line)) return;
                try
                {
                    var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(line);
                    if (obj != null && obj["msg"] != null)
                        log(obj["msg"]!.ToString());
                    else
                        log(line);
                }
                catch { log(line); }
            }

            process.OutputDataReceived += (s, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data)) OnOutputLine(args.Data);
            };
            process.ErrorDataReceived += (s, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data)) OnOutputLine(args.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            _linkProcess = process;

            log($"{coreFileName} 启动成功~");
            IsStatusBadgeVisible = true;
            StatusBadgeState = BadgeState.Success;
            StatusBadgeText = "Online";

            // 加入玩家管理房间（Link模式使用提示码作为房间代码，端口设为0）
            var playerListSuccess = await _playerListService.JoinRoomAsync(user, nickname, 0);
            IsPlayerListConnected = playerListSuccess;
            if (playerListSuccess)
            {
                log("已加入玩家管理房间");
            }
            else
            {
                log("加入玩家管理房间失败，但不影响联机功能");
            }

            StartStatusPolling();

            // 更新全局状态
            P2PStateService.SetRunning(true, CoreMode.Link);
        }
        catch (Exception ex)
        {
            log($"启动失败：{ex.Message}");
            role = "0";
            IsStatusBadgeVisible = false;
            StatusBadgeState = BadgeState.Default;
            StatusBadgeText = "Null";
        }
    }

    // ========== 停止和清理 ==========
    public async void stoplink()
    {
        role = "0";

        ClearAllFloatButtons();

        IsStatusBadgeVisible = false;
        StatusBadgeState = BadgeState.Default;
        StatusBadgeText = "Null";

        IsInfoButtonVisible = false;
        IsAlertVisible = false;

        // 清理玩家管理房间
        if (_playerListService.IsHost)
        {
            await _playerListService.CloseRoomAsync();
            log("玩家管理房间已关闭");
        }
        else
        {
            await _playerListService.LeaveRoomAsync();
            log("已离开玩家管理房间");
        }

        // 重置玩家列表连接状态
        IsPlayerListConnected = false;

        Process[] processes = Process.GetProcessesByName("link");
        foreach (Process process in processes)
        {
            try
            {
                process.Kill();
                process.WaitForExit();
                log("进程已结束~");
            }
            catch (Exception ex)
            {
                log($"无法关闭进程: {ex.Message}");
            }
        }

        IsLabelVisible = false;
        LabelText = "";

        StopStatusPolling();

        // 更新全局状态
        P2PStateService.SetRunning(false);
    }

    // ========== 停止Link核心命令 ==========
    [RelayCommand]
    public void StopLink()
    {
        stoplink();
    }

    private void ClearAllFloatButtons()
    {
        foreach (var btn in _floatButtons)
        {
            try
            {
                btn.GetType().GetMethod("Close")?.Invoke(btn, null);
            }
            catch { }
        }
        _floatButtons.Clear();
    }

    public void AlreadyCore()
    {
        // 显示警告：已启动多个核心
    }

    // ========== 状态轮询 ==========
    private void StartStatusPolling()
    {
        _statusTimer?.Stop();
        _statusTimer?.Dispose();

        _statusTimer = new Timer(1000) { AutoReset = true };
        _statusTimer.Elapsed += (_, __) => SendStatusCommand();
        _statusTimer.Start();
    }

    private void SendStatusCommand()
    {
        if (_linkProcess == null || _linkProcess.HasExited) return;

        string id = Guid.NewGuid().ToString("N");
        string cmdJson = $"{{\"id\":\"{id}\",\"order\":\"status\"}}";

        statusCommandFeedback = string.Empty;

        DataReceivedEventHandler? outputFilter = null;
        DataReceivedEventHandler? errorFilter = null;

        outputFilter = (s, e) => HandlePossibleResponse(e.Data, id, ref outputFilter, ref errorFilter);
        errorFilter = (s, e) => HandlePossibleResponse(e.Data, id, ref outputFilter, ref errorFilter);

        _linkProcess.OutputDataReceived += outputFilter;
        _linkProcess.ErrorDataReceived += errorFilter;

        try
        {
            _linkProcess.StandardInput.WriteLine(cmdJson);
            IsLabelVisible = true;
        }
        catch { }
    }

    private void HandlePossibleResponse(string? line, string expectId, ref DataReceivedEventHandler? outputFilter, ref DataReceivedEventHandler? errorFilter)
    {
        if (string.IsNullOrWhiteSpace(line)) return;

        try
        {
            var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(line);
            if (obj == null || obj["id"] == null || (string?)obj["id"] != expectId) return;

            bool ok = obj["success"] != null && (bool)obj["success"]!;
            string msg = obj["msg"]?.ToString() ?? string.Empty;
            statusCommandFeedback = msg;
            
            // 更新工作模式badge
            WorkModeBadgeText = $"工作模式:{msg}";
            IsWorkModeBadgeVisible = true;
            IsLabelVisible = true;
            LabelText = $"工作模式:{msg}";

            if (_linkProcess != null && outputFilter != null && errorFilter != null)
            {
                _linkProcess.OutputDataReceived -= outputFilter;
                _linkProcess.ErrorDataReceived -= errorFilter;
            }
        }
        catch { }
    }

    public void StopStatusPolling()
    {
        if (_statusTimer != null)
        {
            _statusTimer.Stop();
            _statusTimer.Dispose();
            _statusTimer = null;
            IsLabelVisible = false;
            IsWorkModeBadgeVisible = false;
            WorkModeBadgeText = "";
        }
    }

    // ========== 发送命令 ==========
    [RelayCommand]
    public void SendCommand()
    {
        if (_linkProcess != null && !_linkProcess.HasExited)
        {
            _linkProcess.StandardInput.WriteLine(CommandText);
        }
    }

    // ========== 复制提示码 ==========
    [RelayCommand]
    public async Task CopyPromptCodeAsync()
    {
        try
        {
            // 复制到剪贴板 - 只复制提示码本身
            if (!string.IsNullOrEmpty(InfoButtonText))
            {
                // 使用TopLevel的剪贴板
                if (App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                {
                    if (desktop.MainWindow?.Clipboard != null)
                    {
                        await desktop.MainWindow.Clipboard.SetTextAsync(InfoButtonText);
                        // 使用特殊前缀避免触发关键词替换
                        AppendLogDirect("[系统] 提示码已复制到剪贴板");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            log($"复制失败：{ex.Message}");
        }
    }

    // 直接添加日志，不经过关键词替换处理
    private void AppendLogDirect(string message)
    {
        string logFilePath = Path.Combine(Environment.GetEnvironmentVariable("TEMP") ?? Path.GetTempPath(), "MCZLFAPP", "Temp", "APPLog.ini");
        Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);
        string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}";
        File.AppendAllText(logFilePath, logMessage);
        
        // 直接添加到UI，不经过ProcessLogMessage处理
        LogText += message + Environment.NewLine;
    }

    // ========== 显示错误提示窗 ==========
    public async void ShowErrorToast(string message)
    {
        // 取消之前的自动隐藏任务
        _toastCancellationTokenSource?.Cancel();
        _toastCancellationTokenSource = new System.Threading.CancellationTokenSource();
        var token = _toastCancellationTokenSource.Token;

        // 设置文本并显示
        ErrorToastText = message;
        IsErrorToastVisible = true;

        // 淡入动画 - 30ms间隔，从0到1，共300ms
        for (double i = 0; i <= 1; i += 0.1)
        {
            if (token.IsCancellationRequested) return;
            ErrorToastOpacity = i;
            await Task.Delay(30, token);
        }
        ErrorToastOpacity = 1;

        try
        {
            // 显示6.4秒后淡出（总共约7秒：0.3s淡入 + 6.4s显示 + 0.3s淡出）
            await Task.Delay(6400, token);

            // 淡出动画 - 30ms间隔，从1到0，共300ms
            for (double i = 1; i >= 0; i -= 0.1)
            {
                if (token.IsCancellationRequested) return;
                ErrorToastOpacity = i;
                await Task.Delay(30, token);
            }
            ErrorToastOpacity = 0;
            IsErrorToastVisible = false;
        }
        catch (TaskCanceledException)
        {
            // 任务被取消，淡出隐藏
            for (double i = ErrorToastOpacity; i >= 0; i -= 0.1)
            {
                ErrorToastOpacity = i;
                await Task.Delay(30);
            }
            ErrorToastOpacity = 0;
            IsErrorToastVisible = false;
        }
    }

    // ========== 调试选项 ==========
    [RelayCommand]
    public void ToggleCommandInput()
    {
        IsCommandInputVisible = !IsCommandInputVisible;
    }

    // ========== 复制日志 ==========
    [RelayCommand]
    private async Task CopyLog()
    {
        if (!string.IsNullOrEmpty(LogText))
        {
            bool success = await ClipboardHelper.SetTextAsync(LogText);
            if (success)
            {
                AppendLogDirect("[系统] 日志已复制到剪贴板");
            }
            else
            {
                AppendLogDirect("[系统] 复制日志到剪贴板失败");
            }
        }
    }

    // ========== 打开玩家管理面板 ==========
    [RelayCommand]
    private void OpenPlayerManager()
    {
        OpenPlayerManagerRequested?.Invoke(this, EventArgs.Empty);
    }
}

// 徽章状态枚举
public enum BadgeState
{
    Default,
    Success,
    Warning,
    Error,
    Info
}
