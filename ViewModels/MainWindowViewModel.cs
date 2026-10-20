using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinecraftConnectTool.Services;
using MinecraftConnectTool.Views.Pages;
using MinecraftConnectTool.ViewModels.Pages;

namespace MinecraftConnectTool.ViewModels;

public partial class NavigationItem : ObservableObject
{
    [ObservableProperty]
    private string _key = string.Empty;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _icon = string.Empty;

    [ObservableProperty]
    private bool _isSelected;
}

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly PageCacheService _pageCacheService;
    private readonly NavigationService _navigationService;

    [ObservableProperty]
    private ObservableCollection<NavigationItem> _navigationItems = [];

    [ObservableProperty]
    private NavigationItem? _selectedNavigationItem;

    [ObservableProperty]
    private Control? _currentPage;

    [ObservableProperty]
    private string _currentPageKey = string.Empty;

    [ObservableProperty]
    private bool _isP2PRunning;

    [ObservableProperty]
    private string _stopButtonToolTip = "关闭核心";

    #region 全局 Modal 属性

    [ObservableProperty]
    private bool _isGlobalModalVisible = false;

    [ObservableProperty]
    private string _globalModalTitle = "";

    [ObservableProperty]
    private string _globalModalMessage = "";

    [ObservableProperty]
    private string _globalModalDetail = "";

    private TaskCompletionSource<bool>? _globalModalTcs;

    #endregion

    public MainWindowViewModel()
    {
        _pageCacheService = new PageCacheService();
        _navigationService = new NavigationService(_pageCacheService, CreatePage);
        _navigationService.PageChanged += OnPageChanged;

        // 监听P2P状态变化
        P2PStateService.StateChanged += OnP2PStateChanged;
        P2PStateService.ModeChanged += OnP2PModeChanged;

        InitializeNavigationItems();
        
        NavigateTo(GetDefaultStartupPageKey());
    }

    private string GetDefaultStartupPageKey()
    {
        try
        {
            var configPath = ConfigService.GetConfigFilePath();
            if (!File.Exists(configPath))
            {
                return "Home";
            }

            var json = File.ReadAllText(configPath);
            var config = JsonNode.Parse(json)?.AsObject();
            if (config?.TryGetPropertyValue("DefaultStartupPage", out var value) != true || value == null)
            {
                return "Home";
            }

            var defaultStartupPage = value.GetValue<string>();
            if (NavigationItems.Any(item => item.Key == defaultStartupPage))
            {
                return defaultStartupPage;
            }
        }
        catch
        {
        }

        ConfigService.Delete("DefaultStartupPage");
        return "Home";
    }

    private void OnP2PStateChanged(object? sender, bool isRunning)
    {
        IsP2PRunning = isRunning;
        UpdateStopButtonToolTip();
    }

    private void OnP2PModeChanged(object? sender, CoreMode mode)
    {
        UpdateStopButtonToolTip();
    }

    public Func<Task>? StopAllCoresHandler { get; set; }
    public Func<System.Collections.Generic.IReadOnlyList<string>>? ActiveCoreNamesProvider { get; set; }

    [RelayCommand]
    private async Task StopCore()
    {
        if (StopAllCoresHandler != null)
            await StopAllCoresHandler();
    }

    public void UpdateStopButtonToolTip()
    {
        var cores = ActiveCoreNamesProvider?.Invoke();
        StopButtonToolTip = cores?.Count switch
        {
            1 => $"关闭{cores[0]}核心",
            >= 2 => "关闭所有核心",
            _ => "关闭核心"
        };
    }

    private void InitializeNavigationItems()
    {
        NavigationItems.Add(new NavigationItem { Key = "Home", Title = "首页", Icon = "Home" });
        NavigationItems.Add(new NavigationItem { Key = "P2P", Title = "P2P模式", Icon = "Contact" });
        NavigationItems.Add(new NavigationItem { Key = "Link", Title = "Link模式", Icon = "Link" });
        NavigationItems.Add(new NavigationItem { Key = "ET", Title = "ET", Icon = "CodeBraces" });
        NavigationItems.Add(new NavigationItem { Key = "Optimize", Title = "优化", Icon = "RocketLaunch" });
        NavigationItems.Add(new NavigationItem { Key = "Update", Title = "检查更新", Icon = "Refresh" });
        NavigationItems.Add(new NavigationItem { Key = "Settings", Title = "设置", Icon = "Settings" });
    }

    private void OnPageChanged(object? sender, string pageKey)
    {
        // 先更新 CurrentPage，然后更新 CurrentPageKey 触发属性变更通知
        CurrentPage = _navigationService.CurrentPage;
        CurrentPageKey = pageKey;

        foreach (var item in NavigationItems)
        {
            item.IsSelected = item.Key == pageKey;
        }
    }

    [RelayCommand]
    private void NavigateTo(string pageKey)
    {
        _navigationService.NavigateTo(pageKey);
    }

    public void RefreshCurrentPage()
    {
        _navigationService.RefreshCurrentPage();
    }

    #region 全局 Modal 方法

    /// <summary>
    /// 显示全局确认对话框
    /// </summary>
    public Task<bool> ShowGlobalModalAsync(string title, string message, string detail)
    {
        if (IsGlobalModalVisible && _globalModalTcs != null)
            return Task.FromResult(false);

        GlobalModalTitle = title;
        GlobalModalMessage = message;
        GlobalModalDetail = detail;
        _globalModalTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        IsGlobalModalVisible = true;

        return _globalModalTcs.Task;
    }

    /// <summary>
    /// 隐藏全局对话框
    /// </summary>
    public void HideGlobalModal()
    {
        IsGlobalModalVisible = false;
        _globalModalTcs?.TrySetResult(false);
    }

    public async Task StopLinkForExitAsync()
    {
        var page = CurrentPage as LinkPage ?? _pageCacheService.GetCachedPage("Link") as LinkPage;
        if (page?.DataContext is LinkPageViewModel vm)
            await vm.StopLinkAsync();
    }

    public void ResetStoppedCorePageStates()
    {
        var p2pPage = CurrentPage as P2PPage ?? _pageCacheService.GetCachedPage("P2P") as P2PPage;
        if (p2pPage?.DataContext is P2PPageViewModel p2p)
            p2p.IsPlayerListConnected = false;

        var linkPage = CurrentPage as LinkPage ?? _pageCacheService.GetCachedPage("Link") as LinkPage;
        if (linkPage?.DataContext is LinkPageViewModel link)
            link.ResetStoppedState();

        var etPage = CurrentPage as ETPage ?? _pageCacheService.GetCachedPage("ET") as ETPage;
        if (etPage?.DataContext is ETPageViewModel et)
            et.IsETRunningForPanel = false;
    }

    [RelayCommand]
    private void GlobalModalConfirm()
    {
        _globalModalTcs?.TrySetResult(true);
        IsGlobalModalVisible = false;
    }

    [RelayCommand]
    private void GlobalModalCancel()
    {
        _globalModalTcs?.TrySetResult(false);
        IsGlobalModalVisible = false;
    }

    #endregion

    private Control CreatePage(string pageKey)
    {
        return pageKey switch
        {
            "Home" => new HomePage(),
            "P2P" => new P2PPage(),
            "Link" => new LinkPage(),
            "ET" => new ETPage(),
            "Optimize" => new OptimizePage(),
            "Settings" => new SettingsPage(),
            "Update" => new UpdatePage(),
            "Help" => new HelpPage(),
            _ => new HomePage()
        };
    }
}
