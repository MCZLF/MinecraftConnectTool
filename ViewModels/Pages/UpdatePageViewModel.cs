using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinecraftConnectTool.Services;
using MinecraftConnectTool.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftConnectTool.ViewModels.Pages;

public partial class UpdatePageViewModel : ViewModelBase
{
    #region 跨平台更新API配置

    // ==================== 新API基础URL ====================
    private const string Api_Base_Url = "https://api.mct.mczlf.loft.games/007";

    // ==================== Windows 平台 ====================
    // Windows x86
    private const string Windows_X86_Version_Api = $"{Api_Base_Url}/Win_X86/version";      // Windows x86 版本检测
    private const string Windows_X86_Download_Api = $"{Api_Base_Url}/Win_X86/Latest.zip";     // Windows x86 下载地址 (ZIP格式)

    // Windows x64 (AMD64) - 默认回退平台
    private const string Windows_X64_Version_Api = $"{Api_Base_Url}/Win_X64/version";
    private const string Windows_X64_Download_Api = $"{Api_Base_Url}/Win_X64/Latest.zip";

    // Windows ARM64
    private const string Windows_Arm64_Version_Api = $"{Api_Base_Url}/Win_Arm64/version";    // Windows ARM64 版本检测
    private const string Windows_Arm64_Download_Api = $"{Api_Base_Url}/Win_Arm64/Latest.zip";   // Windows ARM64 下载地址 (ZIP格式)

    // ==================== Linux 平台 ====================
    // Linux x64
    private const string Linux_X64_Version_Api = $"{Api_Base_Url}/Linux_X64/version";        // Linux x64 版本检测
    private const string Linux_X64_Download_Api = $"{Api_Base_Url}/Linux_X64/Latest.zip";       // Linux x64 下载地址 (ZIP格式)

    // Linux ARM //暂不支持
    private const string Linux_Arm_Version_Api = null;        // Linux ARM 版本检测
    private const string Linux_Arm_Download_Api = null;       // Linux ARM 下载地址

    // Linux ARM64
    private const string Linux_Arm64_Version_Api = $"{Api_Base_Url}/Linux_Arm64/version";      // Linux ARM64 版本检测
    private const string Linux_Arm64_Download_Api = $"{Api_Base_Url}/Linux_Arm64/Latest.zip";     // Linux ARM64 下载地址 (ZIP格式)

    // ==================== macOS 平台 ====================
    // macOS x64 (Intel)
    private const string MacOS_X64_Version_Api = $"{Api_Base_Url}/MacOS_X64/version";        // macOS x64 版本检测
    private const string MacOS_X64_Download_Api = $"{Api_Base_Url}/MacOS_X64/Latest.zip";       // macOS x64 下载地址 (ZIP格式)

    // macOS ARM64 (Apple Silicon)
    private const string MacOS_Arm64_Version_Api = $"{Api_Base_Url}/MacOS_Arm64/version";      // macOS ARM64 版本检测
    private const string MacOS_Arm64_Download_Api = $"{Api_Base_Url}/MacOS_Arm64/Latest.zip";     // macOS ARM64 下载地址 (ZIP格式)

    // ==================== 正式版 API (默认Windows x64) ====================
    private const string Stable_Version_Api = $"{Api_Base_Url}/Win_X64/version";
    private const string Stable_Download_Api = $"{Api_Base_Url}/Win_X64/Latest.zip";
    private const string Stable_UpdateLog_Api = $"{Api_Base_Url}/updatelog";

    // ==================== GitHub Actions API ====================
    private const string GitHub_Api_Base = "https://api.github.com";
    private const string GitHub_Owner = "MCZLF";
    private const string GitHub_Repo = "MinecraftConnectTool";
    private const string GitHub_Actions_Url = $"{GitHub_Api_Base}/repos/{GitHub_Owner}/{GitHub_Repo}/actions/runs";
    private const string GitHub_Artifacts_Url = $"{GitHub_Api_Base}/repos/{GitHub_Owner}/{GitHub_Repo}/actions/runs";

    #endregion

    #region 属性

    [ObservableProperty]
    private string _currentVersion = "当前版本: 获取中...";

    [ObservableProperty]
    private string _cloudVersion = "云端版本: 获取中...";

    [ObservableProperty]
    private string _updateChannel = "默认";

    [ObservableProperty]
    private string _updateChannelIcon = "Api";

    [ObservableProperty]
    private string _updateLogContent = "点击「检查更新」按钮获取最新版本信息";

    [ObservableProperty]
    private string _updateButtonText = "立即更新";

    [ObservableProperty]
    private string _updateButtonIcon = "ArrowUpBold";

    [ObservableProperty]
    private bool _canUpdate = false;

    [ObservableProperty]
    private bool _hasNewUpdate = false;

    [ObservableProperty]
    private bool _isProgressVisible = false;

    [ObservableProperty]
    private double _progressValue = 0;

    [ObservableProperty]
    private bool _isDebugVisible = true;

    [ObservableProperty]
    private bool _isChecking = false;

    [ObservableProperty]
    private bool _canUpdateFromActions = false;

    [ObservableProperty]
    private string _actionsVersion = "Actions版本: 获取中...";

    [ObservableProperty]
    private string _actionsVersionShort = "";

    [ObservableProperty]
    private string _actionsCommitSha = "";

    [ObservableProperty]
    private bool _isActionsChannel = false;

    [ObservableProperty]
    private string _switchChannelButtonText = "切换至Action版本";

    [ObservableProperty]
    private string _switchChannelButtonIcon = "Github";

    #endregion

    #region 私有字段

