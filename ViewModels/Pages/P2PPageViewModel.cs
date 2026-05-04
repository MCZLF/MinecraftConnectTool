using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinecraftConnectTool.Services;
using MinecraftConnectTool.Views;

namespace MinecraftConnectTool.ViewModels.Pages;

public partial class P2PPageViewModel : ViewModelBase, IDisposable
{
    #region 属性

    [ObservableProperty]
    private string _roomCode = "";

    [ObservableProperty]
    private string _targetPort = "";

    [ObservableProperty]
    private string _logText = "";

    [ObservableProperty]
    private string _alertText = "提示码";

    [ObservableProperty]
    private bool _isAlertVisible;
    
    // 当前显示的信息内容（用于复制）
    private string _currentInfoText = "";

    [ObservableProperty]
    private bool _isProgressVisible;

    [ObservableProperty]
    private double _progressValue;

    // P2P进程状态
    [ObservableProperty]
    private bool _isP2PRunning;

    [ObservableProperty]
    private bool _canStartP2P = true;

    [ObservableProperty]
    private bool _canStopP2P;

    // 状态徽章
    [ObservableProperty]
    private bool _isCustomPortEnabled;

    [ObservableProperty]
    private bool _isMulticastEnabled;

    [ObservableProperty]
    private bool _isCompatibleModeEnabled;

    [ObservableProperty]
    private bool _isP2POptimized;

    [ObservableProperty]
    private bool _isDataSavingEnabled;

    [ObservableProperty]
    private bool _useLegacyMode;

    [ObservableProperty]
    private bool _isCustomCodeEnabled;

    // 玩家列表服务连接状态
    [ObservableProperty]
    private bool _isPlayerListConnected;

    // 主状态指示灯 (badge3)
    [ObservableProperty]
    private bool _isStatusBadgeVisible;

    [ObservableProperty]
    private string _statusBadgeText = "Null";

    [ObservableProperty]
    private BadgeState _statusBadgeState = BadgeState.Default;

    // P2P模式服务
    private readonly P2PModeService _p2pService;
    
    // 玩家列表服务
    private readonly PlayerListService _playerListService;

    /// <summary>
    /// 指示灯状态枚举
    /// </summary>
    public enum BadgeState
    {
        Default,
        Success,
        Warn,
        Error,
        Processing
    }

    #endregion

    public P2PPageViewModel()
    {
        _p2pService = new P2PModeService();
        _playerListService = PlayerListService.Instance;
        
        // 订阅P2P服务事件
        _p2pService.LogMessage += OnLogMessage;
        _p2pService.ProgressChanged += OnProgressChanged;
        _p2pService.TsmGenerated += OnTsmGenerated;
        _p2pService.CoreStarted += OnCoreStarted;
        _p2pService.CoreStopped += OnCoreStopped;
        _p2pService.ConfigRestored += OnConfigRestored;
        _p2pService.NatTypeDetected += OnNatTypeDetected;
        _p2pService.StatusChanged += OnStatusChanged;
        _p2pService.ErrorOccurred += OnErrorOccurred;
        
        // 订阅全局P2P状态变化事件（处理从FloatButton触发的停止）
        P2PStateService.StateChanged += OnP2PStateChanged;

        // 订阅玩家列表服务事件
        _playerListService.Kicked += OnPlayerKicked;
        _playerListService.LogMessage += OnPlayerListLogMessage;

        // 执行P2P页面加载逻辑（对应原P2PMode_Load）
        P2PLoad();
    }
    
    /// <summary>
    /// 处理玩家列表服务的日志消息
    /// </summary>
    private void OnPlayerListLogMessage(object? sender, string message)
    {
        AddLog(message);
    }
    
    /// <summary>
    /// 全局P2P状态变化处理 - 用于处理从其他页面（如FloatButton）触发的状态变化
    /// </summary>
    private void OnP2PStateChanged(object? sender, bool isRunning)
    {
        if (!isRunning && IsP2PRunning)
        {
            // 核心被停止（可能从FloatButton触发），更新页面状态
            IsP2PRunning = false;
            CanStartP2P = true;
            CanStopP2P = false;
            IsAlertVisible = false;
            IsStatusBadgeVisible = false;
            StatusBadgeState = BadgeState.Default;
            StatusBadgeText = "Null";
            AddLog("P2P核心已停止");
        }
    }

