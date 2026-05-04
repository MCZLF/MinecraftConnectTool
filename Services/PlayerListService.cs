using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MinecraftConnectTool.Services;

/// <summary>
/// 玩家信息
/// </summary>
public class PlayerInfo
{
    public string Id { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; }
    public DateTime JoinedAt { get; set; }
    public int Status { get; set; }
    public string Version { get; set; } = string.Empty;
    // 可选字段，服务端有但客户端不需要显示
    public DateTime LastHeartbeat { get; set; }
    public string? ProcessId { get; set; }
}

/// <summary>
/// 创建房间响应
/// </summary>
public class CreateRoomResponse
{
    public string RoomCode { get; set; } = string.Empty;
    public string HostPlayerId { get; set; } = string.Empty;
}

/// <summary>
/// API响应模型
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
}

/// <summary>
/// 玩家列表服务 - 与MCTListServer通信
/// </summary>
public class PlayerListService
{
    private static PlayerListService? _instance;
    private static readonly object _lock = new();
    
    public static PlayerListService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new PlayerListService();
                }
            }
            return _instance;
        }
    }

    private readonly HttpClient _httpClient;
    private readonly string _baseUrl = "http://193.112.56.140:30148";
    private System.Timers.Timer? _heartbeatTimer;
    
    // 容错机制相关字段
    private int _heartbeatFailureCount = 0;
    private const int MaxHeartbeatFailures = 20;
    
    // 防止心跳并发执行
    private readonly SemaphoreSlim _heartbeatLock = new(1, 1);
    
    /// <summary>
    /// 当前房间代码
    /// </summary>
    public string CurrentRoomCode { get; private set; } = string.Empty;
    
    /// <summary>
    /// 当前玩家ID
    /// </summary>
    public string CurrentPlayerId { get; private set; } = string.Empty;
    
    /// <summary>
    /// 是否是房主
    /// </summary>
    public bool IsHost { get; private set; }
    
    /// <summary>
    /// 当前玩家昵称
    /// </summary>
    public string CurrentNickname { get; private set; } = string.Empty;
    
    /// <summary>
    /// 是否已连接到玩家管理服务
    /// </summary>
    public bool IsConnected { get; private set; }

    /// <summary>
    /// 是否被踢出房间
    /// </summary>
    public bool IsKicked { get; private set; }
    
    /// <summary>
    /// 获取格式化的版本号字符串 (MCT_版本号_平台信息)
    /// </summary>
    public static string GetFormattedVersion()
    {
        var version = Views.MainWindow.version;
        var platform = OperatingSystem.IsWindows() ? "Windows" :
                      OperatingSystem.IsLinux() ? "Linux" :
                      OperatingSystem.IsMacOS() ? "macOS" : "Unknown";
        var arch = Environment.Is64BitOperatingSystem ? "x64" : "x86";
        return $"MCT_{version}_{platform}_{arch}";
    }

    /// <summary>
    /// 玩家列表变化事件
    /// </summary>
    public event EventHandler? PlayersChanged;
    
    /// <summary>
    /// 加入房间事件
    /// </summary>
    public event EventHandler<bool>? JoinedRoom;
    
    /// <summary>
    /// 被踢出事件
    /// </summary>
    public event EventHandler<string>? Kicked;
    
    /// <summary>
    /// 日志事件 - 用于输出到UI日志区
    /// </summary>
    public event EventHandler<string>? LogMessage;

    private PlayerListService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
    }
    
    /// <summary>
    /// 输出日志到UI
    /// </summary>
    private void Log(string message)
    {
        System.Diagnostics.Debug.WriteLine(message);
        LogMessage?.Invoke(this, message);
    }

    /// <summary>
    /// 房主创建房间
    /// </summary>
    public async Task<bool> CreateRoomAsync(string roomCode)
    {
        try
        {
            var request = new { RoomCode = roomCode, Version = GetFormattedVersion() };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/room/create", content);
            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<CreateRoomResponse>>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result?.Success == true && result.Data != null)
            {
                CurrentRoomCode = roomCode;
                IsHost = true;
                IsConnected = true;
                CurrentPlayerId = result.Data.HostPlayerId; // 房主也有玩家ID
                CurrentNickname = "房主";
                StartHeartbeat();
                JoinedRoom?.Invoke(this, true);
                return true;
            }

            IsConnected = false;
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"创建房间失败: {ex.Message}");
            IsConnected = false;
            return false;
        }
    }

    /// <summary>
    /// 房客加入房间
    /// </summary>
    public async Task<bool> JoinRoomAsync(string roomCode, string nickname, int port)
    {
        try
        {
            // 使用格式化版本号
            var version = GetFormattedVersion();
            var request = new { Nickname = nickname, RoomCode = roomCode, Port = port, Version = version };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/room/{roomCode}/player/join", content);
            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<PlayerInfo>>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result?.Success == true && result.Data != null)
            {
                CurrentRoomCode = roomCode;
                IsHost = false;
                IsConnected = true;
                CurrentPlayerId = result.Data.Id;
                CurrentNickname = nickname;
                StartHeartbeat();
                JoinedRoom?.Invoke(this, false);
                return true;
            }

            IsConnected = false;
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加入房间失败: {ex.Message}");
            IsConnected = false;
            return false;
        }
    }

    /// <summary>
    /// 离开房间
    /// </summary>
    public async Task LeaveRoomAsync()
    {
        try
        {
            // 如果被踢出，不再发送离开请求，直接清理状态
            if (IsKicked)
            {
                System.Diagnostics.Debug.WriteLine("已被踢出，跳过发送离开请求");
                return;
            }

            if (!string.IsNullOrEmpty(CurrentRoomCode) && !string.IsNullOrEmpty(CurrentPlayerId))
            {
                await _httpClient.PostAsync($"{_baseUrl}/api/room/{CurrentRoomCode}/player/{CurrentPlayerId}/leave", null);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"离开房间失败: {ex.Message}");
        }
        finally
        {
            StopHeartbeat();
            CurrentRoomCode = string.Empty;
            CurrentPlayerId = string.Empty;
            IsHost = false;
            IsConnected = false;
            IsKicked = false;
        }
    }

    /// <summary>
    /// 关闭房间（房主）
    /// </summary>
    public async Task CloseRoomAsync()
    {
        try
        {
            // 如果被踢出，不再发送关闭请求
            if (IsKicked)
            {
                System.Diagnostics.Debug.WriteLine("已被踢出，跳过发送关闭请求");
                return;
            }

            if (!string.IsNullOrEmpty(CurrentRoomCode) && IsHost)
            {
                await _httpClient.PostAsync($"{_baseUrl}/api/room/{CurrentRoomCode}/close", null);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"关闭房间失败: {ex.Message}");
        }
        finally
        {
            StopHeartbeat();
            CurrentRoomCode = string.Empty;
            CurrentPlayerId = string.Empty;
            IsHost = false;
            IsConnected = false;
            IsKicked = false;
        }
    }

    /// <summary>
    /// 获取玩家列表（异步）
    /// </summary>
    public async Task<List<PlayerInfo>> GetPlayersAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(CurrentRoomCode)) return new List<PlayerInfo>();
            
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/room/{CurrentRoomCode}/player/list");
            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<List<PlayerInfo>>>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            return result?.Data ?? new List<PlayerInfo>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取玩家列表失败: {ex.Message}");
            return new List<PlayerInfo>();
        }
    }
    


    /// <summary>
    /// 异步刷新玩家列表
    /// </summary>
    public async Task RefreshPlayersAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(CurrentRoomCode)) return;
            
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/room/{CurrentRoomCode}/player/list");
            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<List<PlayerInfo>>>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (result?.Success == true)
            {
                PlayersChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"刷新玩家列表失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 踢出玩家（仅房主）
    /// </summary>
    public async Task<bool> KickPlayerAsync(string playerId, string reason)
    {
        try
        {
            if (!IsHost || string.IsNullOrEmpty(CurrentRoomCode)) return false;
            
            var request = new { PlayerId = playerId, Reason = reason };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/room/{CurrentRoomCode}/player/kick", content);
            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<object>>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            return result?.Success == true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"踢出玩家失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 发送心跳
    /// </summary>
    private async Task SendHeartbeatAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(CurrentRoomCode) || string.IsNullOrEmpty(CurrentPlayerId)) return;

            var request = new { PlayerId = CurrentPlayerId };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/room/{CurrentRoomCode}/player/heartbeat", content);

            if (response.IsSuccessStatusCode)
            {
                // 心跳成功，重置容错计数
                if (_heartbeatFailureCount > 0)
                {
                    Log($"[容错] 心跳恢复，重置容错计数，Room={CurrentRoomCode}");
                    _heartbeatFailureCount = 0;
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<object>>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // 检查是否被踢出（白名单模式：仅房客会被踢出）
                if (result?.Message == "KICKED" && !IsHost)
                {
                    Log($"[容错] 收到踢出指令，房客被踢出，Room={CurrentRoomCode}");
                    IsKicked = true;
                    Kicked?.Invoke(this, "已被房主踢出房间");
                    StopHeartbeat();
                    // 清理连接状态
                    IsConnected = false;
                    CurrentRoomCode = string.Empty;
                    CurrentPlayerId = string.Empty;
                }
                else if (result?.Message == "KICKED" && IsHost)
                {
                    // 房主收到KICKED消息（异常情况），房主不会被踢出
                    Log($"[容错] 房主收到KICKED消息（已忽略），Room={CurrentRoomCode}");
                }
            }
            else
            {
                // 心跳失败（非200状态码）
                HandleHeartbeatFailure($"StatusCode={(int)response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            // 心跳异常
            HandleHeartbeatFailure($"Exception={ex.GetType().Name}");
        }
    }

    /// <summary>
    /// 处理心跳失败（容错机制核心逻辑）
    /// </summary>
    private void HandleHeartbeatFailure(string errorInfo)
    {
        // 房主特殊处理：只记录日志，不增加计数，不踢出
        if (IsHost)
        {
            Log($"[容错] 房主心跳异常（已忽略）: {errorInfo}, Room={CurrentRoomCode}");
            return;
        }

        // 房客容错计数机制
        _heartbeatFailureCount++;
        int remaining = MaxHeartbeatFailures - _heartbeatFailureCount;

        Log($"[容错] 房客心跳异常 ({_heartbeatFailureCount}/{MaxHeartbeatFailures}): {errorInfo}, Room={CurrentRoomCode}");

        if (_heartbeatFailureCount >= MaxHeartbeatFailures)
        {
            // 容错次数耗尽，触发踢出
            Log($"[容错] 房客容错次数耗尽，触发踢出，Room={CurrentRoomCode}");
            Kicked?.Invoke(this, "连接已断开（容错次数耗尽）");
            StopHeartbeat();
            IsConnected = false;
            CurrentRoomCode = string.Empty;
            CurrentPlayerId = string.Empty;
        }
        else
        {
            // 继续容错
            Log($"[容错] 房客继续容错，剩余 {remaining} 次，Room={CurrentRoomCode}");
        }
    }

    /// <summary>
    /// 启动心跳定时器
    /// </summary>
    private void StartHeartbeat()
    {
        StopHeartbeat();
        // 重置容错计数
        _heartbeatFailureCount = 0;
        Log($"[容错] 启动心跳定时器，容错计数已重置，Room={CurrentRoomCode}, IsHost={IsHost}");
        
        _heartbeatTimer = new System.Timers.Timer(5000); // 每5秒发送一次心跳
        _heartbeatTimer.Elapsed += OnHeartbeatTimerElapsed;
        _heartbeatTimer.AutoReset = true;
        _heartbeatTimer.Start();
    }
    
    /// <summary>
    /// 心跳定时器事件处理
    /// </summary>
    private async void OnHeartbeatTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        // 防止并发执行，如果上一个心跳还没完成，跳过这次
        if (!await _heartbeatLock.WaitAsync(0))
        {
            Log($"[容错] 跳过心跳，上一次仍在执行");
            return;
        }
        
        try
        {
            await SendHeartbeatAsync();
        }
        catch (Exception ex)
        {
            Log($"[容错] 心跳发送失败: {ex.Message}");
        }
        finally
        {
            _heartbeatLock.Release();
        }
    }

    /// <summary>
    /// 停止心跳定时器
    /// </summary>
    private void StopHeartbeat()
    {
        _heartbeatTimer?.Stop();
        _heartbeatTimer?.Dispose();
        _heartbeatTimer = null;
        
        // 释放锁资源（确保不会阻塞）
        try
        {
            if (_heartbeatLock.CurrentCount == 0)
            {
                _heartbeatLock.Release();
            }
        }
        catch
        {
            // 忽略释放失败
        }
    }
}