    private string _cloudVersionRaw = "";
    
    // URL 配置
    private string _versionUrl = "";
    private string _updateLogUrl = "";
    private string _downloadUrl = "";
    
    // 防重复点击
    private int _clickCount = 0;
    private DateTime _lastClickTime = DateTime.MinValue;
    
    private readonly HttpClient _httpClient;
    private readonly Random _random;
    
    // Actions 通道 commit 信息
    private string _latestCommitMessage = "";
    private string _latestCommitSha = "";
    private string _latestCommitAuthor = "";
    private string _latestCommitDate = "";

    #endregion

    public UpdatePageViewModel()
    {
        _random = new Random();
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        // 设置随机浏览器User-Agent
        SetRandomUserAgent();

        LoadVersionInfo();
        InitializeUrls();
    }

    /// <summary>
    /// 设置随机浏览器User-Agent
    /// </summary>
    private void SetRandomUserAgent()
    {
        string userAgent = GetRandomUserAgent();
        _httpClient.DefaultRequestHeaders.Remove("User-Agent");
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
    }

    /// <summary>
    /// 获取随机浏览器User-Agent
    /// </summary>
    private string GetRandomUserAgent()
    {
        int c = _random.Next(120, 131), e = _random.Next(120, 131), f = _random.Next(120, 131), s = _random.Next(16, 18);
        return new[] {
            $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{c}.0.0.0 Safari/537.36",
            $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{c}.0.0.0 Safari/537.36 Edg/{e}.0.0.0",
            $"Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{c}.0.0.0 Safari/537.36",
            $"Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/{f}.0",
            $"Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/{s}.0 Safari/605.1.15"
        }[_random.Next(5)];
        //不伪造UA我怕又给我封号了（）
    }

    /// <summary>
    /// 页面加载时调用 - 每次进入页面自动检查更新
    /// </summary>
    public void OnPageLoaded()
    {
        // 重新初始化URL以获取最新的镜像源设置
        InitializeUrls();

        // 每次进入页面都自动检查更新
        // 使用 Task.Run 避免阻塞 UI 线程，并确保进度条正确显示
        _ = Task.Run(async () =>
        {
            await CheckUpdateAsync();
            await CheckGitHubActionsVersionAsync();
        });
    }

    #region 初始化

