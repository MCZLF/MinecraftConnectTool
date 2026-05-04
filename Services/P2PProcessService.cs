using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MinecraftConnectTool.Services;

/// <summary>
/// P2P核心进程状态
/// </summary>
public enum P2PProcessState
{
    Stopped,
    Starting,
    Running,
    Stopping
}

/// <summary>
/// P2P核心进程管理服务 - 全局单例
/// </summary>
public interface IP2PProcessService
{
    P2PProcessState State { get; }
    bool IsRunning { get; }
    bool CanStart { get; }
    bool CanStop { get; }
    
    event EventHandler<P2PProcessState>? StateChanged;
    event EventHandler? ProcessStarted;
    event EventHandler? ProcessStopped;
    
    void StartMonitoring();
    void StopMonitoring();
    
    Task<bool> StartP2PAsync(string? roomCode = null, string? targetPort = null);
    Task<bool> StopP2PAsync();
}

public partial class P2PProcessService : ObservableObject, IP2PProcessService
{
    private static IP2PProcessService? _instance;
    private static readonly object _lock = new();
    
    public static IP2PProcessService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new P2PProcessService();
                }
            }
            return _instance;
        }
    }

    private readonly string _processName = "main";
    private CancellationTokenSource? _monitoringCts;
    private P2PProcessState _lastState = P2PProcessState.Stopped;

    [ObservableProperty]
    private P2PProcessState _state = P2PProcessState.Stopped;

    public bool IsRunning => State == P2PProcessState.Running;
    public bool CanStart => State == P2PProcessState.Stopped;
    public bool CanStop => State == P2PProcessState.Running;

    public event EventHandler<P2PProcessState>? StateChanged;
    public event EventHandler? ProcessStarted;
    public event EventHandler? ProcessStopped;

    private P2PProcessService()
    {
    }

    partial void OnStateChanged(P2PProcessState value)
    {
        if (_lastState != value)
        {
            _lastState = value;
            StateChanged?.Invoke(this, value);
            
            if (value == P2PProcessState.Running)
                ProcessStarted?.Invoke(this, EventArgs.Empty);
            else if (value == P2PProcessState.Stopped)
                ProcessStopped?.Invoke(this, EventArgs.Empty);
        }
    }

    public void StartMonitoring()
    {
        _monitoringCts?.Cancel();
        _monitoringCts = new CancellationTokenSource();
        _ = MonitorProcessAsync(_monitoringCts.Token);
    }

    public void StopMonitoring()
    {
        _monitoringCts?.Cancel();
        _monitoringCts = null;
    }

    private async Task MonitorProcessAsync(CancellationToken cancellationToken)
    {
        // 初始状态检测
        CheckProcessStatus();
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(1000, cancellationToken);
                CheckProcessStatus();
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void CheckProcessStatus()
    {
        var isRunning = Process.GetProcessesByName(_processName).Length > 0;
        
        if (isRunning && State != P2PProcessState.Running && State != P2PProcessState.Starting)
        {
            State = P2PProcessState.Running;
        }
        else if (!isRunning && State != P2PProcessState.Stopped && State != P2PProcessState.Stopping)
        {
            State = P2PProcessState.Stopped;
        }
    }

    public async Task<bool> StartP2PAsync(string? roomCode = null, string? targetPort = null)
    {
        if (!CanStart) return false;
        
        try
        {
            State = P2PProcessState.Starting;
            
            // 创建P2PModeService实例
            var p2pService = new P2PModeService();
            
            bool success;
            if (!string.IsNullOrEmpty(roomCode) && !string.IsNullOrEmpty(targetPort))
            {
                // 加入模式
                success = await p2pService.StartJoinAsync(roomCode, targetPort);
            }
            else
            {
                // 创建模式
                success = await p2pService.StartHostAsync();
            }
            
            if (success)
            {
                State = P2PProcessState.Running;
            }
            else
            {
                State = P2PProcessState.Stopped;
            }
            
            return success;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"启动P2P失败: {ex.Message}");
            State = P2PProcessState.Stopped;
            return false;
        }
    }

    public async Task<bool> StopP2PAsync()
    {
        if (!CanStop) return false;
        
        try
        {
            State = P2PProcessState.Stopping;
            
            // 停止 POST 服务
            Server_Post.Stop_Post();
            
            // 清理配置
            ConfigService.Delete("Server");
            ConfigService.Write("EnableRelay", false);

            // 结束进程
            var processes = Process.GetProcessesByName(_processName);
            foreach (var process in processes)
            {
                try
                {
                    process.Kill();
                    await process.WaitForExitAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"结束进程失败: {ex.Message}");
                }
            }

            State = P2PProcessState.Stopped;
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"停止P2P失败: {ex.Message}");
            State = P2PProcessState.Stopped;
            return false;
        }
    }
}

// 临时占位，后续需要完整实现
public static class Server_Post
{
    public static void Stop_Post()
    {
        // TODO: 实现停止POST服务
    }
}
