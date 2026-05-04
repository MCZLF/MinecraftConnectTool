using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinecraftConnectTool.Services;
using MinecraftConnectTool.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftConnectTool.ViewModels.Pages;

/// <summary>
/// 设置页面ViewModel - 基于原WinForm配置逻辑
/// </summary>
public partial class SettingsPageViewModel : ViewModelBase
{
    #region 外观设置
    
    [ObservableProperty]
    private bool _isDarkMode;

    [ObservableProperty]
    private string _language = "简体中文";

    /// <summary>
    /// 启动时不显示欢迎通知
    /// </summary>
    [ObservableProperty]
    private bool _noNotifyWhenStart;

    /// <summary>
    /// 启用彩色主题
    /// </summary>
    [ObservableProperty]
    private bool _enableColor;

    /// <summary>
    /// 主题色（强调色）
    /// </summary>
    [ObservableProperty]
    private Color _accentColor = Color.Parse("#6750A4");

    /// <summary>
    /// 混色浓度 (0.01 - 1.00)
    /// </summary>
    [ObservableProperty]
    private double _mixIntensity = 0.15;

    /// <summary>
    /// 是否显示主题色选择器
    /// </summary>
    [ObservableProperty]
    private bool _isColorPickerVisible;

    /// <summary>
    /// 模拟 Fluent Design (Win11 风格)
    /// </summary>
    [ObservableProperty]
    private bool _simulateFluentDesign;

    /// <summary>
    /// 启用照片背景
    /// </summary>
    [ObservableProperty]
    private bool _enablePhotoBackground;

    /// <summary>
    /// 照片背景图片路径
    /// </summary>
    [ObservableProperty]
    private string? _photoBackgroundPath;

    /// <summary>
    /// 是否显示照片背景控制区域
    /// </summary>
    [ObservableProperty]
    private bool _isPhotoBackgroundControlsVisible;

    /// <summary>
    /// 背景透明度 (0.1 - 0.9)
    /// </summary>
    [ObservableProperty]
    private double _backgroundOpacity = 0.30;

    /// <summary>
    /// 控件区域不透明度 (0.0 - 0.8)
    /// </summary>
    [ObservableProperty]
    private double _controlOpacity = 0.30;

    #endregion

    #region 启动设置

    /// <summary>
    /// 启动时检查更新
    /// </summary>
    [ObservableProperty]
    private bool _goUpdateWhenStart;

    /// <summary>
    /// 启动时检查版本
    /// </summary>
    [ObservableProperty]
    private bool _enableVersionCheck = true;

    /// <summary>
    /// 自动检查P2P IF开启状态
    /// </summary>
    [ObservableProperty]
    private bool _autoCheckP2PIFOpen = true;

    /// <summary>
    /// 动画速度
    /// </summary>
    [ObservableProperty]
    private AnimationSpeed _animationSpeed = AnimationSpeed.Medium;

    /// <summary>
    /// 自定义动画时长（毫秒）
    /// </summary>
    [ObservableProperty]
    private double _customAnimationDuration = 200;

    /// <summary>
    /// 是否显示自定义时长输入框
    /// </summary>
    [ObservableProperty]
    private bool _isCustomAnimationDurationVisible;

    /// <summary>
    /// 自定义动画时长输入文本
    /// </summary>
    [ObservableProperty]
    private string _customAnimationDurationText = "200";

    /// <summary>
    /// 是否显示自定义时长确认按钮（当输入值与当前值不同时显示）
    /// </summary>
    [ObservableProperty]
    private bool _isCustomDurationConfirmVisible;

    /// <summary>
    /// 临时存储的自定义时长输入值
    /// </summary>
    private string _pendingCustomDurationText = "200";

    /// <summary>
    /// 渲染方式
    /// </summary>
    [ObservableProperty]
    private RenderingMode _renderingMode = RenderingMode.SystemDefault;

    #endregion

    #region 功能设置

    /// <summary>
    /// 启用Relay中转
    /// </summary>
    [ObservableProperty]
    private bool _enableRelay;

    /// <summary>
    /// 启用服务器Post
    /// </summary>
    [ObservableProperty]
    private bool _serverPostEnable = true;

    /// <summary>
    /// 启用TCP模式
    /// </summary>
    [ObservableProperty]
    private bool _enableTCP = true;

