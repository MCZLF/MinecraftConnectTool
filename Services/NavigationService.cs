using System;
using System.Collections.Generic;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MinecraftConnectTool.Services;

public interface INavigationService
{
    Control? CurrentPage { get; }
    string CurrentPageKey { get; }
    void NavigateTo(string pageKey);
    void RefreshCurrentPage();
    event EventHandler<string>? PageChanged;
}

public partial class NavigationService(
    IPageCacheService pageCacheService,
    Func<string, Control> pageFactory) : ObservableObject, INavigationService
{
    [ObservableProperty]
    private Control? _currentPage;

    [ObservableProperty]
    private string _currentPageKey = string.Empty;

    public event EventHandler<string>? PageChanged;

    // 不需要缓存的页面列表
    private static readonly HashSet<string> _noCachePages = new(StringComparer.OrdinalIgnoreCase)
    {
        "Update"  // 更新页面不缓存，每次重新加载
    };

    public void NavigateTo(string pageKey)
    {
        if (CurrentPageKey == pageKey)
            return;

        // 如果页面不需要缓存，先移除缓存
        if (_noCachePages.Contains(pageKey))
        {
            pageCacheService.RemovePage(pageKey);
        }

        var page = pageCacheService.GetOrCreatePage(pageKey, () => pageFactory(pageKey));
        if (page != null)
        {
            CurrentPage = page;
            CurrentPageKey = pageKey;
            PageChanged?.Invoke(this, pageKey);
        }
    }

    public void RefreshCurrentPage()
    {
        if (string.IsNullOrEmpty(CurrentPageKey))
            return;

        // 清除当前页面缓存
        pageCacheService.RemovePage(CurrentPageKey);

        // 重新创建页面
        var page = pageCacheService.GetOrCreatePage(CurrentPageKey, () => pageFactory(CurrentPageKey));
        if (page != null)
        {
            CurrentPage = page;
            PageChanged?.Invoke(this, CurrentPageKey);
        }
    }
}
