using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinecraftConnectTool.Services;
using MinecraftConnectTool.Views;

namespace MinecraftConnectTool.ViewModels.Pages;

public partial class ETPageViewModel : ViewModelBase, IDisposable
{
    #region 属性

    [ObservableProperty]
    private string _logText = "";

    private const int MaxLogLength = 300_000;
    private const int LogTrimTargetLength = 240_000;
    private const int MaxUiLogEntryLength = 20_000;
    private const int MaxRecentErrorMessages = 128;
    private const string LogTrimNotice = "...(前面日志已省略，完整日志请使用 AI 日志分析或 APPLog.ini)...\n";
    private readonly object _logTextLock = new();
    private readonly Queue<string> _recentErrorMessages = new();
    private readonly HashSet<string> _recentErrorMessageSet = new(StringComparer.OrdinalIgnoreCase);

    private void AppendUiLog(string message)
    {
        var newText = $"[{DateTime.Now:HH:mm:ss}] {LimitUiLogEntry(message)}\n";

        lock (_logTextLock)
        {
            var currentText = LogText;
            var availableLength = MaxLogLength - newText.Length - LogTrimNotice.Length;

            if (availableLength <= 0)
            {
                var keepEntryLength = Math.Min(LogTrimTargetLength, Math.Max(0, MaxLogLength - LogTrimNotice.Length));
                LogText = LogTrimNotice + newText[^keepEntryLength..];
                return;
            }

            if (currentText.Length > availableLength)
            {
                var keepLength = Math.Min(LogTrimTargetLength, availableLength);
                currentText = LogTrimNotice + currentText[^keepLength..];
            }

            LogText = currentText + newText;
        }
    }

    private static string LimitUiLogEntry(string message)
    {
        if (message.Length <= MaxUiLogEntryLength)
        {
            return message;
        }

        var headLength = MaxUiLogEntryLength / 2;
        var tailLength = MaxUiLogEntryLength - headLength;
        return string.Concat(
            message.AsSpan(0, headLength),
            "\n...(单条日志过长，中间内容已省略，完整日志请使用 AI 日志分析或 APPLog.ini)...\n",
            message.AsSpan(message.Length - tailLength, tailLength));
    }

    private void ResetErrorDeduplication()
    {
        _recentErrorMessages.Clear();
        _recentErrorMessageSet.Clear();
    }

    [ObservableProperty]
    private string _promptCode = "";

    [ObservableProperty]
    private string _joinPromptCode = "";

    [ObservableProperty]
    private string _alertText = "";

    [ObservableProperty]
    private bool _isAlertVisible;

    [ObservableProperty]
    private bool _isProgressVisible;

    [ObservableProperty]
    private double _progressValue;

    [ObservableProperty]
    private bool _isETRunning;

    [ObservableProperty]
    private bool _canStartET = true;

    [ObservableProperty]
    private bool _canStopET;

    // 状态徽章
    [ObservableProperty]
    private bool _isStatusBadgeVisible;

    [ObservableProperty]
    private string _statusBadgeText = "";

    [ObservableProperty]
    private BadgeState _statusBadgeState = BadgeState.Default;

    // 玩家列表
    [ObservableProperty]
    private ObservableCollection<ETPlayerItem> _playerList = new();

    // ET服务
    private readonly ETModeService _etService;
    private string _currentInfoText = "";

    // 房间列表连接状态
    [ObservableProperty]
    private bool _isETRunningForPanel;

    public enum BadgeState
    {
        Default,
        Success,
        Warning,
        Error,
        Info,
        Waiting
    }

    private bool ShouldBlockDuplicateError(string message)
    {
        if (!IsErrorLog(message))
            return false;

        var normalizedMessage = NormalizeErrorMessage(message);
        if (string.IsNullOrWhiteSpace(normalizedMessage))
            return false;

        if (_recentErrorMessageSet.Contains(normalizedMessage))
            return true;

        _recentErrorMessages.Enqueue(normalizedMessage);
        _recentErrorMessageSet.Add(normalizedMessage);

        while (_recentErrorMessages.Count > MaxRecentErrorMessages)
        {
            var oldestMessage = _recentErrorMessages.Dequeue();
            _recentErrorMessageSet.Remove(oldestMessage);
        }

        return false;
    }

