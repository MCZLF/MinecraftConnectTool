using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinecraftConnectTool.Services;

namespace MinecraftConnectTool.ViewModels.RightPage;

/// <summary>
/// ET玩家信息展示模型
/// </summary>
public partial class ETPlayerViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isHost;

    [ObservableProperty]
    private string _displayName = "";

    /// <summary>
    /// 头像首字母
    /// </summary>
    public string Initial => IsHost ? "H" : (string.IsNullOrEmpty(DisplayName) ? "?" : DisplayName.Substring(0, 1).ToUpper());
}

/// <summary>
/// ET房间列表面板ViewModel
/// </summary>
public partial class ETRoomListViewModel : ObservableObject
{
    private readonly ETModeService _etService;

    [ObservableProperty]
    private ObservableCollection<ETPlayerViewModel> _players = new();

    [ObservableProperty]
    private string _roomCodeText = "房间: -";

    [ObservableProperty]
    private string _playerCountText = "0人在线";

    [ObservableProperty]
    private bool _isEmpty = true;

    [ObservableProperty]
    private string _panelTitle = "ET 房间列表";

    [ObservableProperty]
    private string _roleText = "";

    [ObservableProperty]
    private string _statusText = "";

    public event EventHandler? CloseRequested;

    public ETRoomListViewModel()
    {
        _etService = ETModeService.Instance;
        _etService.ScfPlayersUpdated += OnScfPlayersUpdated;
        _etService.CoreStopped += OnCoreStopped;
        _etService.PromptCodeGenerated += OnPromptCodeGenerated;
        _etService.StatusChanged += OnStatusChanged;
        UpdateRoomInfo();

        // 加载当前已缓存的玩家列表
        var cached = _etService.CurrentScfPlayers;
        if (cached.Count > 0) RefreshPlayerDisplay(cached);
    }

    private void OnStatusChanged(object? sender, string status)
    {
        Dispatcher.UIThread.Post(() => StatusText = status);
    }

    private void OnPromptCodeGenerated(object? sender, string code)
    {
        Dispatcher.UIThread.Post(() =>
        {
            RoomCodeText = $"房间: {code}";
            RoleText = "房主";
        });
    }

    private void OnScfPlayersUpdated(object? sender, IReadOnlyList<ScfPlayerProfile> profiles)
    {
        Dispatcher.UIThread.Post(() => RefreshPlayerDisplay(profiles));
    }

    private void RefreshPlayerDisplay(IReadOnlyList<ScfPlayerProfile> profiles)
    {
        Players.Clear();

        // 去重：同一 machineId 只保留一个
        var unique = profiles
            .GroupBy(p => p.MachineId)
            .Select(g => g.First())
            .OrderByDescending(p => p.Kind == PlayerRole.HOST)
            .ThenBy(p => p.Name)
            .ToList();

        foreach (var p in unique)
        {
            var isHost = p.Kind == PlayerRole.HOST;
            Players.Add(new ETPlayerViewModel
            {
                IsHost = isHost,
                DisplayName = p.Name
            });
        }

        IsEmpty = Players.Count == 0;
        PlayerCountText = $"{Players.Count} 人在线";
    }

    private void OnCoreStopped(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Players.Clear();
            IsEmpty = true;
            PlayerCountText = "0人在线";
            RoomCodeText = "房间: -";
            RoleText = "";
            StatusText = "核心已停止";
        });
    }

    private void UpdateRoomInfo()
    {
        var lobby = _etService.LobbyInfo;
        if (lobby != null) RoomCodeText = $"房间: {lobby.FullCode}";

        if (_etService.State != ETCoreState.Stopped)
        {
            RoleText = lobby != null ? "房主" : "已连接";
            StatusText = _etService.State == ETCoreState.Ready ? "已就绪" : "运行中";
        }
        else
        {
            StatusText = "核心未启动";
        }
    }

    [RelayCommand]
    private void Refresh()
    {
        UpdateRoomInfo();
        RefreshPlayerDisplay(_etService.CurrentScfPlayers);
    }

    [RelayCommand]
    private void Close()
    {
        _etService.ScfPlayersUpdated -= OnScfPlayersUpdated;
        _etService.CoreStopped -= OnCoreStopped;
        _etService.PromptCodeGenerated -= OnPromptCodeGenerated;
        _etService.StatusChanged -= OnStatusChanged;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
