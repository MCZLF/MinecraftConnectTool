using System;
using System.Collections.Generic;
using Avalonia.Controls;

namespace MinecraftConnectTool.Services;

public interface IPageCacheService
{
    Control? GetOrCreatePage(string pageKey, Func<Control> factory);
    void ClearCache();
    void RemovePage(string pageKey);
}

/// <summary>
/// 页面缓存服务 - 支持LRU缓存策略（默认模式和性能模式都有缓存限制）
/// </summary>
public class PageCacheService : IPageCacheService
{
    // 默认模式下的最大缓存页面数（限制为3页，控制内存）
    private const int DefaultMaxCacheSize = 3;
    // 性能模式下的最大缓存页面数（更激进的限制）
    private const int PerformanceMaxCacheSize = 2;

    // 使用LinkedList实现LRU（最近最少使用）策略
    private readonly LinkedList<string> _accessOrder = new();
    private readonly Dictionary<string, Control> _pageCache = [];
    private readonly object _lockObj = new();

    /// <summary>
    /// 性能模式是否开启
    /// </summary>
    private static bool IsPerformanceModeEnabled => ConfigService.Read<bool>("EnablePerformanceMode", false);

    /// <summary>
    /// 当前模式下的最大缓存数量
    /// </summary>
    private static int CurrentMaxCacheSize => IsPerformanceModeEnabled ? PerformanceMaxCacheSize : DefaultMaxCacheSize;

    public Control? GetOrCreatePage(string pageKey, Func<Control> factory)
    {
        lock (_lockObj)
        {
            if (_pageCache.TryGetValue(pageKey, out var cachedPage))
            {
                // 更新访问顺序（移到链表末尾表示最近使用）
                UpdateAccessOrder(pageKey);
                return cachedPage;
            }

            // 检查缓存大小限制（默认模式和性能模式都限制）
            if (_pageCache.Count >= CurrentMaxCacheSize)
            {
                // 移除最久未使用的页面
                RemoveLeastRecentlyUsed();
            }

            var newPage = factory();
            _pageCache[pageKey] = newPage;
            _accessOrder.AddLast(pageKey);
            return newPage;
        }
    }

    /// <summary>
    /// 更新页面访问顺序
    /// </summary>
    private void UpdateAccessOrder(string pageKey)
    {
        var node = _accessOrder.Find(pageKey);
        if (node != null)
        {
            _accessOrder.Remove(node);
            _accessOrder.AddLast(node);
        }
    }

    /// <summary>
    /// 移除最久未使用的页面
    /// </summary>
    private void RemoveLeastRecentlyUsed()
    {
        if (_accessOrder.Count == 0) return;

        // 链表头部是最久未使用的
        var lruKey = _accessOrder.First!.Value;
        _accessOrder.RemoveFirst();

        if (_pageCache.TryGetValue(lruKey, out var page))
        {
            // 尝试释放页面资源
            if (page is IDisposable disposable)
            {
                disposable.Dispose();
            }
            _pageCache.Remove(lruKey);
        }
    }

    public void ClearCache()
    {
        lock (_lockObj)
        {
            // 始终尝试释放所有缓存页面的资源（优化内存管理）
            foreach (var page in _pageCache.Values)
            {
                if (page is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _pageCache.Clear();
            _accessOrder.Clear();
        }
    }

    public void RemovePage(string pageKey)
    {
        lock (_lockObj)
        {
            if (_pageCache.TryGetValue(pageKey, out var page))
            {
                // 始终尝试释放页面资源（优化内存管理）
                if (page is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                _pageCache.Remove(pageKey);
            }

            var node = _accessOrder.Find(pageKey);
            if (node != null)
            {
                _accessOrder.Remove(node);
            }
        }
    }
}