    private static bool IsErrorLog(string message)
    {
        var upper = message.ToUpperInvariant();
        return upper.Contains("ERROR") || upper.Contains("FAIL") ||
               upper.Contains("错误") || upper.Contains("失败") ||
               IsRepeatedEtConnectError(message);
    }

    private static bool IsRepeatedEtConnectError(string message)
    {
        return message.Contains("[ET]", StringComparison.OrdinalIgnoreCase) &&
               message.Contains("connect to peer error", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeErrorMessage(string message)
    {
        var normalized = message.Trim();
        if (normalized.StartsWith("错误:", StringComparison.OrdinalIgnoreCase))
            normalized = normalized[3..].Trim();
        if (normalized.StartsWith("[ET]", StringComparison.OrdinalIgnoreCase))
            normalized = normalized[4..].Trim();

        var connectErrorIndex = normalized.IndexOf("connect to peer error", StringComparison.OrdinalIgnoreCase);
        if (connectErrorIndex >= 0)
            return NormalizeEtConnectError(normalized[connectErrorIndex..]);

        return normalized;
    }

    private static string NormalizeEtConnectError(string message)
    {
        var normalized = RemoveField(message.Trim(), " ip_version=");
        var errorIndex = normalized.IndexOf(" error=", StringComparison.OrdinalIgnoreCase);
        if (errorIndex >= 0)
        {
            var errorValueIndex = errorIndex + " error=".Length;
            var bracketIndex = normalized.IndexOf('(', errorValueIndex);
            if (bracketIndex >= 0)
                normalized = normalized[..bracketIndex].Trim();
        }
        return normalized;
    }

    private static string RemoveField(string message, string fieldName)
    {
        var fieldIndex = message.IndexOf(fieldName, StringComparison.OrdinalIgnoreCase);
        if (fieldIndex < 0)
            return message;

        var valueStartIndex = fieldIndex + fieldName.Length;
        var nextFieldIndex = message.IndexOf(' ', valueStartIndex);
        if (nextFieldIndex < 0)
            return message[..fieldIndex].TrimEnd();

        return (message[..fieldIndex] + message[nextFieldIndex..]).Trim();
    }

    #endregion

    #region 事件

    /// <summary>
    /// 请求端口输入事件，参数为默认端口，返回用户输入的端口（null表示取消）
    /// </summary>
    public event Func<string, Task<string?>>? RequestPortInput;

    /// <summary>
    /// 打开玩家管理面板请求事件
    /// </summary>
    public event EventHandler? OpenPlayerManagerRequested;

    #endregion

    public ETPageViewModel()
    {
        _etService = ETModeService.Instance;
        _etService.LogMessage += OnLogMessage;
        _etService.ProgressChanged += OnProgressChanged;
        _etService.CoreStarted += OnCoreStarted;
        _etService.CoreStopped += OnCoreStopped;
        _etService.PromptCodeGenerated += OnPromptCodeGenerated;
        _etService.ServerPortDetected += OnServerPortDetected;
        _etService.PlayerListUpdated += OnPlayerListUpdated;
        _etService.StatusChanged += OnStatusChanged;
        _etService.ErrorOccurred += OnErrorOccurred;
        _etService.NodesFetched += OnNodesFetched;

        P2PStateService.StateChanged += OnP2PStateChanged;

        ETLoad();
    }

    private void ETLoad()
    {
        AdminService.Initialize();
        var version = Views.MainWindow.version;
        AddLog($"感谢您使用Minecraft Connect Tool (ET模式)");
        AddLog($"当前版本: {version}");
        AddLog("ET模式基于 EasyTier + Scaffolding 协议实现");
        AddLog("========================================");
        AddLog("注意: ET模式需要管理员权限以创建虚拟网卡");
        if (AdminService.IsAdmin)
            AddLog("管理员模式: 已启用");
        else
            AddLog("管理员模式: 未启用 (部分功能可能受限)");
        AddLog("温馨提示: 如果不点击关闭按钮，核心会继续在后台运行~");
    }

    #region 事件处理

    private void OnP2PStateChanged(object? sender, bool isRunning)
    {
        if (!isRunning && IsETRunning)
        {
            IsETRunning = false;
            CanStartET = true;
            CanStopET = false;
            IsStatusBadgeVisible = false;
            AddLog("ET核心已停止");
        }
    }

    private void OnLogMessage(object? sender, string message)
    {
        AddServiceLog(message);
    }

    private void OnProgressChanged(object? sender, double progress)
    {
        if (progress <= 0)
        {
            IsProgressVisible = false;
            ProgressValue = 0;
        }
        else
        {
            IsProgressVisible = true;
            ProgressValue = progress;
        }
    }

    private void OnCoreStarted(object? sender, EventArgs e)
    {
        IsETRunning = true;
        IsETRunningForPanel = true;
        CanStartET = false;
        CanStopET = true;
        IsProgressVisible = false;
        ProgressValue = 0;
        IsStatusBadgeVisible = true;
        StatusBadgeState = BadgeState.Info;
        StatusBadgeText = "正在初始化...";
        P2PStateService.SetRunning(true, CoreMode.ET);
    }

    private void OnCoreStopped(object? sender, EventArgs e)
    {
        IsETRunning = false;
        IsETRunningForPanel = false;
        CanStartET = true;
        CanStopET = false;
        IsAlertVisible = false;
        IsStatusBadgeVisible = false;
        StatusBadgeState = BadgeState.Default;
        StatusBadgeText = "";
        PlayerList.Clear();
        P2PStateService.SetRunning(false);
    }

    private void OnPromptCodeGenerated(object? sender, string code)
    {
        PromptCode = code;
        AlertText = $"提示码 → {code}";
        _currentInfoText = code;
        IsAlertVisible = true;
    }

    private void OnServerPortDetected(object? sender, int port)
    {
        AlertText = $"加入地址 → 127.0.0.1:{port}";
        _currentInfoText = $"127.0.0.1:{port}";
        IsAlertVisible = true;
    }

    private void OnPlayerListUpdated(object? sender, IReadOnlyList<ETPlayerInfo> players)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            PlayerList.Clear();
            foreach (var p in players)
            {
                PlayerList.Add(new ETPlayerItem
                {
                    IsHost = p.IsHost,
                    DisplayName = p.IsHost ? "房主" : (p.Username ?? "未知玩家"),
                    VirtualIp = p.VirtualIp ?? "",
                    ConnectionType = p.ConnectionType.ToString(),
                    Ping = $"{p.Ping}ms",
                    Loss = $"{p.Loss}%",
                    NatType = p.NatType ?? "",
                    Icon = p.IsHost ? "Crown" : "Account"
                });
            }
        });
    }

    private void OnStatusChanged(object? sender, string status)
    {
        StatusBadgeText = status;
        IsStatusBadgeVisible = true;
        StatusBadgeState = status switch
        {
            "已就绪" => BadgeState.Success,
            "已连接" => BadgeState.Success,
            "等待玩家加入..." => BadgeState.Waiting,
            "正在查找房主..." => BadgeState.Info,
            _ => BadgeState.Info
        };
    }

    private void OnErrorOccurred(object? sender, string error)
    {
        IsStatusBadgeVisible = true;
        StatusBadgeState = BadgeState.Error;
        StatusBadgeText = "错误";
        AddLog($"错误: {error}");
    }

    private async void OnNodesFetched(object? sender, IReadOnlyList<string> nodes)
    {
        if (!Views.MainWindow.EnableETNodeList) return;
        try
        {
            if (Avalonia.Application.Current?.ApplicationLifetime
                is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow;
                if (mainWindow != null)
                {
                    var nodeText = string.Join("\n", nodes);
                    await ExtensionUI.MD3MessageDialog.ShowAsync(mainWindow, $"已获取 {nodes.Count} 个公共节点：\n\n{nodeText}", "公共节点列表", Material.Icons.MaterialIconKind.ServerNetwork);
                }
            }
        }
        catch { }
    }

    #endregion

    #region 命令

    /// <summary>
    /// 开启联机房间（房主，需手动输入MC端口）
    /// </summary>
    [RelayCommand]
    private async Task OpenRoom()
    {
        var playerName = await GetPlayerName();
        if (playerName == null) return;

        // 弹出端口输入对话框
        int port = 25565;
        if (RequestPortInput != null)
        {
            var portStr = await RequestPortInput.Invoke("25565");
            if (string.IsNullOrWhiteSpace(portStr))
            {
                AddLog("未输入端口，已取消");
                return;
            }
            if (!int.TryParse(portStr, out port) || port < 1 || port > 65535)
            {
                AddLog("端口不合法 (1-65535)");
                return;
            }
        }

        ResetErrorDeduplication();

        AddLog("=== 开始创建 ET 联机房间 ===");

        // 立即显示关闭按钮
        CanStartET = false;
        CanStopET = true;
        IsETRunning = true;
        IsETRunningForPanel = true;
        P2PStateService.SetRunning(true, CoreMode.ET);

        var success = await _etService.StartHostAsync(port, playerName);
        if (!success)
        {
            IsETRunning = false;
            IsETRunningForPanel = false;
            CanStartET = true;
            CanStopET = false;
            IsProgressVisible = false;
            ProgressValue = 0;
            P2PStateService.SetRunning(false);
            AddLog("创建房间失败");
        }
    }

    /// <summary>
    /// 加入联机房间（端口由SCF协议自动协商，无需手动输入）
    /// </summary>
    [RelayCommand]
    private async Task JoinRoom()
    {
        if (string.IsNullOrWhiteSpace(JoinPromptCode))
        {
            AddLog("请输入提示码");
            return;
        }

        var playerName = await GetPlayerName();
        if (playerName == null) return;

        // 立即显示关闭按钮和房间管理，方便用户随时取消
        CanStartET = false;
        CanStopET = true;
        IsETRunning = true;
        IsETRunningForPanel = true;
        P2PStateService.SetRunning(true, CoreMode.ET);

        AddLog("=== 开始加入 ET 联机房间 ===");
        AddLog($"提示码: {JoinPromptCode}");
        ResetErrorDeduplication();
        var success = await _etService.StartJoinAsync(JoinPromptCode, playerName);
        if (!success)
        {
            IsETRunning = false;
            IsETRunningForPanel = false;
            CanStartET = true;
            CanStopET = false;
            IsProgressVisible = false;
            ProgressValue = 0;
            P2PStateService.SetRunning(false);
            AddLog("加入房间失败");
        }
    }

    /// <summary>
    /// 停止ET核心
    /// </summary>
    [RelayCommand]
    public async Task StopET()
    {
        AddLog("正在停止ET核心...");
        var success = await _etService.StopETAsync();
        if (!success)
        {
            AddLog("停止ET核心失败");
        }
    }

    /// <summary>
    /// 打开玩家管理面板
    /// </summary>
    [RelayCommand]
    private void OpenPlayerManager()
    {
        OpenPlayerManagerRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 复制提示码/加入地址
    /// </summary>
    [RelayCommand]
    private async Task CopyInfo()
    {
        var textToCopy = !string.IsNullOrEmpty(_currentInfoText) ? _currentInfoText : _etService.LobbyInfo?.FullCode;
        if (!string.IsNullOrEmpty(textToCopy))
        {
            bool success = await ClipboardHelper.SetTextAsync(textToCopy);
            if (success)
            {
                AddLog("已复制到剪贴板");
            }
            else
            {
                AddLog("复制失败");
            }
        }
    }

    /// <summary>
    /// 复制日志
    /// </summary>
    [RelayCommand]
    private async Task CopyLog()
    {
        if (!string.IsNullOrEmpty(LogText))
        {
            bool success = await ClipboardHelper.SetTextAsync(LogText);
            if (success)
            {
                AddLog("日志已复制到剪贴板");
            }
            else
            {
                AddLog("复制日志失败");
            }
        }
    }

    #endregion

    #region 辅助方法

    private async Task<string?> GetPlayerName()
    {
        string? nickname = null;

        // 先尝试从配置文件读取Username
        var configuredUsername = ConfigService.Read<string>("Username", "");
        if (!string.IsNullOrWhiteSpace(configuredUsername))
        {
            nickname = configuredUsername;
            AddLog($"使用已配置的用户名: {nickname}");
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
                AddLog($"显示昵称输入对话框失败: {ex.Message}");
            }

            if (string.IsNullOrWhiteSpace(nickname))
            {
                AddLog("已取消操作（未输入昵称）");
                return null;
            }

            // 保存输入的昵称到配置文件
            ConfigService.Write("Username", nickname);
            AddLog("昵称已保存到设置");
        }

        return nickname;
    }

    /// <summary>
    /// 日志文本变化事件 - 用于自动滚动到底部
    /// </summary>
    public event EventHandler? LogTextChanged;

    private void AddLog(string message)
    {
        AddLogCore(message, true);
    }

    private void AddServiceLog(string message)
    {
        AddLogCore(message, false);
    }

    private void AddLogCore(string message, bool writeAppLog)
    {
        // 过滤日志：只显示重要信息（ERROR/WARN/SUCCESS 或不带 [ET] 前缀的业务日志）
        if (ShouldFilterLog(message))
            return;

        if (ShouldBlockDuplicateError(message))
            return;

        if (writeAppLog)
            TempRunLogService.AppendPageAndApp("ET模式", message);
        else
            TempRunLogService.Append("ET模式", message);

        AppendUiLog(message);
        LogTextChanged?.Invoke(this, EventArgs.Empty);
    }

    private bool ShouldFilterLog(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return true;

        var upper = message.ToUpperInvariant();
        
        if (IsRepeatedEtConnectError(message))
            return false;

        // 保留所有 ERROR/WARN/FAIL/SUCCESS 级别的日志
        if (upper.Contains("ERROR") || upper.Contains("WARN") || 
            upper.Contains("FAIL") || upper.Contains("SUCCESS") ||
            upper.Contains("错误") || upper.Contains("警告") || upper.Contains("失败"))
            return false;

        // 保留关键业务日志（不带 [ET] 前缀的）
        if (!message.TrimStart().StartsWith("[ET]"))
            return false;

        // 过滤掉 EasyTier 原始输出的 INFO/DEBUG 日志
        return true;
    }

    #endregion

    #region IDisposable

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _etService.LogMessage -= OnLogMessage;
        _etService.ProgressChanged -= OnProgressChanged;
        _etService.CoreStarted -= OnCoreStarted;
        _etService.CoreStopped -= OnCoreStopped;
        _etService.PromptCodeGenerated -= OnPromptCodeGenerated;
        _etService.ServerPortDetected -= OnServerPortDetected;
        _etService.PlayerListUpdated -= OnPlayerListUpdated;
        _etService.StatusChanged -= OnStatusChanged;
        _etService.ErrorOccurred -= OnErrorOccurred;
        _etService.NodesFetched -= OnNodesFetched;
        P2PStateService.StateChanged -= OnP2PStateChanged;

        _etService.Dispose();
        LogTextChanged = null;

        GC.SuppressFinalize(this);
    }

    #endregion
}

/// <summary>
/// 玩家列表项
/// </summary>
public partial class ETPlayerItem : ObservableObject
{
    [ObservableProperty] private bool _isHost;
    [ObservableProperty] private string _displayName = "";
    [ObservableProperty] private string _virtualIp = "";
    [ObservableProperty] private string _connectionType = "";
    [ObservableProperty] private string _ping = "";
    [ObservableProperty] private string _loss = "";
    [ObservableProperty] private string _natType = "";
    [ObservableProperty] private string _icon = "Account";
}