    private void LoadVersionInfo()
    {
        try
        {
            string version = MainWindow.version;
            if (string.IsNullOrEmpty(version))
            {
                version = "Unknown Version";
            }
            CurrentVersion = $"当前版本: {version}";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"获取当前版本时出错: {ex.Message}");
            CurrentVersion = "当前版本: Unknown Version";
        }
    }

    private void InitializeUrls()
    {
        // 获取当前平台架构
        var (platform, architecture) = GetCurrentPlatformInfo();

        UpdateChannel = "正式通道";
        UpdateChannelIcon = "Api";
        _updateLogUrl = Stable_UpdateLog_Api; // 正式版更新日志

        // 根据平台架构选择对应的正式版本API
        (_versionUrl, _downloadUrl) = GetStableApiUrls(platform, architecture);

        // 应用GitHub镜像源转换
        ApplyGitHubMirror();
    }

    /// <summary>
    /// 应用GitHub镜像源转换
    /// </summary>
    private void ApplyGitHubMirror()
    {
        // [已禁用] 国内加速不再需要GitHub镜像源
        // bool useMirror = ConfigService.Read("githubmirror", true);
        // if (!useMirror) return;
        //
        // string mirrorType = ConfigService.Read("githubmirrortype", "fastly");
        // string mirrorPrefix = mirrorType.ToLower() switch
        // {
        //     "cloudflare" => Mirror_Cloudflare,
        //     "hk" => Mirror_HK,
        //     "fastly" => Mirror_Fastly,
        //     _ => Mirror_Fastly
        // };
        //
        // // 转换包含github.com的URL
        // _downloadUrl = ConvertToMirrorUrl(_downloadUrl, mirrorPrefix);
        // _updateLogUrl = ConvertToMirrorUrl(_updateLogUrl, mirrorPrefix);
    }

    /// <summary>
    /// 将GitHub URL转换为镜像源URL
    /// </summary>
    private string ConvertToMirrorUrl(string url, string mirrorPrefix)
    {
        // [已禁用] 国内加速不再需要GitHub镜像源
        // if (string.IsNullOrEmpty(url)) return url;
        // if (!url.Contains("github.com")) return url;
        // if (url.StartsWith(mirrorPrefix)) return url;
        //
        // return mirrorPrefix + url;
        return url;
    }
    
    /// <summary>
    /// 获取当前平台信息
    /// </summary>
    private (string platform, string architecture) GetCurrentPlatformInfo()
    {
        string platform;
        string architecture;
        
        // 检测操作系统
        if (OperatingSystem.IsWindows())
        {
            platform = "Windows";
        }
        else if (OperatingSystem.IsLinux())
        {
            platform = "Linux";
        }
        else if (OperatingSystem.IsMacOS())
        {
            platform = "MacOS";
        }
        else
        {
            platform = "Unknown";
        }
        
        // 检测处理器架构
        architecture = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X86 => "X86",
            Architecture.X64 => "X64",
            Architecture.Arm => "Arm",
            Architecture.Arm64 => "Arm64",
            _ => "Unknown"
        };
        
        return (platform, architecture);
    }
    
    /// <summary>
    /// 获取正式版API地址
    /// </summary>
    private (string versionUrl, string downloadUrl) GetStableApiUrls(string platform, string architecture)
    {
        string versionUrl = platform switch
        {
            "Windows" => architecture switch
            {
                "X86" => Windows_X86_Version_Api,
                "X64" => Windows_X64_Version_Api,
                "Arm64" => Windows_Arm64_Version_Api,
                _ => ""
            },
            "Linux" => architecture switch
            {
                "X64" => Linux_X64_Version_Api,
                "Arm" => Linux_Arm_Version_Api,
                "Arm64" => Linux_Arm64_Version_Api,
                _ => ""
            },
            "MacOS" => architecture switch
            {
                "X64" => MacOS_X64_Version_Api,
                "Arm64" => MacOS_Arm64_Version_Api,
                _ => ""
            },
            _ => ""
        };
        
        string downloadUrl = platform switch
        {
            "Windows" => architecture switch
            {
                "X86" => Windows_X86_Download_Api,
                "X64" => Windows_X64_Download_Api,
                "Arm64" => Windows_Arm64_Download_Api,
                _ => ""
            },
            "Linux" => architecture switch
            {
                "X64" => Linux_X64_Download_Api,
                "Arm" => Linux_Arm_Download_Api,
                "Arm64" => Linux_Arm64_Download_Api,
                _ => ""
            },
            "MacOS" => architecture switch
            {
                "X64" => MacOS_X64_Download_Api,
                "Arm64" => MacOS_Arm64_Download_Api,
                _ => ""
            },
            _ => ""
        };
        
        // 如果特定平台的API未配置，默认回退到 Windows x64 (AMD64)
        if (string.IsNullOrEmpty(versionUrl))
        {
            versionUrl = Windows_X64_Version_Api;
            downloadUrl = Windows_X64_Download_Api;
        }
        
        return (versionUrl, downloadUrl);
    }

    #endregion

    #region 日志处理

    public void SetUpdateLog(string logText)
    {
        UpdateLogContent = logText;
    }

    #endregion

    #region 命令

    [RelayCommand]
    private async Task CheckUpdateAsync()
    {
        // 防重复点击检查
        TimeSpan timeSinceLastClick = DateTime.Now - _lastClickTime;
        if (timeSinceLastClick.TotalSeconds > 60)
        {
            _clickCount = 0;
        }
        
        if (_clickCount >= 10)
        {
            // 显示错误提示
            return;
        }
        
        _clickCount++;
        _lastClickTime = DateTime.Now;
        
        IsChecking = true;
        IsProgressVisible = true;
        ProgressValue = 30;
        
        try
        {
            string currentVersion = MainWindow.version;
            Debug.WriteLine($"当前版本: {currentVersion}");
            
            ProgressValue = 50;
            
            // 获取云端版本
            string cloudVersion = await FetchCloudVersionAsync();
            _cloudVersionRaw = cloudVersion;
            CloudVersion = $"云端版本: {cloudVersion}";
            
            ProgressValue = 70;
            
            // 获取更新日志
            string updateLog = await FetchUpdateLogAsync();
            SetUpdateLog(updateLog);
            
            ProgressValue = 100;
            
            // 比较版本
            if (currentVersion == cloudVersion)
            {
                CanUpdate = false;
                HasNewUpdate = false;
                UpdateButtonText = "已是最新版本";
                UpdateButtonIcon = "CheckCircle";
            }
            else
            {
                CanUpdate = true;
                HasNewUpdate = true;
                UpdateButtonText = "立即更新";
                UpdateButtonIcon = "ArrowUpBold";
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"检查更新失败: {ex.Message}");
            UpdateLogContent = $"检查更新失败: {ex.Message}";
        }
        finally
        {
            await Task.Delay(300);
            IsProgressVisible = false;
            IsChecking = false;
        }
    }

    [RelayCommand]
    private async Task SwitchChannelAsync()
    {
        if (IsActionsChannel)
        {
            // 切换回正式通道
            await SwitchToStableChannelAsync();
        }
        else
        {
            // 切换到 Actions 通道
            await SwitchToActionsChannelAsync();
        }
    }

    private async Task SwitchToActionsChannelAsync()
    {
        if (!CanUpdateFromActions) return;
        
        // 显示确认对话框（使用全局 Modal）
        bool confirmed = await ShowConfirmDialogAsync(
            "切换至 Actions 通道",
            "确定要切换到 GitHub Actions 自动构建版本吗？",
            "这是自动构建版本，非正式发布版本！\n\n• 包含最新的更新内容\n• 但可能包含未发现的 Bug\n• 可能不如正式版本稳定\n• 建议仅在测试环境使用");
        if (!confirmed)
        {
            return; // 用户取消
        }
        
        // 切换到 Actions 通道
        IsActionsChannel = true;
        SwitchChannelButtonText = "切换为正式通道";
        SwitchChannelButtonIcon = "ArrowLeft";
        
        // 更新云端版本显示为 Actions 版本格式
        if (!string.IsNullOrEmpty(ActionsVersionShort) && !string.IsNullOrEmpty(ActionsCommitSha))
        {
            CloudVersion = $"云端版本: {ActionsVersionShort}({ActionsCommitSha})";
        }
        else
        {
            CloudVersion = $"云端版本: Actions Build";
        }
        
        // 更新通道标识
        UpdateChannel = "Actions (自动构建)";
        UpdateChannelIcon = "Github";
        
        // 更新日志显示 Actions 信息
        await LoadActionsUpdateLogAsync();
        
        // 切换到 Actions 通道后立即启用更新按钮
        CanUpdate = true;
        HasNewUpdate = true;
        UpdateButtonText = "立即更新";
        UpdateButtonIcon = "ArrowUpBold";
    }

    private async Task SwitchToStableChannelAsync()
    {
        // 切换回正式通道
        IsActionsChannel = false;
        SwitchChannelButtonText = "切换至Action版本";
        SwitchChannelButtonIcon = "Github";
        
        // 恢复正式版本信息
        CloudVersion = $"云端版本: {_cloudVersionRaw}";
        UpdateChannel = "正式通道";
        UpdateChannelIcon = "Api";
        
        // 恢复正式通道日志
        try
        {
            string updateLog = await FetchUpdateLogAsync();
            SetUpdateLog(updateLog);
        }
        catch
        {
            UpdateLogContent = "点击「检查更新」按钮获取最新版本信息";
        }
        
        // 检查是否需要更新
        string currentVersion = MainWindow.version;
        if (currentVersion == _cloudVersionRaw)
        {
            CanUpdate = false;
            HasNewUpdate = false;
            UpdateButtonText = "已是最新版本";
            UpdateButtonIcon = "CheckCircle";
        }
        else
        {
            CanUpdate = true;
            HasNewUpdate = true;
            UpdateButtonText = "立即更新";
            UpdateButtonIcon = "ArrowUpBold";
        }
    }

    /// <summary>
    /// 获取 MainWindowViewModel 实例
    /// </summary>
    private MainWindowViewModel? GetMainWindowViewModel()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow?.DataContext is MainWindowViewModel vm)
            {
                return vm;
            }
        }
        return null;
    }

    /// <summary>
    /// 显示全局确认对话框
    /// </summary>
    private async Task<bool> ShowConfirmDialogAsync(string title, string message, string detail)
    {
        var mainVm = GetMainWindowViewModel();
        if (mainVm != null)
        {
            return await mainVm.ShowGlobalModalAsync(title, message, detail);
        }
        // 如果无法获取 MainWindowViewModel，默认返回 true
        return true;
    }

    private Task LoadActionsUpdateLogAsync()
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══ GitHub Actions 自动构建版本 ═══");
            sb.AppendLine();
            sb.AppendLine("⚠️ 警告：这是自动构建版本，非正式发布版本！");
            sb.AppendLine("   包含最新的更新内容，但可能不稳定。");
            
            if (!string.IsNullOrEmpty(_latestCommitMessage))
            {
                sb.AppendLine();
                sb.AppendLine($"📋 最新提交 ({_latestCommitSha}):");
                sb.AppendLine($"   {_latestCommitMessage}");
            }
            
            if (!string.IsNullOrEmpty(_latestCommitAuthor))
            {
                sb.AppendLine($"👤 作者: {_latestCommitAuthor}");
            }
            
            if (!string.IsNullOrEmpty(_latestCommitDate))
            {
                sb.AppendLine($"📅 时间: {_latestCommitDate}");
            }
            
            sb.AppendLine();
            sb.AppendLine("💡 如需稳定版本，请点击「切换为正式通道」按钮");
            
            UpdateLogContent = sb.ToString();
        }
        catch
        {
            UpdateLogContent = "═══ GitHub Actions 自动构建版本 ═══\n\n⚠️ 警告：这是自动构建版本，非正式发布版本！\n   包含最新的更新内容，但可能不稳定。\n\n💡 如需稳定版本，请切换回正式通道。";
        }
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task UpdateNowAsync()
    {
        if (IsActionsChannel)
        {
            await UpdateFromActionsAsync();
        }
        else
        {
            await UpdateFromStableAsync();
        }
    }

    private async Task UpdateFromStableAsync()
    {
        if (!CanUpdate) return;
        
        IsProgressVisible = true;
        ProgressValue = 0;
        
        try
        {
            var (platform, architecture) = GetCurrentPlatformInfo();
            
            string? currentDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName);
            if (string.IsNullOrEmpty(currentDirectory))
            {
                currentDirectory = AppContext.BaseDirectory;
                if (string.IsNullOrEmpty(currentDirectory))
                    throw new Exception("无法获取当前目录");
            }

            string currentProcessName = Path.GetFileName(Process.GetCurrentProcess().MainModule?.FileName) ?? "MinecraftConnectTool.exe";
            string fileExtension = Path.GetExtension(currentProcessName);

            Random random = new();
            int randomNum = random.Next(10000, 100000);
            string oldFileName = $"MinecraftConnectTool_Old{randomNum}{fileExtension}";
            string oldFilePath = Path.Combine(currentDirectory, oldFileName);
            string currentFilePath = Path.Combine(currentDirectory, currentProcessName);

            await Task.Run(() => File.Move(currentFilePath, oldFilePath));

            string zipFileName = $"MCTUpdatePack_{randomNum}.zip";
            string zipFilePath = Path.Combine(currentDirectory, zipFileName);
            string extractDir = Path.Combine(currentDirectory, $"MCTExtract_{randomNum}");

            try
            {
                using (var response = await _httpClient.GetAsync(_downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    long totalBytes = response.Content.Headers.ContentLength ?? 0;
                    long downloadedBytes = 0;

                    using (var httpStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(zipFilePath, FileMode.Create, FileAccess.Write, FileShare.Write, bufferSize: 8192, useAsync: true))
                    {
                        byte[] buffer = new byte[8192];
                        int bytesRead;
                        while ((bytesRead = await httpStream.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
                        {
                            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                            downloadedBytes += bytesRead;
                            if (totalBytes > 0)
                                ProgressValue = (int)((downloadedBytes * 80) / totalBytes);
                        }
                    }
                }

                ProgressValue = 85;
                Directory.CreateDirectory(extractDir);
                await Task.Run(() => ZipFile.ExtractToDirectory(zipFilePath, extractDir));

                ProgressValue = 90;
                string extractedFile = FindExecutableInDirectory(extractDir, platform);
                if (string.IsNullOrEmpty(extractedFile))
                    throw new Exception("解压后未找到可执行文件");

                ProgressValue = 95;
                File.Move(extractedFile, currentFilePath);
                ProgressValue = 100;
            }
            finally
            {
                CleanupTempFiles(zipFilePath, extractDir);
            }

            switch (platform)
            {
                case "Windows":
                    await ExecuteWindowsUpdate(currentDirectory, currentProcessName, oldFileName);
                    break;
                case "Linux":
                    await ExecuteLinuxUpdate(currentDirectory, currentProcessName, oldFileName);
                    break;
                case "MacOS":
                    await ExecuteMacOSUpdate(currentDirectory, currentProcessName, oldFileName);
                    break;
                default:
                    throw new NotSupportedException($"不支持的平台: {platform}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"下载更新失败: {ex.Message}");
            UpdateLogContent = $"下载更新失败: {ex.Message}";
            IsProgressVisible = false;
            ProgressValue = 0;
        }
    }

    [RelayCommand]
    private async Task UpdateFromActionsAsync()
    {
        if (!CanUpdateFromActions) return;
        
        IsProgressVisible = true;
        ProgressValue = 0;
        
        try
        {
            // 获取当前平台信息
            var (platform, architecture) = GetCurrentPlatformInfo();
            
            // 获取 GitHub Actions 最新构建的下载链接
            string? artifactUrl = await FetchGitHubActionsArtifactUrlAsync(platform, architecture);
            if (string.IsNullOrEmpty(artifactUrl))
            {
                UpdateLogContent = "无法获取 GitHub Actions 构建版本，请稍后重试或前往 GitHub 手动下载";
                IsProgressVisible = false;
                return;
            }
            
            // 使用进程主模块路径获取应用程序目录（比 AppContext.BaseDirectory 更可靠）
            string? currentDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName);
            if (string.IsNullOrEmpty(currentDirectory))
            {
                // 回退到 AppContext.BaseDirectory
                currentDirectory = AppContext.BaseDirectory;
                if (string.IsNullOrEmpty(currentDirectory))
                {
                    throw new Exception("无法获取当前目录");
                }
            }

            // 获取当前实际运行的文件名
            string currentProcessName = Path.GetFileName(Process.GetCurrentProcess().MainModule?.FileName) ?? "MinecraftConnectTool.exe";
            string fileExtension = Path.GetExtension(currentProcessName);

            // 生成随机数用于文件名
            Random random = new();
            int randomNum = random.Next(10000, 100000);
            string oldFileName = $"MinecraftConnectTool_Old{randomNum}{fileExtension}";
            string oldFilePath = Path.Combine(currentDirectory, oldFileName);
            string currentFilePath = Path.Combine(currentDirectory, currentProcessName);

            // 先把当前运行中的文件重命名为旧版本
            await Task.Run(() => File.Move(currentFilePath, oldFilePath));

            // 临时ZIP下载文件名
            string zipFileName = $"MCTUpdatePack_{randomNum}.zip";
            string zipFilePath = Path.Combine(currentDirectory, zipFileName);
            // 解压临时目录
            string extractDir = Path.Combine(currentDirectory, $"MCTExtract_{randomNum}");

            try
            {
                // 下载ZIP文件
                using (var response = await _httpClient.GetAsync(artifactUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    long totalBytes = response.Content.Headers.ContentLength ?? 0;
                    long downloadedBytes = 0;

                    using (var httpStream = await response.Content.ReadAsStreamAsync())
                    {
                        using (var fileStream = new FileStream(zipFilePath, FileMode.Create, FileAccess.Write, FileShare.Write, bufferSize: 8192, useAsync: true))
                        {
                            byte[] buffer = new byte[8192];
                            int bytesRead;

                            while ((bytesRead = await httpStream.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
                            {
                                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                                downloadedBytes += bytesRead;

                                // 更新进度条 (下载阶段占80%)
                                if (totalBytes > 0)
                                {
                                    ProgressValue = (int)((downloadedBytes * 80) / totalBytes);
                                }
                            }
                        }
                    }
                }

                // 解压ZIP文件
                ProgressValue = 85;
                Directory.CreateDirectory(extractDir);
                await Task.Run(() => ZipFile.ExtractToDirectory(zipFilePath, extractDir));

                // 查找解压后的可执行文件
                ProgressValue = 90;
                string extractedFile = FindExecutableInDirectory(extractDir, platform);
                if (string.IsNullOrEmpty(extractedFile))
                {
                    throw new Exception("解压后未找到可执行文件");
                }

                // 移动解压后的文件到目标位置
                ProgressValue = 95;
                File.Move(extractedFile, currentFilePath);

                ProgressValue = 100;
            }
            catch
            {
                throw;
            }
            finally
            {
                // 清理临时文件
                CleanupTempFiles(zipFilePath, extractDir);
            }

            // 根据平台执行不同的更新逻辑
            switch (platform)
            {
                case "Windows":
                    await ExecuteWindowsUpdate(currentDirectory, currentProcessName, oldFileName);
                    break;
                case "Linux":
                    await ExecuteLinuxUpdate(currentDirectory, currentProcessName, oldFileName);
                    break;
                case "MacOS":
                    await ExecuteMacOSUpdate(currentDirectory, currentProcessName, oldFileName);
                    break;
                default:
                    throw new NotSupportedException($"不支持的平台: {platform}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"下载更新失败: {ex.Message}");
            UpdateLogContent = $"下载更新失败: {ex.Message}";
            IsProgressVisible = false;
            ProgressValue = 0;
        }
    }
    
    /// <summary>
    /// 执行Windows平台更新
    /// </summary>
    private async Task ExecuteWindowsUpdate(string currentDirectory, string currentProcessName, string oldFileName)
    {
        string currentFilePath = Path.Combine(currentDirectory, currentProcessName);

        // 启动新版本（原文件名）
        Process.Start(new ProcessStartInfo(currentFilePath)
        {
            UseShellExecute = true
        });

        // 创建自删除批处理脚本 - 删除旧版本文件
        string deleteScriptPath = Path.Combine(currentDirectory, $"delete_old_{Guid.NewGuid():N}.bat");
        string scriptContent = $@"@echo off
cd /d ""%~dp0""
timeout /t 2 /nobreak >nul
del /f /q ""{oldFileName}""
del /f /q ""%~nx0""";
        
        await File.WriteAllTextAsync(deleteScriptPath, scriptContent);

        Process.Start(new ProcessStartInfo(deleteScriptPath)
        {
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true
        });

        // 退出当前程序
        Environment.Exit(0);
    }
    
    /// <summary>
    /// 执行Linux平台更新
    /// </summary>
    private async Task ExecuteLinuxUpdate(string currentDirectory, string currentProcessName, string oldFileName)
    {
        string currentFilePath = Path.Combine(currentDirectory, currentProcessName);

        // 设置新文件为可执行
        await Task.Run(() =>
        {
            Process.Start(new ProcessStartInfo("chmod", $"+x \"{currentFilePath}\"")
            {
                UseShellExecute = false
            })?.WaitForExit();
        });

        // 启动新版本
        Process.Start(new ProcessStartInfo(currentFilePath)
        {
            UseShellExecute = true
        });

        // 创建自删除shell脚本 - 删除旧版本文件
        string deleteScriptPath = Path.Combine(currentDirectory, $"delete_old_{Guid.NewGuid():N}.sh");
        string scriptContent = $"#!/bin/bash\nsleep 2\nrm -f \"{Path.Combine(currentDirectory, oldFileName)}\"\nrm -f \"{deleteScriptPath}\"";
        await File.WriteAllTextAsync(deleteScriptPath, scriptContent);
        
        // 设置脚本为可执行
        await Task.Run(() =>
        {
            Process.Start(new ProcessStartInfo("chmod", $"+x \"{deleteScriptPath}\"")
            {
                UseShellExecute = false
            })?.WaitForExit();
        });

        // 在后台运行删除脚本
        Process.Start(new ProcessStartInfo("bash", deleteScriptPath)
        {
            UseShellExecute = false,
            CreateNoWindow = true
        });

        // 退出当前程序
        Environment.Exit(0);
    }
    
    /// <summary>
    /// 执行macOS平台更新
    /// </summary>
    private async Task ExecuteMacOSUpdate(string currentDirectory, string currentProcessName, string oldFileName)
    {
        string currentFilePath = Path.Combine(currentDirectory, currentProcessName);

        // 设置新文件为可执行
        await Task.Run(() =>
        {
            Process.Start(new ProcessStartInfo("chmod", $"+x \"{currentFilePath}\"")
            {
                UseShellExecute = false
            })?.WaitForExit();
        });

        // 启动新版本
        Process.Start(new ProcessStartInfo(currentFilePath)
        {
            UseShellExecute = true
        });

        // 创建自删除shell脚本 - 删除旧版本文件
        string deleteScriptPath = Path.Combine(currentDirectory, $"delete_old_{Guid.NewGuid():N}.sh");
        string scriptContent = $"#!/bin/bash\nsleep 2\nrm -f \"{Path.Combine(currentDirectory, oldFileName)}\"\nrm -f \"{deleteScriptPath}\"";
        await File.WriteAllTextAsync(deleteScriptPath, scriptContent);
        
        // 设置脚本为可执行
        await Task.Run(() =>
        {
            Process.Start(new ProcessStartInfo("chmod", $"+x \"{deleteScriptPath}\"")
            {
                UseShellExecute = false
            })?.WaitForExit();
        });

        // 在后台运行删除脚本
        Process.Start(new ProcessStartInfo("bash", deleteScriptPath)
        {
            UseShellExecute = false,
            CreateNoWindow = true
        });

        // 退出当前程序
        Environment.Exit(0);
    }

    [RelayCommand]
    private void DebugAction()
    {
        // 调试功能 - 模拟原版的异常抛出
        UpdateLogContent = "调试模式已激活\n准备抛出测试异常...";
        throw new Exception("114514,点继续就没事了");
    }

    #endregion

    #region 网络请求

    /// <summary>
    /// 获取 GitHub Actions 最新构建的 artifact 下载链接
    /// </summary>
    private async Task<string?> FetchGitHubActionsArtifactUrlAsync(string platform, string architecture)
    {
        try
        {
            // 构建 artifact 名称模式（匹配实际的 artifact 命名）
            string artifactName = platform switch
            {
                "Windows" => architecture switch
                {
                    "X64" => "Win_X64",
                    "Arm64" => "Win_Arm64",
                    _ => "Win_X64"
                },
                "Linux" => architecture switch
                {
                    "X64" => "Linux_X64",
                    "Arm64" => "Linux_Arm64",
                    _ => "Linux_X64"
                },
                "MacOS" => architecture switch
                {
                    "X64" => "MacOS_X64",
                    "Arm64" => "MacOS_Arm64",
                    _ => "MacOS_X64"
                },
                _ => "Win_X64"
            };

            // 创建新的 HttpClient 用于 GitHub API，添加必要的请求头
            using var githubClient = new HttpClient();
            githubClient.DefaultRequestHeaders.Add("User-Agent", "MinecraftConnectTool/1.0");
            githubClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
            githubClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

            // 获取最新的 workflow runs（不限制分支）
            string runsUrl = $"{GitHub_Actions_Url}?status=completed&per_page=1";
            using var runsResponse = await githubClient.GetAsync(runsUrl);
            
            if (!runsResponse.IsSuccessStatusCode)
            {
                Debug.WriteLine($"获取 GitHub Actions runs 失败: {runsResponse.StatusCode}");
                return null;
            }

            string runsJson = await runsResponse.Content.ReadAsStringAsync();
            using var runsDoc = System.Text.Json.JsonDocument.Parse(runsJson);
            
            if (!runsDoc.RootElement.TryGetProperty("workflow_runs", out var runsElement) || 
                runsElement.GetArrayLength() == 0)
            {
                Debug.WriteLine("没有找到 workflow runs");
                return null;
            }

            var firstRun = runsElement[0];
            if (!firstRun.TryGetProperty("id", out var runIdElement))
            {
                Debug.WriteLine("无法获取 run id");
                return null;
            }

            long runId = runIdElement.GetInt64();
            
            // 获取该 run 的 artifacts
            string artifactsUrl = $"{GitHub_Artifacts_Url}/{runId}/artifacts";
            using var artifactsResponse = await githubClient.GetAsync(artifactsUrl);
            
            if (!artifactsResponse.IsSuccessStatusCode)
            {
                Debug.WriteLine($"获取 artifacts 失败: {artifactsResponse.StatusCode}");
                return null;
            }

            string artifactsJson = await artifactsResponse.Content.ReadAsStringAsync();
            using var artifactsDoc = System.Text.Json.JsonDocument.Parse(artifactsJson);
            
            if (!artifactsDoc.RootElement.TryGetProperty("artifacts", out var artifactsElement))
            {
                Debug.WriteLine("没有找到 artifacts");
                return null;
            }

            // 查找匹配的 artifact
            foreach (var artifact in artifactsElement.EnumerateArray())
            {
                if (artifact.TryGetProperty("name", out var nameElement) &&
                    nameElement.GetString()?.StartsWith(artifactName) == true)
                {
                    if (artifact.TryGetProperty("id", out var idElement))
                    {
                        long artifactId = idElement.GetInt64();
                        // 使用 nightly.link 绕过 GitHub 身份验证
                        string nightlyLink = $"https://nightly.link/{GitHub_Owner}/{GitHub_Repo}/actions/artifacts/{artifactId}.zip";
                        Debug.WriteLine($"找到 artifact: {nameElement.GetString()}, nightly.link: {nightlyLink}");
                        return nightlyLink;
                    }
                }
            }

            // 如果没有找到精确匹配，返回第一个 artifact
            foreach (var artifact in artifactsElement.EnumerateArray())
            {
                if (artifact.TryGetProperty("id", out var idElement))
                {
                    long artifactId = idElement.GetInt64();
                    // 使用 nightly.link 绕过 GitHub 身份验证
                    string nightlyLink = $"https://nightly.link/{GitHub_Owner}/{GitHub_Repo}/actions/artifacts/{artifactId}.zip";
                    Debug.WriteLine($"使用第一个 artifact, nightly.link: {nightlyLink}");
                    return nightlyLink;
                }
            }

            Debug.WriteLine("没有找到任何 artifact");
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"获取 GitHub Actions artifact URL 失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 检查 GitHub Actions 是否有新版本
    /// </summary>
    private async Task CheckGitHubActionsVersionAsync()
    {
        try
        {
            // 创建新的 HttpClient 用于 GitHub API，添加必要的请求头
            using var githubClient = new HttpClient();
            githubClient.DefaultRequestHeaders.Add("User-Agent", "MinecraftConnectTool/1.0");
            githubClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
            githubClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
            
            // 先尝试获取任意分支的最新 completed run
            string runsUrl = $"{GitHub_Actions_Url}?status=completed&per_page=1";
            using var response = await githubClient.GetAsync(runsUrl);
            
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                
                if (doc.RootElement.TryGetProperty("workflow_runs", out var runsElement) && 
                    runsElement.GetArrayLength() > 0)
                {
                    var firstRun = runsElement[0];
                    if (firstRun.TryGetProperty("run_number", out var runNumberElement))
                    {
                        int runNumber = runNumberElement.GetInt32();
                        string? headBranch = null;
                        string? headSha = null;
                        
                        if (firstRun.TryGetProperty("head_branch", out var branchElement))
                            headBranch = branchElement.GetString();
                        if (firstRun.TryGetProperty("head_sha", out var shaElement))
                            headSha = shaElement.GetString();
                        
                        ActionsVersion = $"Actions版本: Build #{runNumber} ({headBranch})";
                        CanUpdateFromActions = true;
                        
                        // 获取最新 commit 信息并更新到日志区
                        await FetchLatestCommitInfoAsync(githubClient, headBranch, headSha);
                        return;
                    }
                }
            }
            else
            {
                Debug.WriteLine($"GitHub API 返回错误: {response.StatusCode}");
                string errorContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"错误详情: {errorContent}");
            }
            
            ActionsVersion = "Actions版本: 获取失败";
            CanUpdateFromActions = false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"检查 GitHub Actions 版本失败: {ex.Message}");
            ActionsVersion = "Actions版本: 获取失败";
            CanUpdateFromActions = false;
        }
    }

    /// <summary>
    /// 获取最新 commit 信息
    /// </summary>
    private async Task FetchLatestCommitInfoAsync(HttpClient githubClient, string? branch, string? headSha)
    {
        try
        {
            string commitsUrl = !string.IsNullOrEmpty(branch) 
                ? $"{GitHub_Api_Base}/repos/{GitHub_Owner}/{GitHub_Repo}/commits?sha={branch}&per_page=1"
                : $"{GitHub_Api_Base}/repos/{GitHub_Owner}/{GitHub_Repo}/commits?per_page=1";
            
            using var response = await githubClient.GetAsync(commitsUrl);
            
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                
                if (doc.RootElement.GetArrayLength() > 0)
                {
                    var commit = doc.RootElement[0];
                    string? sha = commit.TryGetProperty("sha", out var shaElement) 
                        ? shaElement.GetString()?.Substring(0, 7) 
                        : headSha?.Substring(0, 7);
                    
                    string? message = null;
                    string? author = null;
                    string? date = null;
                    
                    if (commit.TryGetProperty("commit", out var commitElement))
                    {
                        if (commitElement.TryGetProperty("message", out var msgElement))
                            message = msgElement.GetString();
                        if (commitElement.TryGetProperty("author", out var authorElement))
                        {
                            if (authorElement.TryGetProperty("name", out var nameElement))
                                author = nameElement.GetString();
                            if (authorElement.TryGetProperty("date", out var dateElement))
                            {
                                date = dateElement.GetString();
                                if (DateTime.TryParse(date, out var dt))
                                    date = dt.ToString("yyyy-MM-dd HH:mm");
                            }
                        }
                    }
                    
                    // 存储 commit 信息供后续使用
                    _latestCommitSha = sha ?? "";
                    _latestCommitMessage = message ?? "";
                    _latestCommitAuthor = author ?? "";
                    _latestCommitDate = date ?? "";
                    
                    // 设置 Actions 版本号格式: 0.0.7(4690dca)
                    ActionsVersionShort = "0.0.7"; // 从分支名或配置中获取版本号
                    ActionsCommitSha = _latestCommitSha;
                    
                    // 不再直接更新日志，等待用户切换到 Actions 通道后才显示
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"获取 commit 信息失败: {ex.Message}");
            // 仅存储错误信息，不直接更新日志显示
            _latestCommitMessage = "";
            _latestCommitSha = "";
        }
    }

    private async Task<string> FetchCloudVersionAsync()
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(_versionUrl);
            if (response.IsSuccessStatusCode)
            {
                // 读取字节数组并正确处理编码，避免中文乱码
                byte[] bytes = await response.Content.ReadAsByteArrayAsync();
                string content = Encoding.UTF8.GetString(bytes);
                return content.Trim();
            }
            else
            {
                return string.Empty;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"获取云端版本失败: {ex.Message}");
            return string.Empty;
        }
    }

    private async Task<string> FetchUpdateLogAsync()
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(_updateLogUrl);
            if (response.IsSuccessStatusCode)
            {
                // 读取字节数组并正确处理编码，避免中文乱码
                byte[] bytes = await response.Content.ReadAsByteArrayAsync();
                // 尝试 UTF-8 编码
                string content = Encoding.UTF8.GetString(bytes);
                return content;
            }
            else
            {
                return $"Error: {response.StatusCode} || 可能是服务正在维护中...";
            }
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// 在解压目录中查找可执行文件
    /// </summary>
    private string FindExecutableInDirectory(string directory, string platform)
    {
        string[] executablePatterns = platform switch
        {
            "Windows" => new[] { "*.exe" },
            "Linux" => new[] { "*.AppImage", "*" }, // Linux可能没有扩展名
            "MacOS" => new[] { "*.app", "*" },
            _ => new[] { "*" }
        };

        foreach (var pattern in executablePatterns)
        {
            var files = Directory.GetFiles(directory, pattern, SearchOption.AllDirectories);
            foreach (var file in files)
            {
                // 排除常见的非可执行文件
                string fileName = Path.GetFileName(file).ToLower();
                if (fileName.EndsWith(".zip") || fileName.EndsWith(".txt") || fileName.EndsWith(".md"))
                    continue;

                // Windows平台检查.exe文件
                if (platform == "Windows" && fileName.EndsWith(".exe"))
                    return file;

                // Linux/MacOS平台返回第一个非特殊文件
                if (platform != "Windows")
                    return file;
            }
        }

        // 如果没有找到特定模式，返回目录中的第一个文件
        var allFiles = Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            .ToList();

        return allFiles.FirstOrDefault() ?? string.Empty;
    }

    /// <summary>
    /// 清理临时文件和目录
    /// </summary>
    private void CleanupTempFiles(string zipFilePath, string extractDir)
    {
        try
        {
            // 删除ZIP文件
            if (File.Exists(zipFilePath))
            {
                File.Delete(zipFilePath);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"删除临时ZIP文件失败: {ex.Message}");
        }

        try
        {
            // 删除解压目录
            if (Directory.Exists(extractDir))
            {
                Directory.Delete(extractDir, true);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"删除解压目录失败: {ex.Message}");
        }
    }

    #endregion
}

/// <summary>
/// HttpClient 扩展方法
/// </summary>
public static class HttpClientExtensions
{
    public static void DownloadFile(this HttpClient client, string url, string filePath)
    {
        var bytes = client.GetByteArrayAsync(url).Result;
        File.WriteAllBytes(filePath, bytes);
    }
}
