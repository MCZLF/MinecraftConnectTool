using System;
using System.Collections.ObjectModel;
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
        
        // 根据设置决定启动页面
        bool goUpdateWhenStart = ConfigService.Read("goupdatewhenstart", false);
        if (goUpdateWhenStart)
        {
            NavigateTo("Update");
        }
        else
        {
            NavigateTo("Home");
        }
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

    [RelayCommand]
    private async Task StopCore()
    {
        // 根据当前模式调用相应的停止命令
        if (P2PStateService.CurrentMode == CoreMode.Link)
        {
            // 停止Link核心 - 通过获取当前页面来执行停止
            if (CurrentPage is LinkPage linkPage && linkPage.DataContext is LinkPageViewModel linkVm)
            {
                linkVm.StopLink();
            }
            else
            {
                // 如果当前不在Link页面，尝试从缓存获取
                var linkPageFromCache = _pageCacheService.GetOrCreatePage("Link", () => new LinkPage());
                if (linkPageFromCache?.DataContext is LinkPageViewModel linkVmCached)
                {
                    linkVmCached.StopLink();
                }
            }
        }
        else
        {
            // 停止P2P核心 - 通过获取当前页面来执行停止
            if (CurrentPage is P2PPage p2pPage && p2pPage.DataContext is P2PPageViewModel p2pVm)
            {
                await p2pVm.StopP2P();
            }
            else
            {
                // 如果当前不在P2P页面，尝试从缓存获取
                var p2pPageFromCache = _pageCacheService.GetOrCreatePage("P2P", () => new P2PPage());
                if (p2pPageFromCache?.DataContext is P2PPageViewModel p2pVmCached)
                {
                    await p2pVmCached.StopP2P();
                }
            }
        }
    }

    private void UpdateStopButtonToolTip()
    {
        StopButtonToolTip = P2PStateService.CurrentMode == CoreMode.Link
            ? "关闭Link核心"
            : "关闭P2P核心";
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
        GlobalModalTitle = title;
        GlobalModalMessage = message;
        GlobalModalDetail = detail;
        IsGlobalModalVisible = true;

        _globalModalTcs = new TaskCompletionSource<bool>();
        return _globalModalTcs.Task;
    }

    /// <summary>
    /// 隐藏全局对话框
    /// </summary>
    public void HideGlobalModal()
    {
        IsGlobalModalVisible = false;
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