    /// <summary>
    /// 启用数据节省模式
    /// </summary>
    [ObservableProperty]
    private bool _dataSaving;

    /// <summary>
    /// 启用OLAN兼容模式
    /// </summary>
    [ObservableProperty]
    private bool _enableOLAN;

    /// <summary>
    /// 启用TL模式
    /// </summary>
    [ObservableProperty]
    private bool _enableTL;

    /// <summary>
    /// 启用DST模式
    /// </summary>
    [ObservableProperty]
    private bool _enableDST;

    /// <summary>
    /// 启用性能模式（内存优化）
    /// </summary>
    [ObservableProperty]
    private bool _enablePerformanceMode;

    /// <summary>
    /// 使用GitHub镜像源
    /// </summary>
    [ObservableProperty]
    private bool _useGitHubMirror;

    /// <summary>
    /// GitHub镜像源类型 (cloudflare, hk, fastly)
    /// </summary>
    [ObservableProperty]
    private string _gitHubMirrorType = "fastly";

    /// <summary>
    /// 允许Probe探针上报
    /// </summary>
    [ObservableProperty]
    private bool _allowProbe = true;

    /// <summary>
    /// 是否显示镜像源选择器
    /// </summary>
    [ObservableProperty]
    private bool _isMirrorTypeSelectorVisible;

    /// <summary>
    /// 可用的镜像源类型列表
    /// </summary>
    public List<string> GitHubMirrorTypes { get; } = new() { "cloudflare", "hk", "fastly" };

    /// <summary>
    /// 动画速度选项列表
    /// </summary>
    public List<AnimationSpeedOption> AnimationSpeedOptions { get; } = new()
    {
        new AnimationSpeedOption(AnimationSpeed.Fast, "快速", "100ms"),
        new AnimationSpeedOption(AnimationSpeed.Medium, "适中", "300ms"),
        new AnimationSpeedOption(AnimationSpeed.Slow, "慢速", "500ms"),
        new AnimationSpeedOption(AnimationSpeed.Custom, "自定义", "自定义")
    };

    /// <summary>
    /// 接收测试更新 (getpreupdate) - Windows平台专用
    /// </summary>
    [ObservableProperty]
    private bool _getPreUpdate;

    /// <summary>
    /// 是否支持测试更新（仅Windows平台支持）
    /// </summary>
    [ObservableProperty]
    private bool _isPreviewUpdateSupported;

    /// <summary>
    /// 显示P2P调试信息
    /// </summary>
    [ObservableProperty]
    private bool _showP2PBug;

    /// <summary>
    /// 自定义提示码更新时间 (1=每次, 2=每小时, 3=每天, 4=永久)
    /// </summary>
    [ObservableProperty]
    private int _codeUpdate = 1;

    /// <summary>
    /// 使用自定义提示码
    /// </summary>
    [ObservableProperty]
    private bool _useCustomNode;

    /// <summary>
    /// 自定义提示码内容
    /// </summary>
    [ObservableProperty]
    private string _customNode = "";

    /// <summary>
    /// 使用自定义端口
    /// </summary>
    [ObservableProperty]
    private bool _useCustomPort;

    /// <summary>
    /// 自定义端口值
    /// </summary>
    [ObservableProperty]
    private string _customPort = "None";

    /// <summary>
    /// Relay服务器地址
    /// </summary>
    [ObservableProperty]
    private string _relayServer = "None";

    #endregion

    #region 用户信息

    /// <summary>
    /// 用户名/游戏ID
    /// </summary>
    [ObservableProperty]
    private string _username = "";

    /// <summary>
    /// 是否显示用户设置区域（当Username不为空时显示）
    /// </summary>
    [ObservableProperty]
    private bool _isUserSettingsVisible;

    #endregion

    #region 关于信息

    [ObservableProperty]
    private string _versionText = $"版本 {MainWindow.version}";

    [ObservableProperty]
    private string _designation = MainWindow.designation;

    #endregion

    #region 进度状态

    /// <summary>
    /// 网络优化是否进行中
    /// </summary>
    [ObservableProperty]
    private bool _isNetworkOptimizing;

    /// <summary>
    /// 网络优化进度 (0-100)
    /// </summary>
    [ObservableProperty]
    private int _networkOptimizationProgress;

    #endregion

    #region Toast 通知