    /// <summary>
    /// 被踢出房间处理 - 模拟按下floatbutton停止所有
    /// </summary>
    private async void OnPlayerKicked(object? sender, string reason)
    {
        AddLog($"被踢出房间: {reason}");
        AddLog("正在停止P2P核心...");

        // 模拟floatbutton点击 - 直接调用停止命令
        await StopP2PCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// P2P页面加载 - 对应原WinForm的P2PMode_Load方法
    /// </summary>
    private void P2PLoad()
    {
        // 获取云公告
        _ = _p2pService.CloudAlertAsync();
        
        // 加载版本号
        string version = Views.MainWindow.version;
        
        // 初始公告内容 - 严格按照原代码
        string gonggao = $"感谢您使用Minecraft Connect Tool\n群聊 690625244       《欢迎加入ヾ(≧▽≦*)o\n仅供Minecraft联机及其他合法用途拓展使用,违法使用作者不负任何责任\n========================================================\n当前版本Minecraft Connect Tool {version}";
        AddLog(gonggao);
        AddLog("温馨提示:如果不点击右下角关闭按钮，核心会继续在后台运行~");
        
        // 检查管理员模式
        AdminService.Initialize();
        _p2pService.admin = AdminService.IsAdmin;
        if (AdminService.IsAdmin)
        {
            AddLog("管理员模式");
        }
        else
        {
            AddLog("非管理员启动");
        }
        
        // 检查核心是否已在运行
        if (System.Diagnostics.Process.GetProcessesByName("main").Length > 0)
        {
            AddLog("Core ==> running");
        }
        
        // 检查Windows版本
        bool ifwin7 = IsWindows7OrLower();
        if (ifwin7)
        {
            AddLog("警告:当前系统版本为Windows7或更低版本,已停止支持，在当前系统版本下可能无法使用Tools某些功能，且可能出现意外崩溃，请勿报告为bug");
        }
        else
        {
            AddLog("systemok");
        }
        
        // 检查ShowP2PBug配置
        bool showbug1 = ConfigService.Read("ShowP2PBug", false);
        if (showbug1)
        {
            // materialSingleLineTextField4.Text = "显示所有被捕获的异常，如果遇到无法退出的异常请通过任务栏或任务管理器结束任务";
        }
        
        // 初始化配置状态
        LoadConfigStatus();
        
        // 读取上次联机配置
        _p2pService.ReadPeerConfig();
        
        // 检查各种配置开关
        bool usecustomport = ConfigService.Read("usecustomport", false);
        if (usecustomport)
        {
            IsCustomPortEnabled = true;
            string customport = ConfigService.Read("customport", "None");
            if (!int.TryParse(customport, out int port) || port < 1 || port > 65535)
            {
                AddLog("错误的自定义端口");
            }
        }

        // 检查自定义提示码设置
        int codeupdate = ConfigService.Read("codeupdate", 1);
        if (codeupdate == 4)
        {
            bool usecustomnode = ConfigService.Read("usecustomnode", false);
            IsCustomCodeEnabled = usecustomnode;
        }

        bool EnableServerPost = ConfigService.Read("ServerPostEnable", true);
        if (EnableServerPost)
        {
            IsMulticastEnabled = true;
        }
        
        bool EnableOLAN = ConfigService.Read("EnableOLAN", false);
        if (EnableOLAN) { IsCompatibleModeEnabled = true; }
        
        // 初始化主状态指示灯badge3
        IsStatusBadgeVisible = false;
        StatusBadgeState = BadgeState.Default;
        StatusBadgeText = "Null";
        
        bool EnableTL = ConfigService.Read("EnableTL", false);
        if (EnableTL) { TargetPort = "7777"; }
        
        bool EnableDST = ConfigService.Read("EnableDST", false);
        if (EnableDST) { /* DSTFill(); */ }
        
        bool EnableRelay = ConfigService.Read("EnableRelay", false);
        if (EnableRelay) { /* badge5.Visible = true; */ }
        
        bool TCP = ConfigService.Read("TCP", true);
        if (!TCP) { /* TopText.Text = "您正在使用P2P模式进行联机ヾ(≧▽≦*)o(UDP)"; */ }
        
        bool Testchannel = ConfigService.Read("Testchannel", false);
        if (Testchannel) 
        { 
            // TopText.Text = "您正在使用P2P模式进行联机ヾ(≧▽≦*)o(测试)";
            _p2pService.tokenNormal = "7196174974940052261"; // Token重复值
        }
    }

    /// <summary>
    /// 检查是否为Windows7或更低版本
    /// </summary>
    private static bool IsWindows7OrLower()
    {
        OperatingSystem os = Environment.OSVersion;
        Version osVersion = os.Version;
        return os.Platform == PlatformID.Win32NT && osVersion.Major < 6 || (osVersion.Major == 6 && osVersion.Minor <= 1);
    }

    /// <summary>
    /// 加载配置状态到徽章显示
    /// </summary>
    private void LoadConfigStatus()
    {
        IsCustomPortEnabled = ConfigService.Read("usecustomport", false);
        IsMulticastEnabled = ConfigService.Read("ServerPostEnable", true);
        IsCompatibleModeEnabled = ConfigService.Read("EnableOLAN", false);
        IsP2POptimized = ConfigService.Read("P2POptimize", false);
        IsDataSavingEnabled = ConfigService.Read("datasaving", false);
        UseLegacyMode = ConfigService.Read("useoldway", false);

        // 检查自定义提示码设置
        int codeupdate = ConfigService.Read("codeupdate", 1);
        if (codeupdate == 4)
        {
            IsCustomCodeEnabled = ConfigService.Read("usecustomnode", false);
        }
        else
        {
            IsCustomCodeEnabled = false;
        }
    }

    #region 事件处理

    private void OnLogMessage(object? sender, string message)
    {
        AddLog(message);
    }

    private void OnProgressChanged(object? sender, double progress)
    {
        if (progress == 0)
        {
            // 进度为0，隐藏进度条
            IsProgressVisible = false;
            ProgressValue = 0;
        }
        else if (progress > 0 && progress <= 100)
        {
            // 正常进度更新，显示进度条
            IsProgressVisible = true;
            ProgressValue = progress;
        }
    }

    private void OnTsmGenerated(object? sender, string tsm)
    {
        // 区分房主和加入方：如果是IP地址格式（127.0.0.1:xxx），则是加入方
        if (tsm.StartsWith("127.0.0.1:"))
        {
            AlertText = $"加入IP→ {tsm}";
        }
        else
        {
            AlertText = $"提示码→ {tsm}";
        }
        _currentInfoText = tsm;
        IsAlertVisible = true;
    }

    private void OnCoreStarted(object? sender, EventArgs e)
    {
        IsP2PRunning = true;
        CanStartP2P = false;
        CanStopP2P = true;
        // 显示状态指示灯 - 严格按照原代码: badge3.Visible = true; badge3.State = TState.Processing; badge3.Text = "正在处理中...";
        IsStatusBadgeVisible = true;
        StatusBadgeState = BadgeState.Processing;
        StatusBadgeText = "正在处理中...";
        // 通知全局状态服务
        P2PStateService.SetRunning(true);
        AddLog("P2P核心已启动");
    }

    private void OnCoreStopped(object? sender, EventArgs e)
    {
        IsP2PRunning = false;
        CanStartP2P = true;
        CanStopP2P = false;
        IsAlertVisible = false;
        // 重置状态指示灯 - 严格按照原代码: badge3.Visible = false; badge3.State = TState.Default; badge3.Text = "Null";
        IsStatusBadgeVisible = false;
        StatusBadgeState = BadgeState.Default;
        StatusBadgeText = "Null";
        // 通知全局状态服务
        P2PStateService.SetRunning(false);
        AddLog("P2P核心已停止");
    }

    private void OnConfigRestored(object? sender, (string User, string Port) e)
    {
        RoomCode = e.User;
        TargetPort = e.Port;
        AddLog($"已恢复上次联机配置: 提示码={e.User}, 端口={e.Port}");
    }

    private void OnNatTypeDetected(object? sender, string e)
    {
        AddLog($"NAT类型检测: {e}");
    }

    private void OnStatusChanged(object? sender, (string BadgeState, string BadgeText) e)
    {
        AddLog($"状态更新: {e.BadgeState} - {e.BadgeText}");
        
        // 更新状态指示灯 - 严格按照原代码的badge3逻辑
        IsStatusBadgeVisible = true;
        StatusBadgeText = e.BadgeText;
        
        // 根据状态设置指示灯颜色
        StatusBadgeState = e.BadgeState switch
        {
            "Success" => BadgeState.Success,
            "Warn" => BadgeState.Warn,
            "Error" => BadgeState.Error,
            _ => BadgeState.Processing
        };
    }

    private void OnErrorOccurred(object? sender, string e)
    {
        AddLog($"错误: {e}");
        // 错误状态下指示灯变红
        IsStatusBadgeVisible = true;
        StatusBadgeState = BadgeState.Error;
        // 对方不在线时显示特定文本，其他错误统一显示简短提示
        StatusBadgeText = e == "对方不在线,请检查是否有输入错误,或好友是否正确的启动了房间" 
            ? "对方不在线" 
            : "发生错误，请检查日志";
    }

    #endregion

    #region 命令

    /// <summary>
    /// 开启联机房间（房主模式）
    /// </summary>
    [RelayCommand]
    private async Task OpenRoom()
    {
        AddLog("=== 开始开启联机房间 ===");
        
        // 同步配置到服务
        _p2pService.useoldway = UseLegacyMode;
        
        var success = await _p2pService.StartHostAsync();
        
        if (success)
        {
            // 创建玩家管理房间
            var roomCode = _p2pService.tsm;
            if (!string.IsNullOrEmpty(roomCode))
            {
                var playerListSuccess = await _playerListService.CreateRoomAsync(roomCode);
                IsPlayerListConnected = playerListSuccess;
                if (playerListSuccess)
                {
                    AddLog("玩家管理房间已创建");
                }
                else
                {
                    AddLog("玩家管理房间创建失败，但不影响联机功能");
                }
            }
        }
        else
        {
            AddLog("开启房间失败");
        }
    }

    /// <summary>
    /// 加入联机房间
    /// </summary>
    [RelayCommand]
    private async Task JoinRoom()
    {
        // 验证输入
        if (string.IsNullOrWhiteSpace(RoomCode))
        {
            AddLog("错误：请输入提示码");
            return;
        }

        if (string.IsNullOrWhiteSpace(TargetPort))
        {
            AddLog("错误：请输入目标端口");
            return;
        }

        // 验证端口合法性
        if (!int.TryParse(TargetPort, out int port) || port < 1 || port > 65535)
        {
            AddLog("错误：端口不合法 [1-65535]");
            return;
        }

        // 获取昵称：优先从配置文件读取，如果没有则弹出对话框
        string? nickname = null;
        
        // 先尝试从配置文件读取Username
        string configuredUsername = ConfigService.Read<string>("Username", "");
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
                AddLog("已取消加入房间（未输入昵称）");
                return;
            }

            // 保存输入的昵称到配置文件
            ConfigService.Write("Username", nickname);
            AddLog("昵称已保存到设置");
        }

