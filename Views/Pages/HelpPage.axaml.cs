using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Material.Icons;
using Material.Icons.Avalonia;
using MinecraftConnectTool.Services;
using MinecraftConnectTool.ViewModels.Pages;

namespace MinecraftConnectTool.Views.Pages;

public partial class HelpPage : UserControl
{
    private const string RemoteControlStartText = "启动远程控制";
    private const string RemoteControlStopText = "关闭远程控制";
    private const string RemoteControlLoadingText = "正在启动...";
    private const string RemoteControlStoppingText = "正在关闭...";
    private bool _isRemoteControlRunning;
    private static readonly HttpClient HttpClient = new();

    public HelpPage()
    {
        InitializeComponent();
        DataContext = new HelpPageViewModel();
        ConfigureRemoteControlButton();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnTutorialLinkClick(object? sender, PointerPressedEventArgs e)
    {
        OpenUrl("https://mct.loft.games/tutorial");
    }

    private void OnVideoLinkClick(object? sender, PointerPressedEventArgs e)
    {
        OpenUrl("https://www.bilibili.com/video/BV1DdK66UECG");
    }

    private void OnWebsiteLinkClick(object? sender, PointerPressedEventArgs e)
    {
        OpenUrl("https://mct.loft.games/");
    }

    private void OnQQGroupLinkClick(object? sender, PointerPressedEventArgs e)
    {
        OpenUrl("https://qm.qq.com/q/gicBD965gI");
    }

    private void OnImageGuideClick(object? sender, RoutedEventArgs e)
    {
        OpenUrl("https://github.com/MCZLF/MinecraftConnectTool");
    }

    private void OnBugReportClick(object? sender, RoutedEventArgs e)
    {
        OpenUrl("https://qm.qq.com/q/8NAoszhKqk");
    }

    private async void OnForceKillCoreClick(object? sender, RoutedEventArgs e)
    {
        var killedCount = 0;
        var failedCount = 0;

        try
        {
            Server_Post.Stop_Post();
        }
        catch
        {
        }

        try
        {
            global::MinecraftConnectTool.Server_Post.Stop_Post();
        }
        catch
        {
        }

        KillProcessesByName("main", ref killedCount, ref failedCount, true);
        KillProcessesByName("easytier-core", ref killedCount, ref failedCount, true);
        KillProcessesByName("link", ref killedCount, ref failedCount, true);

        P2PStateService.SetRunning(false);

        await ShowForceKillCoreMessageAsync(killedCount, failedCount);
    }

    private async void OnRemoteControlClick(object? sender, RoutedEventArgs e)
    {
        if (!IsRemoteControlSupported()) return;

        _isRemoteControlRunning = IsRemoteControlProcessRunning();
        SetRemoteControlButtonEnabled(false);
        SetRemoteControlButtonState(_isRemoteControlRunning, _isRemoteControlRunning ? RemoteControlStoppingText : RemoteControlLoadingText);

        try
        {
            if (_isRemoteControlRunning)
            {
                await StopRemoteControlAsync();
                _isRemoteControlRunning = false;
                await ShowRemoteControlMessageAsync("已尝试关闭，但由于权限问题，如果未能够关闭请在任务栏托盘处关闭", false);
                return;
            }

            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                await ExtensionUI.MD3MessageDialog.ShowAsync(
                    desktop.MainWindow,
                    "请谨慎确认协助方身份，切勿随意发送给未知人员\n请将弹出的连接码和验证码截图发送给可信的管理员或开发者\n使用后请在托盘内退出，请勿保留后台",
                    "注意事项",
                    MaterialIconKind.AlertCircle);
            }

            var remoteControlFile = GetRemoteControlFile();
            Directory.CreateDirectory(Path.GetDirectoryName(remoteControlFile.Path)!);

            if (NeedsDownload(remoteControlFile.Path, remoteControlFile.ExpectedMd5))
            {
                ShowDownloadProgress();
                var progress = new Progress<double?>(UpdateDownloadProgress);
                await DownloadFileAsync(remoteControlFile.Url, remoteControlFile.Path, progress);
            }

            if (File.Exists(remoteControlFile.Path))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = remoteControlFile.Path,
                    UseShellExecute = true
                });
                _isRemoteControlRunning = true;
            }
        }
        catch (Exception ex)
        {
            await ShowRemoteControlMessageAsync($"发生错误：{ex.Message}", true);
        }
        finally
        {
            HideDownloadProgress();
            SetRemoteControlButtonEnabled(IsRemoteControlSupported());
            SetRemoteControlButtonState(_isRemoteControlRunning);
        }
    }

    private void ConfigureRemoteControlButton()
    {
        _isRemoteControlRunning = IsRemoteControlProcessRunning();
        HideDownloadProgress();
        SetRemoteControlButtonEnabled(IsRemoteControlSupported());
        SetRemoteControlButtonState(_isRemoteControlRunning);
    }

    private void SetRemoteControlButtonEnabled(bool isEnabled)
    {
        var button = this.FindControl<Button>("RemoteControlButton");
        if (button != null)
        {
            button.IsEnabled = isEnabled;
        }
    }

    private void SetRemoteControlButtonState(bool isRunning, string? text = null)
    {
        var buttonText = this.FindControl<TextBlock>("RemoteControlButtonText");
        if (buttonText != null)
        {
            buttonText.Text = text ?? (isRunning ? RemoteControlStopText : RemoteControlStartText);
        }

        var buttonIcon = this.FindControl<MaterialIcon>("RemoteControlButtonIcon");
        if (buttonIcon != null)
        {
            buttonIcon.Kind = isRunning ? MaterialIconKind.StopCircle : MaterialIconKind.PlayCircle;
        }
    }

    private static Task StopRemoteControlAsync()
    {
        return Task.Run(() =>
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/C taskkill /IM SunLogin32.exe /F & taskkill /IM SunLogin64.exe /F",
                Verb = "runas",
                CreateNoWindow = true,
                UseShellExecute = true
            };
            Process.Start(processStartInfo);
        });
    }

    private static async Task ShowRemoteControlMessageAsync(string message, bool isError)
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            if (isError)
            {
                await ExtensionUI.MD3MessageDialog.ShowErrorAsync(desktop.MainWindow, message, "错误");
            }
            else
            {
                await ExtensionUI.MD3MessageDialog.ShowInfoAsync(desktop.MainWindow, message, "提示");
            }
        }
    }

    private static void KillProcessesByName(string processName, ref int killedCount, ref int failedCount, bool killEntireProcessTree = false)
    {
        foreach (var process in Process.GetProcessesByName(processName))
        {
            try
            {
                process.Kill(killEntireProcessTree);
                process.WaitForExit();
                killedCount++;
            }
            catch
            {
                failedCount++;
            }
            finally
            {
                process.Dispose();
            }
        }
    }

    private static async Task ShowForceKillCoreMessageAsync(int killedCount, int failedCount)
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow == null)
        {
            return;
        }

        if (failedCount > 0)
        {
            await ExtensionUI.MD3MessageDialog.ShowErrorAsync(desktop.MainWindow, $"已终止 {killedCount} 个核心进程，{failedCount} 个进程终止失败", "强制终止核心");
            return;
        }

        await ExtensionUI.MD3MessageDialog.ShowInfoAsync(desktop.MainWindow, killedCount > 0 ? $"已终止 {killedCount} 个核心进程" : "未发现正在运行的核心进程", "强制终止核心");
    }

    private static bool IsRemoteControlSupported()
    {
        return OperatingSystem.IsWindows() &&
            (RuntimeInformation.ProcessArchitecture == Architecture.X86 || RuntimeInformation.ProcessArchitecture == Architecture.X64);
    }

    private static (string Url, string ExpectedMd5, string Path) GetRemoteControlFile()
    {
        if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
        {
            return (
                "https://api.mct.mczlf.loft.games/Sunlogin/SC64.exe",
                "e31cb2f51ebbcc98ca9f51645727eb00",
                LocalStorageService.GetTempFilePath("SunLogin64.exe"));
        }

        return (
            "https://api.mct.mczlf.loft.games/SC32.exe",
            "55726ad06d8ad4484210345b195d285a",
            LocalStorageService.GetTempFilePath("SunLogin32.exe"));
    }

    private static bool NeedsDownload(string path, string expectedMd5)
    {
        return !File.Exists(path) || !string.Equals(GetFileMd5Hash(path), expectedMd5, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task DownloadFileAsync(string url, string destinationPath, IProgress<double?> progress)
    {
        using var response = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;
        long downloadedBytes = 0;

        await using var httpStream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
        byte[] buffer = new byte[8192];
        int bytesRead;

        while ((bytesRead = await httpStream.ReadAsync(buffer)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
            downloadedBytes += bytesRead;

            if (totalBytes > 0)
            {
                progress.Report(downloadedBytes * 100d / totalBytes.Value);
            }
            else
            {
                progress.Report(null);
            }
        }

        progress.Report(100);
    }

    private void ShowDownloadProgress()
    {
        var border = this.FindControl<Border>("RemoteControlDownloadBorder");
        if (border != null)
        {
            border.IsVisible = true;
        }

        UpdateDownloadProgress(0);
    }

    private void HideDownloadProgress()
    {
        var border = this.FindControl<Border>("RemoteControlDownloadBorder");
        if (border != null)
        {
            border.IsVisible = false;
        }

        UpdateDownloadProgress(0);
    }

    private void UpdateDownloadProgress(double? progress)
    {
        var progressBar = this.FindControl<ProgressBar>("RemoteControlDownloadProgressBar");
        var percentText = this.FindControl<TextBlock>("RemoteControlDownloadPercentText");
        var downloadText = this.FindControl<TextBlock>("RemoteControlDownloadText");

        if (progressBar != null)
        {
            progressBar.IsIndeterminate = !progress.HasValue;
            progressBar.Value = Math.Clamp(progress ?? 0, 0, 100);
        }

        if (percentText != null)
        {
            percentText.Text = progress.HasValue ? $"{Math.Clamp(progress.Value, 0, 100):0}%" : "计算中";
        }

        if (downloadText != null)
        {
            downloadText.Text = "正在下载远程控制组件...";
        }
    }

    private static bool IsRemoteControlProcessRunning()
    {
        return Process.GetProcessesByName("SunLogin32").Length > 0 ||
            Process.GetProcessesByName("SunLogin64").Length > 0;
    }

    private static string GetFileMd5Hash(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            byte[] hash = MD5.HashData(stream);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
        catch
        {
            return string.Empty;
        }
    }

    private void OnRestartClick(object? sender, RoutedEventArgs e)
    {
        var exePath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(exePath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true
            });
        }
        Environment.Exit(0);
    }

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"打开链接失败: {ex.Message}");
        }
    }
}