    [ObservableProperty]
    private bool _isToastVisible = false;

    [ObservableProperty]
    private string _toastMessage = "";

    [ObservableProperty]
    private string _toastIconKind = "AlertCircle";

    [ObservableProperty]
    private string _toastBackground = "{DynamicResource MaterialErrorContainerBrush}";

    [ObservableProperty]
    private string _toastForeground = "{DynamicResource MaterialOnErrorContainerBrush}";

    [ObservableProperty]
    private string _toastIconColor = "#F44336";

    [ObservableProperty]
    private double _toastOpacity = 0;

    private System.Threading.CancellationTokenSource? _toastCancellationTokenSource;

    #endregion

    #region 初始化标志

    /// <summary>
    /// 是否正在加载设置（避免加载时触发通知）
    /// </summary>
    private bool _isLoadingSettings = false;

    #endregion

    public SettingsPageViewModel()
    {
        // 检测平台支持情况
        IsPreviewUpdateSupported = OperatingSystem.IsWindows();
        
        LoadSettings();
        
        // 非Windows平台强制关闭测试更新
        if (!IsPreviewUpdateSupported)
        {
            GetPreUpdate = false;
            ConfigService.Write("getpreupdate", false);
        }
    }

    /// <summary>
    /// 从配置文件加载设置
    /// </summary>
    private void LoadSettings()
    {
        // 设置加载标志，避免触发通知
        _isLoadingSettings = true;
        
        // 外观设置
        IsDarkMode = ThemeService.Instance.IsDarkMode;
        NoNotifyWhenStart = ConfigService.Read<bool>("nonotifywhenstart", false);
        EnableColor = ThemeService.Instance.EnableColorMode;
        IsColorPickerVisible = EnableColor;
        
        // 读取主题色
        var accentColorHex = ConfigService.Read<string>("AccentColor", "#6750A4");
        if (Color.TryParse(accentColorHex, out var parsedColor))
        {
            AccentColor = parsedColor;
        }
        
        // 读取混色浓度
        MixIntensity = ConfigService.Read<double>("MixIntensity", 0.15);
        
        SimulateFluentDesign = ThemeService.Instance.SimulateFluentDesign;

        // 照片背景设置
        EnablePhotoBackground = ConfigService.Read<bool>("EnablePhotoBackground", false);
        PhotoBackgroundPath = ConfigService.Read<string?>("PhotoBackgroundPath", null);
        IsPhotoBackgroundControlsVisible = EnablePhotoBackground;
        BackgroundOpacity = ConfigService.Read<double>("BackgroundOpacity", 0.30);
        ControlOpacity = ConfigService.Read<double>("ControlOpacity", 0.30);

        // 启动设置
        GoUpdateWhenStart = ConfigService.Read<bool>("goupdatewhenstart", false);
        EnableVersionCheck = ConfigService.Read<bool>("EnableVersionCheck", true);
        AutoCheckP2PIFOpen = ConfigService.Read<bool>("AutoCheckP2PIFOpen", true);

        // 动画速度设置
        AnimationSpeed = ThemeService.Instance.AnimationSpeed;
        IsCustomAnimationDurationVisible = AnimationSpeed == AnimationSpeed.Custom;
        CustomAnimationDuration = ThemeService.Instance.CustomAnimationDuration;
        CustomAnimationDurationText = CustomAnimationDuration.ToString("F0");

        // 渲染方式
        RenderingMode = ThemeService.Instance.RenderingMode;

        // 功能设置 - Relay强制关闭
        EnableRelay = false;
        ConfigService.Write("EnableRelay", false);
        ServerPostEnable = ConfigService.Read<bool>("ServerPostEnable", true);
        EnableTCP = ConfigService.Read<bool>("TCP", true);
        DataSaving = ConfigService.Read<bool>("datasaving", false);
        EnableOLAN = ConfigService.Read<bool>("EnableOLAN", false);
        EnableTL = ConfigService.Read<bool>("EnableTL", false);
        EnableDST = ConfigService.Read<bool>("EnableDST", false);
        GetPreUpdate = ConfigService.Read<bool>("getpreupdate", false);
        ShowP2PBug = ConfigService.Read<bool>("ShowP2PBug", false);
        EnablePerformanceMode = ConfigService.Read<bool>("EnablePerformanceMode", false);
        // [已禁用] GitHub镜像源功能已弃用，强制设置为false
        UseGitHubMirror = false;
        ConfigService.Write("githubmirror", false);
        // GitHubMirrorType = ConfigService.Read<string>("githubmirrortype", "fastly");
        IsMirrorTypeSelectorVisible = false;
        AllowProbe = ConfigService.Read<bool>("AllowProbe", true);

        // 自定义设置
        CodeUpdate = ConfigService.Read<int>("codeupdate", 1);
        UseCustomNode = ConfigService.Read<bool>("usecustomnode", false);
        CustomNode = ConfigService.Read<string>("customnode", "");
        UseCustomPort = ConfigService.Read<bool>("usecustomport", false);
        CustomPort = ConfigService.Read<string>("customport", "None");
        RelayServer = ConfigService.Read<string>("Server", "None");

        // 用户设置
        Username = ConfigService.Read<string>("Username", "");
        IsUserSettingsVisible = !string.IsNullOrWhiteSpace(Username);

        // 重置加载标志
        _isLoadingSettings = false;
    }

