using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using MinecraftConnectTool.ExtensionUI;
using MinecraftConnectTool.Services;

namespace MinecraftConnectTool.Views;

public partial class MainWindow
{
    private bool _allowClose;
    private bool _windowInitialized;
    private bool _closeRequestPending;
    private TrayIcon? _trayIcon;
    private WindowState _stateBeforeHide = WindowState.Normal;
    private static readonly string[] CoreProcessNames = ["main", "link", "easytier-core"];

    private static bool HasActiveCore()
    {
        return GetActiveCoreNames().Count > 0;
    }

    private static List<string> GetActiveCoreNames()
    {
        var names = new List<string>();
        var modes = new[] { CoreMode.P2P, CoreMode.Link, CoreMode.ET };
        for (var i = 0; i < CoreProcessNames.Length; i++)
        {
            var mode = modes[i];
            var running = P2PStateService.IsRunning && P2PStateService.CurrentMode == mode;
            if (mode == CoreMode.ET)
                running |= ETModeService.Instance.State != ETCoreState.Stopped;

            var processes = Process.GetProcessesByName(CoreProcessNames[i]);
            foreach (var process in processes)
            {
                running |= CoreProcessOwnership.IsManaged(process);
                process.Dispose();
            }
            if (running)
                names.Add(mode.ToString());
        }

        return names;
    }

    private async Task HandleCloseRequestAsync(bool forceExit = false)
    {
        if (_closeRequestPending)
            return;

        _closeRequestPending = true;
        try
        {
            var behavior = forceExit ? "Exit" : ConfigService.Read<string>("WindowCloseBehavior", "");
            if (behavior is not ("MinimizeToTray" or "Exit"))
            {
                if (_viewModel?.IsGlobalModalVisible == true)
                    return;

                var activeCores = GetActiveCoreNames();
                if (activeCores.Count == 0)
                {
                    _allowClose = true;
                    Close();
                    return;
                }

                var result = await CloseBehaviorDialog.ShowAsync(this, activeCores);
                if (result == null)
                    return;

                behavior = result.Behavior;
                if (result.RememberChoice)
                    ConfigService.Write("WindowCloseBehavior", behavior);
            }

            if (behavior == "MinimizeToTray")
            {
                HideToTray();
                return;
            }

            await StopCoresForExitAsync();
            _allowClose = true;
            Close();
        }
        catch (Exception ex)
        {
            RestoreFromTray();
            LocalStorageService.AppendAppLog($"处理窗口关闭失败：{ex}{Environment.NewLine}");
            if (_viewModel != null && !_viewModel.IsGlobalModalVisible)
                await _viewModel.ShowGlobalModalAsync("无法关闭程序", "处理关闭请求失败，程序仍保持运行。", ex.Message);
        }
        finally
        {
            _closeRequestPending = false;
        }
    }

    private void HideToTray()
    {
        if (!OperatingSystem.IsWindows())
        {
            WindowState = WindowState.Minimized;
            return;
        }

        if (_trayIcon == null)
        {
            using var stream = AssetLoader.Open(new Uri("avares://MinecraftConnectTool/IconGroup3000.ico"));
            var menu = new NativeMenu();
            var restore = new NativeMenuItem("显示主窗口");
            restore.Click += (_, _) => Dispatcher.UIThread.Post(RestoreFromTray);
            var exit = new NativeMenuItem("退出核心并关闭程序");
            exit.Click += (_, _) => Dispatcher.UIThread.Post(() => { _ = HandleCloseRequestAsync(true); });
            menu.Items.Add(restore);
            menu.Items.Add(new NativeMenuItemSeparator());
            menu.Items.Add(exit);
            _trayIcon = new TrayIcon
            {
                Icon = new WindowIcon(stream),
                ToolTipText = "MinecraftConnectTool · 联机核心运行中",
                Menu = menu,
                IsVisible = true
            };
            _trayIcon.Clicked += (_, _) => Dispatcher.UIThread.Post(RestoreFromTray);
        }

        if (_trayIcon.NativeMenuExporter == null)
        {
            _trayIcon.Dispose();
            _trayIcon = null;
            WindowState = WindowState.Minimized;
            return;
        }

        _trayIcon.IsVisible = true;
        _stateBeforeHide = WindowState == WindowState.Minimized ? WindowState.Normal : WindowState;
        Hide();
    }

    private void RestoreFromTray()
    {
        Show();
        WindowState = _stateBeforeHide;
        Activate();
        if (_trayIcon != null)
            _trayIcon.IsVisible = false;
    }

    private Task? _stopAllCoresTask;

    private Task StopCoresForExitAsync()
    {
        if (_stopAllCoresTask == null || _stopAllCoresTask.IsCompleted)
            _stopAllCoresTask = StopAllCoresAsync();
        return _stopAllCoresTask;
    }

    private async Task StopAllCoresFromButtonAsync()
    {
        try
        {
            await StopCoresForExitAsync();
        }
        catch (Exception ex)
        {
            if (_viewModel != null)
            {
                _viewModel.IsP2PRunning = true;
                if (!_viewModel.IsGlobalModalVisible)
                    await _viewModel.ShowGlobalModalAsync("停止核心失败", "部分核心未能停止，请重试。", ex.Message);
            }
        }
    }

    private async Task StopAllCoresAsync()
    {
        var errors = new List<Exception>();
        async Task AttemptAsync(Func<Task> stop)
        {
            try
            {
                await stop();
            }
            catch (Exception ex)
            {
                errors.Add(ex);
                LocalStorageService.AppendAppLog($"停止核心阶段失败：{ex}{Environment.NewLine}");
            }
        }

        if (_viewModel != null)
            await AttemptAsync(_viewModel.StopLinkForExitAsync);
        await AttemptAsync(() => PlayerListService.Instance.IsHost
            ? PlayerListService.Instance.CloseRoomAsync()
            : PlayerListService.Instance.LeaveRoomAsync());
        await AttemptAsync(async () =>
        {
            if (!await _p2pService.stopp2p())
                throw new InvalidOperationException("P2P 核心停止失败。");
        });
        await AttemptAsync(async () =>
        {
            if (!await ETModeService.Instance.StopETAsync())
                throw new InvalidOperationException("ET 核心停止失败。");
        });

        foreach (var name in CoreProcessNames)
        {
            foreach (var process in Process.GetProcessesByName(name))
            {
                using (process)
                {
                    try
                    {
                        if (!CoreProcessOwnership.IsManaged(process) || process.HasExited)
                            continue;
                        process.Kill(true);
                        await process.WaitForExitAsync();
                    }
                    catch (InvalidOperationException)
                    {
                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex);
                    }
                }
            }
        }
        if (errors.Count > 0)
            throw new AggregateException("清理核心时发生错误。", errors);

        P2PStateService.SetRunning(false);
        if (_viewModel != null)
            _viewModel.IsP2PRunning = false;
    }
}
