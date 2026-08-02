using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    private readonly string _baseUrl = "http://room2.mct.mczlf.loft.games";//对,就是rom/room2,为了防止旧版本发生崩溃,直接该地址屏蔽旧地址请求
    private System.Timers.Timer? _heartbeatTimer;
    
    // 容错机制相关字段
    private int _heartbeatFailureCount = 0;
    private bool _heartbeatInRecoveryMode;
    private readonly object _playerCacheLock = new();
    private List<PlayerInfo> _lastKnownPlayers = new();
    private DateTime _currentJoinedAt = DateTime.Now;
    private const int MaxHeartbeatFailures = 20;
    private const double HeartbeatIntervalMs = 5000;
    private const double RecoveryHeartbeatIntervalMs = 30000;
    
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

    private int CurrentPort { get; set; }
    
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
                CurrentPort = 0;
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
                CurrentPort = port;
                _currentJoinedAt = result.Data.JoinedAt == default ? DateTime.Now : result.Data.JoinedAt;
                SetLastKnownPlayers(new List<PlayerInfo>
                {
                    CreateCurrentPlayerInfo()
                });
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
            ClearState();
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
            ClearState();
        }
    }

    private void ClearState()
    {
        _heartbeatInRecoveryMode = false;
        CurrentRoomCode = string.Empty;
        CurrentPlayerId = string.Empty;
        CurrentPort = 0;
        IsHost = false;
        IsConnected = false;
        IsKicked = false;
        SetLastKnownPlayers(new List<PlayerInfo>());
    }

    private PlayerInfo CreateCurrentPlayerInfo()
    {
        return new PlayerInfo
        {
            Id = CurrentPlayerId,
            Nickname = string.IsNullOrWhiteSpace(CurrentNickname) ? (IsHost ? "房主" : "玩家") : CurrentNickname,
            Port = CurrentPort,
            JoinedAt = _currentJoinedAt,
            Status = 1,
            Version = GetFormattedVersion()
        };
    }

    private List<PlayerInfo> EnsureCurrentPlayerInList(IEnumerable<PlayerInfo> players)
    {
        var result = players.ToList();
        if (!string.IsNullOrEmpty(CurrentPlayerId) && result.All(p => p.Id != CurrentPlayerId))
        {
            result.Add(CreateCurrentPlayerInfo());
        }
        return result;
    }

    private void SetLastKnownPlayers(List<PlayerInfo> players)
    {
        lock (_playerCacheLock)
        {
            _lastKnownPlayers = players.ToList();
        }
    }

    private List<PlayerInfo> GetLastKnownPlayersSnapshot()
    {
        lock (_playerCacheLock)
        {
            return _lastKnownPlayers.ToList();
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
            if (_heartbeatInRecoveryMode) return GetLastKnownPlayersSnapshot();
            
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/room/{CurrentRoomCode}/player/list");
            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<List<PlayerInfo>>>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (result?.Success == true && result.Data != null)
            {
                var players = EnsureCurrentPlayerInList(result.Data);
                SetLastKnownPlayers(players);
                return players;
            }

            return GetLastKnownPlayersSnapshot();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取玩家列表失败: {ex.Message}");
            return GetLastKnownPlayersSnapshot();
        }
    }
    /// <summary>
    /// 异步刷新玩家列表
    /// </summary>
    public async Task RefreshPlayersAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(CurrentRoomCode) || _heartbeatInRecoveryMode) return;
            
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/room/{CurrentRoomCode}/player/list");
            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<List<PlayerInfo>>>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (result?.Success == true && result.Data != null)
            {
                SetLastKnownPlayers(EnsureCurrentPlayerInList(result.Data));
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
        var roomCode = CurrentRoomCode;
        var playerId = CurrentPlayerId;

        try
        {
            if (string.IsNullOrEmpty(roomCode) || string.IsNullOrEmpty(playerId)) return;

            var request = new { PlayerId = playerId };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/room/{roomCode}/player/heartbeat", content);

            if (roomCode != CurrentRoomCode || playerId != CurrentPlayerId || !IsConnected)
            {
                Log($"[容错] 忽略过期心跳响应，Room={roomCode}");
                return;
            }

            if (response.IsSuccessStatusCode)
            {
                if (_heartbeatInRecoveryMode)
                {
                    _heartbeatInRecoveryMode = false;
                    if (_heartbeatTimer != null)
                    {
                        _heartbeatTimer.Interval = HeartbeatIntervalMs;
                    }
                    Log($"[容错] 玩家管理服务已恢复，恢复正常心跳频率，Room={roomCode}");
                    PlayersChanged?.Invoke(this, EventArgs.Empty);
                }

                if (_heartbeatFailureCount > 0)
                {
                    Log($"[容错] 心跳恢复，重置容错计数，Room={roomCode}");
                    _heartbeatFailureCount = 0;
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<object>>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Message == "KICKED" && !IsHost)
                {
                    Log($"[容错] 收到踢出指令，房客被踢出，Room={roomCode}");
                    IsKicked = true;
                    Kicked?.Invoke(this, "已被房主踢出房间");
                    StopHeartbeat();
                    ClearState();
                }
                else if (result?.Message == "KICKED" && IsHost)
                {
                    Log($"[容错] 房主收到KICKED消息（已忽略），Room={roomCode}");
                }
            }
            else
            {
                if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest)
                {
                    if (await RecoverRegistrationAsync(roomCode))
                    {
                        _heartbeatInRecoveryMode = false;
                        _heartbeatFailureCount = 0;
                        if (_heartbeatTimer != null)
                        {
                            _heartbeatTimer.Interval = HeartbeatIntervalMs;
                        }
                        Log($"[容错] 房间管理服务重注册成功，Room={roomCode}");
                        PlayersChanged?.Invoke(this, EventArgs.Empty);
                        return;
                    }
                }

                HandleHeartbeatFailure($"StatusCode={(int)response.StatusCode}", roomCode);
            }
        }
        catch (Exception ex)
        {
            HandleHeartbeatFailure($"Exception={ex.GetType().Name}", roomCode);
        }
    }

    private async Task<bool> RecoverRegistrationAsync(string roomCode)
    {
        try
        {
            if (IsHost)
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/room/{roomCode}");
                if (response.StatusCode != HttpStatusCode.NotFound)
                {
                    return false;
                }

                Log($"[容错] 房间管理服务房间丢失，尝试重建房间，Room={roomCode}");
                var request = new { RoomCode = roomCode, Version = GetFormattedVersion() };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var createResponse = await _httpClient.PostAsync($"{_baseUrl}/api/room/create", content);
                var createJson = await createResponse.Content.ReadAsStringAsync();
                var createResult = JsonSerializer.Deserialize<ApiResponse<CreateRoomResponse>>(createJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (createResult?.Success == true && createResult.Data != null)
                {
                    CurrentPlayerId = createResult.Data.HostPlayerId;
                    CurrentPort = 0;
                    return true;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(CurrentNickname)) return false;

                Log($"[容错] 房间管理服务玩家丢失，尝试重新加入房间，Room={roomCode}");
                var request = new { Nickname = CurrentNickname, RoomCode = roomCode, Port = CurrentPort, Version = GetFormattedVersion() };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var joinResponse = await _httpClient.PostAsync($"{_baseUrl}/api/room/{roomCode}/player/join", content);
                var joinJson = await joinResponse.Content.ReadAsStringAsync();
                var joinResult = JsonSerializer.Deserialize<ApiResponse<PlayerInfo>>(joinJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (joinResult?.Success == true && joinResult.Data != null)
                {
                    CurrentPlayerId = joinResult.Data.Id;
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log($"[容错] 房间管理服务重注册失败: {ex.GetType().Name}, Room={roomCode}");
        }

        return false;
    }

    /// <summary>
    /// 处理心跳失败（容错机制核心逻辑）
    /// </summary>
    private void HandleHeartbeatFailure(string errorInfo, string roomCode)
    {
        if (string.IsNullOrEmpty(roomCode) || roomCode != CurrentRoomCode || string.IsNullOrEmpty(CurrentPlayerId) || !IsConnected)
        {
            Log($"[容错] 忽略过期心跳异常: {errorInfo}, Room={roomCode}");
            return;
        }

        if (IsHost)
        {
            Log($"[容错] 房主心跳异常（已忽略）: {errorInfo}, Room={roomCode}");
            return;
        }

        if (_heartbeatInRecoveryMode)
        {
            Log($"[容错] 玩家管理服务恢复探测失败: {errorInfo}, Room={roomCode}");
            return;
        }

        _heartbeatFailureCount++;
        int remaining = MaxHeartbeatFailures - _heartbeatFailureCount;

        Log($"[容错] 房客心跳异常 ({_heartbeatFailureCount}/{MaxHeartbeatFailures}): {errorInfo}, Room={roomCode}");

        if (_heartbeatFailureCount >= MaxHeartbeatFailures)
        {
            Log($"[容错] 房客容错次数耗尽，玩家管理心跳降级为低频恢复探测，Room={roomCode}");
            _heartbeatInRecoveryMode = true;
            if (_heartbeatTimer != null)
            {
                _heartbeatTimer.Interval = RecoveryHeartbeatIntervalMs;
            }
            PlayersChanged?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            Log($"[容错] 房客继续容错，剩余 {remaining} 次，Room={roomCode}");
        }
    }

    /// <summary>
    /// 启动心跳定时器
    /// </summary>
    private void StartHeartbeat()
    {
        StopHeartbeat();
        _heartbeatInRecoveryMode = false;
        _heartbeatFailureCount = 0;
        Log($"[容错] 启动心跳定时器，容错计数已重置，Room={CurrentRoomCode}, IsHost={IsHost}");
        
        _heartbeatTimer = new System.Timers.Timer(HeartbeatIntervalMs); // 每5秒发送一次心跳
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
            // 安全释放：捕获 SemaphoreFullException 防止重复释放导致崩溃
            try
            {
                _heartbeatLock.Release();
            }
            catch (SemaphoreFullException)
            {
                Log($"[容错] 信号量已满，跳过释放（可能由 StopHeartbeat 引起）");
            }
        }
    }

    /// <summary>
    /// 停止心跳定时器
    /// </summary>
    private void StopHeartbeat()
    {
        // 先停止定时器，防止新的心跳任务启动
        _heartbeatTimer?.Stop();
        _heartbeatTimer?.Dispose();
        _heartbeatTimer = null;
        
        // 不主动释放信号量，原因：
        // 1. 如果在心跳回调内部调用（SendHeartbeatAsync中被踢出/容错失败），finally块会自动释放
        // 2. 如果在外部调用（LeaveRoomAsync等），可能有正在执行的心跳任务，避免重复释放导致崩溃
        // 3. 信号量会在下次StartHeartbeat时重新初始化或由正在执行的任务自然释放
    }
}