    #region 属性变更处理

    partial void OnIsDarkModeChanged(bool value)
    {
        ThemeService.Instance.IsDarkMode = value;
    }

    partial void OnNoNotifyWhenStartChanged(bool value)
    {
        ConfigService.Write("nonotifywhenstart", value);
    }

    partial void OnEnableColorChanged(bool value)
    {
        ThemeService.Instance.EnableColorMode = value;
        IsColorPickerVisible = value;
    }

    partial void OnAccentColorChanged(Color value)
    {
        ThemeService.Instance.AccentColor = value;
    }

    partial void OnMixIntensityChanged(double value)
    {
        ThemeService.Instance.MixIntensity = value;
    }

    partial void OnSimulateFluentDesignChanged(bool value)
    {
        ThemeService.Instance.SimulateFluentDesign = value;
    }

    partial void OnEnablePhotoBackgroundChanged(bool value)
    {
        ThemeService.Instance.EnablePhotoBackground = value;
        IsPhotoBackgroundControlsVisible = value;
    }

    partial void OnPhotoBackgroundPathChanged(string? value)
    {
        ThemeService.Instance.PhotoBackgroundPath = value;
    }

    partial void OnBackgroundOpacityChanged(double value)
    {
        ThemeService.Instance.BackgroundOpacity = value;
    }

    partial void OnControlOpacityChanged(double value)
    {
        ThemeService.Instance.ControlOpacity = value;
    }

    partial void OnGoUpdateWhenStartChanged(bool value)
    {
        ConfigService.Write("goupdatewhenstart", value);
    }

    partial void OnEnableVersionCheckChanged(bool value)
    {
        ConfigService.Write("EnableVersionCheck", value);
    }

    partial void OnAutoCheckP2PIFOpenChanged(bool value)
    {
        ConfigService.Write("AutoCheckP2PIFOpen", value);
    }

    partial void OnServerPostEnableChanged(bool value)
    {
        ConfigService.Write("ServerPostEnable", value);
    }

    partial void OnEnableTCPChanged(bool value)
    {
        ConfigService.Write("TCP", value);
    }

    partial void OnDataSavingChanged(bool value)
    {
        ConfigService.Write("datasaving", value);
    }

    partial void OnEnableOLANChanged(bool value)
    {
        ConfigService.Write("EnableOLAN", value);
    }

    partial void OnEnableTLChanged(bool value)
    {
        ConfigService.Write("EnableTL", value);
    }

    partial void OnEnableDSTChanged(bool value)
    {
        ConfigService.Write("EnableDST", value);
    }

    partial void OnGetPreUpdateChanged(bool value)
    {
        // 非Windows平台禁止开启测试更新
        if (!IsPreviewUpdateSupported && value)
        {
            GetPreUpdate = false;
            ShowToast("测试更新仅在Windows平台可用");
            return;
        }
        ConfigService.Write("getpreupdate", value);
    }

    partial void OnShowP2PBugChanged(bool value)
    {
        ConfigService.Write("ShowP2PBug", value);
    }

