using System;

namespace MinecraftConnectTool.Services;

/// <summary>
/// 核心运行模式
/// </summary>
public enum CoreMode
{
    None,
    P2P,
    Link
}

/// <summary>
/// P2P/Link核心状态服务 - 用于跨页面/跨ViewModel通信核心运行状态
/// </summary>
public static class P2PStateService
{
    private static bool _isRunning;
    private static CoreMode _currentMode = CoreMode.None;

    /// <summary>
    /// 核心运行状态
    /// </summary>
    public static bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (_isRunning != value)
            {
                _isRunning = value;
                StateChanged?.Invoke(null, value);
            }
        }
    }

    /// <summary>
    /// 当前运行模式
    /// </summary>
    public static CoreMode CurrentMode
    {
        get => _currentMode;
        private set
        {
            if (_currentMode != value)
            {
                _currentMode = value;
                ModeChanged?.Invoke(null, value);
            }
        }
    }

    /// <summary>
    /// 状态变化事件
    /// </summary>
    public static event EventHandler<bool>? StateChanged;

    /// <summary>
    /// 模式变化事件
    /// </summary>
    public static event EventHandler<CoreMode>? ModeChanged;

    /// <summary>
    /// 设置核心运行状态
    /// </summary>
    public static void SetRunning(bool isRunning, CoreMode mode = CoreMode.P2P)
    {
        if (isRunning)
        {
            CurrentMode = mode;
        }
        else
        {
            CurrentMode = CoreMode.None;
        }
        IsRunning = isRunning;
    }
}
