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

    // ==================== GitHub 镜像源前缀 (已弃用) ====================
    private const string Mirror_Cloudflare = "https://gh-proxy.org/";
    private const string Mirror_HK = "https://hk.gh-proxy.org/";
    private const string Mirror_Fastly = "https://cdn.gh-proxy.org/";

    // ==================== 测试版/内测版 API (新API) ====================
    // 测试版 (Preview)
    private const string Preview_Version_Api = $"{Api_Base_Url}/Preview/version";
    private const string Preview_Download_Api = $"{Api_Base_Url}/Preview/Latest.zip";
    private const string Preview_UpdateLog_Api = $"{Api_Base_Url}/Preview/updatelog";

    // 内测版 (Nightly)
    private const string Nightly_Version_Api = $"{Api_Base_Url}/Nightly/version";
    private const string Nightly_Download_Api = $"{Api_Base_Url}/Nightly/Latest.zip";
    private const string Nightly_UpdateLog_Api = $"{Api_Base_Url}/Nightly/updatelog";
    private const string Nightly_List_Api = $"{Api_Base_Url}/Nightly/List";

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
    private string _updateLogContent = "喵喵喵？检查一下更新？";

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
    private string _betaTipTitle = "如需使用测试通道，请在设置中开启接收测试更新";

    [ObservableProperty]
    private string _betaTipContent = "测试用户如需降级回正式版，直接在设置中关闭接收测试更新并重新检查更新即可";

    [ObservableProperty]
    private bool _isChecking = false;

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
        bool getPreUpdate = ConfigService.Read("getpreupdate", false);

        // 获取当前平台架构
        var (platform, architecture) = GetCurrentPlatformInfo();

        // 非Windows平台强制使用正式版
        if (!OperatingSystem.IsWindows())
        {
            getPreUpdate = false;
        }

        if (getPreUpdate)
        {
            // 测试模式下显示关闭提示
            BetaTipTitle = "如需关闭测试通道，请在设置中关闭接收测试更新";
            BetaTipContent = "测试版本可能存在未修复的问题，如需稳定使用请切换回正式版";

            // 简化内测逻辑：如果设备在名单中，自动切换到内测模式
            bool isInNightlyList = CheckName();

            if (isInNightlyList)
            {
                UpdateChannel = "内测";
                UpdateChannelIcon = "Flask";
                _updateLogUrl = Nightly_UpdateLog_Api;

                // 根据平台架构选择对应的内测版本API
                (_versionUrl, _downloadUrl) = GetPreviewApiUrls(platform, architecture, true);
            }
            else
            {
                UpdateChannel = "测试";
                UpdateChannelIcon = "ClockOutline";
                _updateLogUrl = Preview_UpdateLog_Api;

                // 根据平台架构选择对应的测试版本API
                (_versionUrl, _downloadUrl) = GetPreviewApiUrls(platform, architecture, false);
            }
        }
        else
        {
            // 正式版显示开启提示
            BetaTipTitle = "如需使用测试通道，请在设置中开启接收测试更新";
            BetaTipContent = "测试用户如需降级回正式版，直接在设置中关闭接收测试更新并重新检查更新即可";
            UpdateChannel = "默认";
            UpdateChannelIcon = "Api";
            _updateLogUrl = Stable_UpdateLog_Api; // 正式版更新日志

            // 根据平台架构选择对应的正式版本API
            (_versionUrl, _downloadUrl) = GetStableApiUrls(platform, architecture);
        }

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
    
    /// <summary>
    /// 获取测试/内测版API地址
    /// </summary>
    private (string versionUrl, string downloadUrl) GetPreviewApiUrls(string platform, string architecture, bool isNightly)
    {
        // 测试版和内测版目前使用相同的回退逻辑
        // 后续可以为不同平台配置专门的测试版API
        
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
        
        // 如果特定平台的API未配置，使用测试版/内测版共用API
        if (string.IsNullOrEmpty(versionUrl))
        {
            if (isNightly)
            {
                versionUrl = Nightly_Version_Api;
                downloadUrl = Nightly_Download_Api;
            }
            else
            {
                versionUrl = Preview_Version_Api;
                downloadUrl = Preview_Download_Api;
            }
        }
        
        return (versionUrl, downloadUrl);
    }

    /// <summary>
    /// 检查内测名单
    /// </summary>
    private bool CheckName()
    {
        string listUrl = Nightly_List_Api;
        string machineName = Environment.MachineName;

        try
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "MCZLFAPP", "Temp");
            if (!Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(tempPath);
            }

            string filePath = Path.Combine(tempPath, "list.txt");

            using (var client = new HttpClient())
            {
                // 添加 User-Agent 请求头，避免 405 错误
                client.DefaultRequestHeaders.Add("User-Agent", "MinecraftConnectTool/1.0");
                client.DownloadFile(listUrl, filePath);
            }

            bool contains = false;
            string[] lines = File.ReadAllLines(filePath);
            foreach (string line in lines)
            {
                if (line.Trim() == machineName)
                {
                    contains = true;
                    break;
                }
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            return contains;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"检查内测名单失败: {ex.Message}");
            return false;
        }
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
    private async Task UpdateNowAsync()
    {
        if (!CanUpdate) return;
        
        IsProgressVisible = true;
        ProgressValue = 0;
        
        try
        {
            // 获取当前平台信息
            var (platform, architecture) = GetCurrentPlatformInfo();
            
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
                using (var response = await _httpClient.GetAsync(_downloadUrl, HttpCompletionOption.ResponseHeadersRead))
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