    partial void OnEnablePerformanceModeChanged(bool value)
    {
        // 检查配置文件中之前的值
        bool oldValue = ConfigService.Read<bool>("EnablePerformanceMode", false);

        ConfigService.Write("EnablePerformanceMode", value);

        // 只有在非加载状态下，且值真正发生变化时才显示提示
        if (!_isLoadingSettings && oldValue != value)
        {
            // 显示提示，告知用户需要重启才能生效
            ShowToast(value ? "性能模式已开启，重启应用后生效" : "性能模式已关闭，重启应用后生效");
        }
    }

    partial void OnUseGitHubMirrorChanged(bool value)
    {
        // [已禁用] GitHub镜像源功能已弃用
        // ConfigService.Write("githubmirror", value);
        // IsMirrorTypeSelectorVisible = value;
    }

    partial void OnGitHubMirrorTypeChanged(string value)
    {
        // [已禁用] GitHub镜像源功能已弃用
        // ConfigService.Write("githubmirrortype", value);
    }

    partial void OnAllowProbeChanged(bool value)
    {
        if (_isLoadingSettings) return;

        if (!value)
        {
            // 用户选择关闭，显示警告提示
            ShowToast("Probe 探针已关闭，我们无法收集版本统计信息");
        }
        else
        {
            ShowToast("Probe 探针已开启，感谢您的支持");
        }

        ConfigService.Write("AllowProbe", value);
    }

    partial void OnCodeUpdateChanged(int value)
    {
        ConfigService.Write("codeupdate", value);
    }

    partial void OnUseCustomNodeChanged(bool value)
    {
        ConfigService.Write("usecustomnode", value);
    }

    partial void OnCustomNodeChanged(string value)
    {
        ConfigService.Write("customnode", value);
    }

    partial void OnUseCustomPortChanged(bool value)
    {
        ConfigService.Write("usecustomport", value);
    }

    partial void OnCustomPortChanged(string value)
    {
        ConfigService.Write("customport", value);
    }

    partial void OnRelayServerChanged(string value)
    {
        ConfigService.Write("Server", value);
    }

    #endregion

    #region 命令

    /// <summary>
    /// 选择照片背景图片
    /// </summary>
    [RelayCommand]
    private async Task SelectPhotoBackgroundAsync()
    {
        try
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                && desktop.MainWindow != null)
            {
                var options = new Avalonia.Platform.Storage.FilePickerOpenOptions
                {
                    Title = "选择背景图片",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new Avalonia.Platform.Storage.FilePickerFileType("图片文件")
                        {
                            Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.bmp", "*.gif", "*.webp" }
                        }
                    }
                };