        AddLog("=== 开始加入联机房间 ===");
        AddLog($"提示码: {RoomCode}");
        AddLog($"目标端口: {TargetPort}");
        AddLog($"昵称: {nickname}");
        
        // 同步配置到服务
        _p2pService.useoldway = UseLegacyMode;
        
        var success = await _p2pService.StartJoinAsync(RoomCode, TargetPort);
        
        if (success)
        {
            // 加入玩家管理房间
            var playerListSuccess = await _playerListService.JoinRoomAsync(RoomCode, nickname, port);
            IsPlayerListConnected = playerListSuccess;
            if (playerListSuccess)
            {
                AddLog("已加入玩家管理房间");
            }
            else
            {
                AddLog("加入玩家管理房间失败，但不影响联机功能");
            }
        }
        else
        {
            AddLog("加入房间失败");
        }
    }

    /// <summary>
    /// 停止P2P核心
    /// </summary>
    [RelayCommand]
    public async Task StopP2P()
    {
        AddLog("正在停止P2P核心...");
        
        // 清理玩家管理房间
        if (_playerListService.IsHost)
        {
            await _playerListService.CloseRoomAsync();
            AddLog("玩家管理房间已关闭");
        }
        else
        {
            await _playerListService.LeaveRoomAsync();
            AddLog("已离开玩家管理房间");
        }
        
        // 重置玩家列表连接状态
        IsPlayerListConnected = false;
        
        var success = await _p2pService.stopp2p();
        
        if (!success)
        {
            AddLog("停止P2P核心失败");
        }
    }

    /// <summary>
    /// 调试模式
    /// </summary>
    [RelayCommand]
    private void Debug()
    {
        AddLog("调试模式已启动");
        
        try
        {
            // 收集调试信息
            string node = Environment.MachineName;
            string netVersion = Environment.Version.ToString();
            string systemVersion = Environment.OSVersion.VersionString + " " + (Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit");
            string appVersion = Views.MainWindow.version;
            
            string debugInfo = $"协助调试信息\n当前版本{appVersion}\nNode:{node}\nEnvironment版本{netVersion}\nSystem {systemVersion}";
            
            AddLog("调试信息已生成");
            AddLog(debugInfo.Replace("\n", " | "));
            
            // 显示调试信息对话框（通过事件通知View层显示）
            DebugInfoRequested?.Invoke(this, debugInfo);
        }
        catch (Exception ex)
        {
            AddLog($"生成调试信息失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 调试信息请求事件 - 用于通知View层显示调试对话框
    /// </summary>
    public event EventHandler<string>? DebugInfoRequested;
    
    /// <summary>
    /// 打开玩家管理面板请求事件
    /// </summary>
    public event EventHandler? OpenPlayerManagerRequested;

    /// <summary>
    /// 复制信息到剪贴板
    /// </summary>
    [RelayCommand]
    private async Task CopyInfo()
    {
        string textToCopy = !string.IsNullOrEmpty(_currentInfoText) ? _currentInfoText : _p2pService.tsm;
        if (!string.IsNullOrEmpty(textToCopy))
        {
            bool success = await ClipboardHelper.SetTextAsync(textToCopy);
            if (success)
            {
                AddLog($"{textToCopy} 已复制到剪贴板");
            }
            else
            {
                AddLog("复制到剪贴板失败");
            }
        }
    }

    /// <summary>
    /// 复制日志到剪贴板
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
                AddLog("复制日志到剪贴板失败");
            }
        }
    }

    /// <summary>
    /// 打开玩家管理面板
    /// </summary>
    [RelayCommand]
    private void OpenPlayerManager()
    {
        if (!IsP2PRunning)
        {
            AddLog("请先开启或加入房间");
            return;
        }
        
        OpenPlayerManagerRequested?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 日志文本变化事件 - 用于自动滚动到底部
    /// </summary>
    public event EventHandler? LogTextChanged;

    private void AddLog(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        LogText += $"[{timestamp}] {message}\n";
        // 触发日志变化事件
        LogTextChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region IDisposable

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // 取消所有事件订阅
        _p2pService.LogMessage -= OnLogMessage;
        _p2pService.ProgressChanged -= OnProgressChanged;
        _p2pService.TsmGenerated -= OnTsmGenerated;
        _p2pService.CoreStarted -= OnCoreStarted;
        _p2pService.CoreStopped -= OnCoreStopped;
        _p2pService.ConfigRestored -= OnConfigRestored;
        _p2pService.NatTypeDetected -= OnNatTypeDetected;
        _p2pService.StatusChanged -= OnStatusChanged;
        _p2pService.ErrorOccurred -= OnErrorOccurred;
        P2PStateService.StateChanged -= OnP2PStateChanged;
        
        // 取消玩家列表服务事件订阅
        _playerListService.LogMessage -= OnPlayerListLogMessage;

        // 释放P2P服务资源
        _p2pService.Dispose();

        DebugInfoRequested = null;
        LogTextChanged = null;

        GC.SuppressFinalize(this);
    }

    #endregion
}
