using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;

namespace MinecraftConnectTool.Services;

/// <summary>
/// 剪贴板帮助类 - 用于跨ViewModel访问剪贴板
/// </summary>
public static class ClipboardHelper
{
    private static TopLevel? _topLevel;

    /// <summary>
    /// 初始化剪贴板帮助类
    /// </summary>
    public static void Initialize(TopLevel topLevel)
    {
        _topLevel = topLevel;
    }

    /// <summary>
    /// 设置文本到剪贴板
    /// </summary>
    public static async Task<bool> SetTextAsync(string text)
    {
        try
        {
            if (_topLevel?.Clipboard != null)
            {
                await _topLevel.Clipboard.SetTextAsync(text);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"设置剪贴板失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取剪贴板文本
    /// </summary>
    public static async Task<string?> GetTextAsync()
    {
        try
        {
            if (_topLevel?.Clipboard != null)
            {
                return await _topLevel.Clipboard.GetTextAsync();
            }
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"获取剪贴板失败: {ex.Message}");
            return null;
        }
    }
}