                var result = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(options);
                if (result != null && result.Count > 0)
                {
                    string selectedPath = result[0].Path.LocalPath;
                    if (File.Exists(selectedPath))
                    {
                        PhotoBackgroundPath = selectedPath;
                        ShowToast("背景图片已设置", true);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowToast($"选择图片失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 清除照片背景
    /// </summary>
    [RelayCommand]
    private void ClearPhotoBackground()
    {
        PhotoBackgroundPath = null;
        ShowToast("背景图片已清除", true);
    }

    /// <summary>
    /// 打开自定义邀请信息设置抽屉
    /// </summary>
    [RelayCommand]
    private async Task OpenInviteEditDrawerAsync()
    {
        // 获取主窗口实例并显示Panel
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow is Views.MainWindow mainWindow)
            {
                await mainWindow.ShowPanelInviteEditAsync();
            }
        }
    }

    /// <summary>
    /// 清除缓存
    /// </summary>
    [RelayCommand]
    private void ClearCache()
    {
        try
        {
            string tempPath = Path.GetTempPath();
            string targetFolder = Path.Combine(tempPath, "MCZLFAPP");

            if (Directory.Exists(targetFolder))
            {
                foreach (string file in Directory.GetFiles(targetFolder, "*.*", SearchOption.AllDirectories))
                {
                    try
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"删除文件失败 {file}: {ex.Message}");
                    }
                }

                foreach (string folder in Directory.GetDirectories(targetFolder, "*.*", SearchOption.AllDirectories).OrderByDescending(d => d.Length))
                {
                    try
                    {
                        Directory.Delete(folder, true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"删除目录失败 {folder}: {ex.Message}");
                    }
                }

                // 显示成功 Toast
                ShowToast("清除成功~", true);

                // 延迟后重启
                _ = Task.Run(async () =>
                {
                    await Task.Delay(1500);
                    RestartApplication();
                });
            }
            else
            {
                ShowToast("缓存为空，无需清理");
            }
        }
        catch (Exception ex)
        {
            ShowToast($"清除失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 重置配置文件
    /// </summary>
    [RelayCommand]
    private void ResetConfig()
    {
        try
        {
            string tempPath = Path.GetTempPath();
            
            // 使用 ConfigService 获取配置文件路径，确保路径一致性
            string configFilePath = ConfigService.GetConfigFilePath();
            if (File.Exists(configFilePath))
            {
                try
                {
                    File.SetAttributes(configFilePath, FileAttributes.Normal);
                    File.Delete(configFilePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"删除配置文件失败 {configFilePath}: {ex.Message}");
                }
            }
            
            // 删除theme.json
            string targetThemePath = Path.Combine(tempPath, "MCZLFAPP", "theme.json");
            if (File.Exists(targetThemePath))
            {
                try
                {
                    File.SetAttributes(targetThemePath, FileAttributes.Normal);
                    File.Delete(targetThemePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"删除主题文件失败 {targetThemePath}: {ex.Message}");
                }
            }
            
            // 显示成功 Toast
            ShowToast("重置成功~", true);
            
            // 延迟重启，让用户看到Toast
            Task.Delay(1500).ContinueWith(_ =>
            {
                // 重启应用
                RestartApplication();
            });
        }
        catch (Exception ex)
        {
            ShowToast($"重置失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 添加到Windows Defender排除列表
    /// </summary>
    [RelayCommand]
    private void AddToDefenderExclusion()
    {
        try
        {
            // 仅在Windows平台执行
            if (!OperatingSystem.IsWindows())
            {
                ShowToast("此功能仅在Windows平台可用");
                return;
            }

            string tempPath = Path.GetTempPath();
            string folderPath = Path.Combine(tempPath, "MCZLFAPP");
            string command = $"Add-MpPreference -ExclusionPath \"{folderPath}\"";

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
                Verb = "runas",
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
            };
            System.Diagnostics.Process.Start(psi);

            // 显示成功 Toast
            ShowToast("已成功加入白名单", true);
        }
        catch (Exception ex)
        {
            ShowToast($"发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 网络优化 - 下载并运行360DNS
    /// </summary>
    [RelayCommand]
    private async Task NetworkOptimization()
    {
        IsNetworkOptimizing = true;
        NetworkOptimizationProgress = 0;

        try
        {
            string url = "https://api.mct.mczlf.loft.games/360DNS.exe";
            string tempDir = Path.Combine(Path.GetTempPath(), "MCZLFAPP", "Temp");
            string destinationPath = Path.Combine(tempDir, "360DNS.exe");
            string expectedMd5 = "a0c67c45b118e9706cadb771b3014528";
            bool needsDownload = false;

            // 确保目标文件夹存在
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }

            // 检查文件是否存在且MD5正确
            if (File.Exists(destinationPath))
            {
                string md5Hash = GetFileMD5Hash(destinationPath);
                if (md5Hash != expectedMd5)
                {
                    needsDownload = true;
                }
            }
            else
            {
                needsDownload = true;
            }

            // 下载文件
            if (needsDownload)
            {
                using var client = new System.Net.Http.HttpClient();
                using var response = await client.GetAsync(url, System.Net.Http.HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                long totalBytes = response.Content.Headers.ContentLength ?? 0;
                long downloadedBytes = 0;

                using var httpStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.Write, bufferSize: 8192, useAsync: true);
                byte[] buffer = new byte[8192];
                int bytesRead;

                while ((bytesRead = await httpStream.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                    downloadedBytes += bytesRead;
                    if (totalBytes > 0)
                    {
                        NetworkOptimizationProgress = (int)((downloadedBytes * 100) / totalBytes);
                    }
                }
            }

            NetworkOptimizationProgress = 100;

            // 启动程序
            if (File.Exists(destinationPath))
            {
                var processStartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = destinationPath,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(processStartInfo);
            }
        }
        catch (Exception)
        {
            // TODO: 显示错误消息
        }
        finally
        {
            IsNetworkOptimizing = false;
        }
    }

    /// <summary>
    /// 计算文件MD5
    /// </summary>
    private static string GetFileMD5Hash(string filePath)
    {
        try
        {
            using FileStream stream = File.OpenRead(filePath);
            using var md5 = System.Security.Cryptography.MD5.Create();
            byte[] hashValue = md5.ComputeHash(stream);
            StringBuilder hex = new(hashValue.Length * 2);
            foreach (byte b in hashValue)
            {
                hex.AppendFormat("{0:x2}", b);
            }
            return hex.ToString();
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    /// 重启应用程序
    /// </summary>
    [RelayCommand]
    private static void RestartApplication()
    {
        // 启动新进程
        var processStartInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "",
            UseShellExecute = true
        };
        System.Diagnostics.Process.Start(processStartInfo);

        // 关闭当前应用程序
        System.Environment.Exit(0);
    }

    /// <summary>
    /// 显示 Toast 通知 - 类似安卓Toast，带淡入淡出动画
    /// </summary>
    public async void ShowToast(string message, bool isSuccess = false)
    {
        // 取消之前的自动隐藏任务
        _toastCancellationTokenSource?.Cancel();
        _toastCancellationTokenSource = new System.Threading.CancellationTokenSource();
        var token = _toastCancellationTokenSource.Token;

        // 根据类型设置样式
        if (isSuccess)
        {
            ToastIconKind = "CheckCircle";
            ToastIconColor = "#4CAF50";  // 绿色，成功
        }
        else
        {
            ToastIconKind = "AlertCircle";
            ToastIconColor = "#F44336";  // 红色，错误
        }

        // 设置文本并显示
        ToastMessage = message;
        IsToastVisible = true;

        // 淡入动画 - 30ms间隔，从0到1，共300ms
        for (double i = 0; i <= 1; i += 0.1)
        {
            if (token.IsCancellationRequested) return;
            ToastOpacity = i;
            await Task.Delay(30, token);
        }
        ToastOpacity = 1;

        try
        {
            // 显示2.4秒后淡出（总共约3秒：0.3s淡入 + 2.4s显示 + 0.3s淡出）
            await Task.Delay(2400, token);

            // 淡出动画 - 30ms间隔，从1到0，共300ms
            for (double i = 1; i >= 0; i -= 0.1)
            {
                if (token.IsCancellationRequested) return;
                ToastOpacity = i;
                await Task.Delay(30, token);
            }
            ToastOpacity = 0;
            IsToastVisible = false;
        }
        catch (TaskCanceledException)
        {
            // 任务被取消，淡出隐藏
            for (double i = ToastOpacity; i >= 0; i -= 0.1)
            {
                ToastOpacity = i;
                await Task.Delay(30);
            }
            ToastOpacity = 0;
            IsToastVisible = false;
        }
    }

    /// <summary>
    /// 打开 GitHub 仓库页面
    /// </summary>
    [RelayCommand]
    private void OpenGitHub()
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/MCZLF/MinecraftConnectTool",
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch (Exception ex)
        {
            ShowToast($"无法打开链接: {ex.Message}");
        }
    }

    /// <summary>
    /// 打开邮件客户端
    /// </summary>
    [RelayCommand]
    private void OpenEmail()
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "mailto:mczlf@qq.com",
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch (Exception ex)
        {
            ShowToast($"无法打开邮件: {ex.Message}");
        }
    }

    /// <summary>
    /// 修改用户名
    /// </summary>
    [RelayCommand]
    private async Task ChangeUsername()
    {
        try
        {
            var mainWindow = Avalonia.Application.Current?.ApplicationLifetime
                is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow : null;

            if (mainWindow != null)
            {
                var newUsername = await ExtensionUI.NicknameInputDialog.ShowAsync(mainWindow);
                if (!string.IsNullOrWhiteSpace(newUsername))
                {
                    Username = newUsername;
                    ConfigService.Write("Username", newUsername);
                    ShowToast("用户名已更新");
                }
            }
        }
        catch (Exception ex)
        {
            ShowToast($"修改用户名失败: {ex.Message}");
        }
    }

    #endregion

    #region 动画速度设置

    partial void OnAnimationSpeedChanged(AnimationSpeed value)
    {
        ThemeService.Instance.AnimationSpeed = value;
        IsCustomAnimationDurationVisible = value == AnimationSpeed.Custom;
        if (value == AnimationSpeed.Custom)
        {
            CustomAnimationDurationText = CustomAnimationDuration.ToString("F0");
        }
    }

    partial void OnCustomAnimationDurationChanged(double value)
    {
        ThemeService.Instance.CustomAnimationDuration = value;
        // 同步文本值，但不触发通知
        if (CustomAnimationDurationText != value.ToString("F0"))
        {
            _pendingCustomDurationText = value.ToString("F0");
            CustomAnimationDurationText = _pendingCustomDurationText;
        }
        // 隐藏确认按钮
        IsCustomDurationConfirmVisible = false;
    }

    partial void OnRenderingModeChanged(RenderingMode value)
    {
        ThemeService.Instance.RenderingMode = value;
        // 只有在非加载状态下才显示 Toast（避免初始化时弹出）
        if (!_isLoadingSettings)
        {
            ShowToast($"渲染方式已切换为 {GetRenderingModeDisplayName(value)}，重启后生效", true);
        }
    }

    private string GetRenderingModeDisplayName(RenderingMode mode) => mode switch
    {
        RenderingMode.SystemDefault => "系统默认",
        RenderingMode.Gpu => "GPU 渲染",
        RenderingMode.Cpu => "CPU 渲染",
        _ => mode.ToString()
    };

    partial void OnCustomAnimationDurationTextChanged(string value)
    {
        // 存储待确认的输入值
        _pendingCustomDurationText = value;
        
        // 检查是否需要显示确认按钮
        if (double.TryParse(value, out var inputDuration))
        {
            // 边界处理：0（关闭动画）或 50-2000，<0 设为 0，0-50 设为 50，>2000 设为 200
            double clampedDuration;
            if (inputDuration <= 0)
            {
                clampedDuration = 0;
            }
            else if (inputDuration < 50)
            {
                clampedDuration = 50;
            }
            else if (inputDuration > 2000)
            {
                clampedDuration = 200;
            }
            else
            {
                clampedDuration = inputDuration;
            }
            
            // 只有当输入值与当前值不同时显示确认按钮
            IsCustomDurationConfirmVisible = Math.Abs(clampedDuration - CustomAnimationDuration) > 0.01;
        }
        else
        {
            // 输入无效时隐藏确认按钮
            IsCustomDurationConfirmVisible = false;
        }
    }

    /// <summary>
    /// 设置动画速度
    /// </summary>
    [RelayCommand]
    private void SetAnimationSpeed(AnimationSpeed speed)
    {
        AnimationSpeed = speed;
        var speedName = speed switch
        {
            AnimationSpeed.Fast => "快速",
            AnimationSpeed.Medium => "适中",
            AnimationSpeed.Slow => "慢速",
            AnimationSpeed.Custom => "自定义",
            _ => "未知"
        };
        if (speed == AnimationSpeed.Custom)
        {
            ShowToast($"动画速度已切换为 {speedName} ({CustomAnimationDuration:F0}ms)", true);
        }
        else
        {
            ShowToast($"动画速度已切换为 {speedName}", true);
        }
    }

    /// <summary>
    /// 确认自定义动画时长
    /// </summary>
    [RelayCommand]
    private void ConfirmCustomAnimationDuration()
    {
        if (double.TryParse(_pendingCustomDurationText, out var duration))
        {
            // 边界处理：0（关闭动画）或 50-2000，<0 设为 0，0-50 设为 50，>2000 设为 200
            if (duration <= 0)
            {
                duration = 0;
            }
            else if (duration < 50)
            {
                duration = 50;
            }
            else if (duration > 2000)
            {
                duration = 200;
            }
            
            CustomAnimationDuration = duration;
            if (duration == 0)
            {
                ShowToast("动画已关闭", true);
            }
            else
            {
                ShowToast($"自定义动画时长已设置为 {duration:F0}ms", true);
            }
        }
        IsCustomDurationConfirmVisible = false;
    }

    /// <summary>
    /// 设置渲染方式
    /// </summary>
    [RelayCommand]
    private void SetRenderingMode(RenderingMode mode)
    {
        RenderingMode = mode;
    }

    #endregion
}

/// <summary>
/// 动画速度选项
/// </summary>
public class AnimationSpeedOption
{
    public AnimationSpeed Value { get; }
    public string DisplayName { get; }
    public string DurationText { get; }

    public AnimationSpeedOption(AnimationSpeed value, string displayName, string durationText)
    {
        Value = value;
        DisplayName = displayName;
        DurationText = durationText;
    }

    public override string ToString() => DisplayName;
}
