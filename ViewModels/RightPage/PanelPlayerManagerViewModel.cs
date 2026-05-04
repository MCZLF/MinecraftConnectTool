using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinecraftConnectTool.Services;

namespace MinecraftConnectTool.ViewModels.RightPage;

/// <summary>
/// 玩家信息展示模型
/// </summary>
public partial class PlayerViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _nickname = string.Empty;

    [ObservableProperty]
    private string _version = string.Empty;

    [ObservableProperty]
    private DateTime _joinedAt;

    /// <summary>
    /// 是否是房主
    /// </summary>
    public bool IsHostPlayer => Nickname == "房主";

    /// <summary>
    /// 首字母（用于头像显示）
    /// </summary>
    public string Initial => string.IsNullOrEmpty(Nickname) ? "?" : Nickname.Substring(0, 1).ToUpper();
}

/// <summary>
/// 玩家管理面板ViewModel
/// </summary>
public partial class PanelPlayerManagerViewModel : ObservableObject
{
    private readonly PlayerListService _playerListService;
    private System.Timers.Timer? _refreshTimer;

    [ObservableProperty]
    private ObservableCollection<PlayerViewModel> _players = new();

    [ObservableProperty]
    private bool _isHost;

    [ObservableProperty]
    private string _roomCode = string.Empty;

    [ObservableProperty]
    private string _playerId = string.Empty;

    [ObservableProperty]
    private string _roleText = "房客";

    [ObservableProperty]
    private string _roomCodeText = "房间: -";

    [ObservableProperty]
    private string _playerCountText = "0人在线";

    [ObservableProperty]
    private bool _isEmpty = true;

    [ObservableProperty]
    private string _panelTitle = "玩家管理";

    /// <summary>
    /// 关闭请求事件
    /// </summary>
    public event EventHandler? CloseRequested;

    public PanelPlayerManagerViewModel()
    {
        _playerListService = PlayerListService.Instance;
        
        // 订阅服务事件
        _playerListService.PlayersChanged += OnPlayersChanged;
        _playerListService.JoinedRoom += OnJoinedRoom;
        _playerListService.Kicked += OnKicked;

        // 初始化状态（异步，不阻塞构造函数）
        _ = InitializeAsync();
        
        // 启动定时刷新
        StartRefreshTimer();
    }
    
    /// <summary>
    /// 异步初始化
    /// </summary>
    private async Task InitializeAsync()
    {
        try
        {
            await UpdateStateFromServiceAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ViewModel初始化失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 从服务更新状态
    /// </summary>
    private async Task UpdateStateFromServiceAsync()
    {
        IsHost = _playerListService.IsHost;
        RoomCode = _playerListService.CurrentRoomCode;
        PlayerId = _playerListService.CurrentPlayerId;

        RoleText = IsHost ? "房主" : "房客";
        RoomCodeText = string.IsNullOrEmpty(RoomCode) ? "房间: -" : $"房间: {RoomCode}";
        PanelTitle = IsHost ? "玩家管理" : "房间列表";

        try
        {
            await RefreshPlayersListAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"初始化刷新玩家列表失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 启动定时刷新
    /// </summary>
    private void StartRefreshTimer()
    {
        _refreshTimer = new System.Timers.Timer(3000); // 每3秒刷新一次
        _refreshTimer.Elapsed += OnRefreshTimerElapsed;
        _refreshTimer.AutoReset = true;
        _refreshTimer.Start();
    }
    
    /// <summary>
    /// 定时器触发刷新
    /// </summary>
    private async void OnRefreshTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        try
        {
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"定时刷新失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 玩家列表变化事件
    /// </summary>
    private void OnPlayersChanged(object? sender, EventArgs e)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
        {
            await RefreshPlayersListAsync();
        });
    }

    /// <summary>
    /// 加入房间事件
    /// </summary>
    private void OnJoinedRoom(object? sender, bool isHost)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
        {
            await UpdateStateFromServiceAsync();
        });
    }

    /// <summary>
    /// 被踢出事件
    /// </summary>
    private void OnKicked(object? sender, string reason)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            // 被踢出后关闭面板
            CloseCommand.Execute(null);
        });
    }

    /// <summary>
    /// 刷新玩家列表（异步）
    /// </summary>
    private async Task RefreshPlayersListAsync()
    {
        var players = await _playerListService.GetPlayersAsync();

        // 在UI线程上更新列表，避免重合问题
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            Players.Clear();

            // 排序：房主在最上方，其他玩家按加入时间排序
            var sortedPlayers = players
                .OrderByDescending(p => p.Nickname == "房主")
                .ThenBy(p => p.JoinedAt)
                .ToList();

            foreach (var player in sortedPlayers)
            {
                Players.Add(new PlayerViewModel
                {
                    Id = player.Id,
                    Nickname = player.Nickname,
                    Version = player.Version,
                    JoinedAt = player.JoinedAt
                });
            }

            IsEmpty = Players.Count == 0;
            PlayerCountText = $"{Players.Count}人在线";
        });
    }

    /// <summary>
    /// 刷新命令
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            await _playerListService.RefreshPlayersAsync();
            await RefreshPlayersListAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"刷新玩家列表失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 踢出玩家命令
    /// </summary>
    [RelayCommand]
    private async Task KickPlayerAsync(string playerId)
    {
        if (!IsHost) return;

        try
        {
            var success = await _playerListService.KickPlayerAsync(playerId, "房主踢出");
            if (success)
            {
                await RefreshAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"踢出玩家失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 关闭命令
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        _refreshTimer?.Stop();
        _refreshTimer?.Dispose();
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 清理资源
    /// </summary>
    public void Dispose()
    {
        _refreshTimer?.Stop();
        _refreshTimer?.Dispose();
        _playerListService.PlayersChanged -= OnPlayersChanged;
        _playerListService.JoinedRoom -= OnJoinedRoom;
        _playerListService.Kicked -= OnKicked;
    }
}
