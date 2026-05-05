using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using MinecraftConnectTool.Services;
using MinecraftConnectTool.Views;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace MinecraftConnectTool.ViewModels;

/// <summary>
/// 初次启动引导向导ViewModel
/// </summary>
public partial class FirstLaunchWizardViewModel : ViewModelBase
{
    #region 页面导航

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _totalPages = 3;

    [ObservableProperty]
    private string _nextButtonText = "Next→";

    partial void OnCurrentPageChanged(int value)
    {
        UpdateNextButtonText();
        OnPropertyChanged(nameof(IsPage1Visible));
        OnPropertyChanged(nameof(IsPage2Visible));
        OnPropertyChanged(nameof(IsPage3Visible));
        OnPropertyChanged(nameof(IsPage4Visible));
        OnPropertyChanged(nameof(ProgressValue));
    }

    public bool IsPage1Visible => CurrentPage == 1;
    public bool IsPage2Visible => CurrentPage == 2;
    public bool IsPage3Visible => CurrentPage == 3;
    public bool IsPage4Visible => CurrentPage == 4;

    public double ProgressValue => (double)CurrentPage / TotalPages * 100;

    private void UpdateNextButtonText()
    {
        NextButtonText = CurrentPage == TotalPages ? "开启联机之旅" : "Next→";
    }

    #endregion

    #region 第一页：欢迎

    [ObservableProperty]
    private string _welcomeTitle = "Hi，初次见面";

    [ObservableProperty]
    private string _welcomeSubtitle = "欢迎使用 MinecraftConnectTool";

    [ObservableProperty]
    private string _welcomeDescription = "接下来我们需要准备一下";

    #endregion

    #region 第二页：用户名/游戏ID

    [ObservableProperty]
    private string _username = "";

    [ObservableProperty]
    private string _usernamePlaceholder = "请输入您的游戏ID/昵称";

    [ObservableProperty]
    private bool _enablePerformanceMode;

    [ObservableProperty]
    private bool _isLowMemoryDevice;

    [ObservableProperty]
    private string _memoryInfoText = "";

    [ObservableProperty]
    private long _totalMemoryGB;

    async partial void OnEnablePerformanceModeChanged(bool value)
    {
        if (_isInitializingPerformanceMode)
            return;

        // 更新内存信息文本
        UpdateMemoryInfoText();

        // 如果是低内存设备且用户试图关闭性能模式，显示确认对话框
        if (IsLowMemoryDevice && !value)
        {
            await ShowLowMemoryWarningAsync();
        }
    }

    /// <summary>
    /// 根据性能模式状态和内存情况更新提示文本
    /// </summary>
    private void UpdateMemoryInfoText()
    {
        if (IsLowMemoryDevice)
        {
            MemoryInfoText = $"当前设备Ram:{TotalMemoryGB}G,已自动开启性能模式";
        }
        else
        {
            if (EnablePerformanceMode)
            {
                MemoryInfoText = $"当前设备Ram:{TotalMemoryGB}G,不是吧...没必要😱";
            }
            else
            {
                MemoryInfoText = $"当前设备Ram:{TotalMemoryGB}G,无需启用";
            }
        }
    }

    private bool _isInitializingPerformanceMode = false;

    /// <summary>
    /// 检查内存并初始化性能模式设置
    /// </summary>
    public void InitializePerformanceMode()
    {
        _isInitializingPerformanceMode = true;
        try
        {
            // 尝试获取物理内存大小（MB）
            if (TryGetTotalPhysicalMemoryMB(out var totalMemoryMB))
            {
                TotalMemoryGB = totalMemoryMB / 1024; // 转换为GB
                IsLowMemoryDevice = totalMemoryMB <= 4096; // ≤4GB
                
                // 低内存设备自动开启性能模式
                if (IsLowMemoryDevice)
                {
                    EnablePerformanceMode = true;
                }
            }
            // 获取失败不处理，保持默认状态
            
            // 设置内存信息文本
            UpdateMemoryInfoText();
        }
        finally
        {
            _isInitializingPerformanceMode = false;
        }
    }

    /// <summary>
    /// 尝试获取物理内存大小（MB）
    /// </summary>
    /// <param name="totalMemoryMB">输出的内存大小（MB）</param>
    /// <returns>是否成功获取</returns>
    private static bool TryGetTotalPhysicalMemoryMB(out long totalMemoryMB)
    {
        totalMemoryMB = 0;
        
        try
        {
            // 方法1: 使用 GC.GetGCMemoryInfo 获取内存信息
            var gcInfo = GC.GetGCMemoryInfo();
            var totalMemoryBytes = gcInfo.TotalAvailableMemoryBytes;
            totalMemoryMB = totalMemoryBytes / 1024 / 1024;
            
            // 如果 GC 方法获取的内存合理（大于 2GB），认为成功
            if (totalMemoryMB >= 2048)
            {
                return true;
            }
            
            // 方法2: 使用 System.Diagnostics 获取物理内存 (Windows)
            #if WINDOWS
            try
            {
                var computerInfo = new Microsoft.VisualBasic.Devices.ComputerInfo();
                totalMemoryMB = (long)(computerInfo.TotalPhysicalMemory / 1024 / 1024);
                if (totalMemoryMB >= 1024)
                {
                    return true;
                }
            }
            catch { }
            #endif
            
            // 方法3: 读取 /proc/meminfo (Linux)
            if (OperatingSystem.IsLinux() && File.Exists("/proc/meminfo"))
            {
                try
                {
                    var memInfo = File.ReadAllText("/proc/meminfo");
                    var match = System.Text.RegularExpressions.Regex.Match(memInfo, @"MemTotal:\s+(\d+)\s+kB");
                    if (match.Success && long.TryParse(match.Groups[1].Value, out var memKB))
                    {
                        totalMemoryMB = memKB / 1024;
                        return true;
                    }
                }
                catch { }
            }
            
            // 方法4: 使用 sysctl (macOS)
            if (OperatingSystem.IsMacOS())
            {
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo("sysctl", "-n hw.memsize")
                    {
                        RedirectStandardOutput = true,
                        UseShellExecute = false
                    };
                    using var process = System.Diagnostics.Process.Start(psi);
                    if (process != null)
                    {
                        var output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();
                        if (long.TryParse(output.Trim(), out var memBytes))
                        {
                            totalMemoryMB = memBytes / 1024 / 1024;
                            return true;
                        }
                    }
                }
                catch { }
            }
            
            // 所有方法都失败
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 显示低内存警告对话框
    /// </summary>
    private async Task ShowLowMemoryWarningAsync()
    {
        // 获取当前窗口
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w is FirstLaunchWizardWindow);
            if (window != null)
            {
                await ExtensionUI.MD3MessageDialog.ShowWarningAsync(
                    window,
                    "当前设备运行内存小于或等于4G，建议保持性能模式开启以获得更好的体验",
                    "性能模式建议");
            }
        }
    }

    #endregion

    #region 第三页：主题设置

    [ObservableProperty]
    private bool _isDarkMode = true;

    partial void OnIsDarkModeChanged(bool value)
    {
        ApplyThemeSettings();
    }

    [ObservableProperty]
    private bool _enableColorMode = false;

    partial void OnEnableColorModeChanged(bool value)
    {
        ApplyThemeSettings();
    }

    [ObservableProperty]
    private Color _accentColor = Color.Parse("#6750A4");

    partial void OnAccentColorChanged(Color value)
    {
        ApplyThemeSettings();
    }

    [ObservableProperty]
    private double _mixIntensity = 0.12;

    partial void OnMixIntensityChanged(double value)
    {
        ApplyThemeSettings();
    }

    [ObservableProperty]
    private bool _enablePhotoBackground = false;

    partial void OnEnablePhotoBackgroundChanged(bool value)
    {
        ApplyThemeSettings();
    }

    [ObservableProperty]
    private string? _photoBackgroundPath = null;

    partial void OnPhotoBackgroundPathChanged(string? value)
    {
        ApplyThemeSettings();
    }

    [ObservableProperty]
    private double _backgroundOpacity = 0.10;

    partial void OnBackgroundOpacityChanged(double value)
    {
        ApplyThemeSettings();
    }

    [ObservableProperty]
    private double _controlOpacity = 0.40;

    partial void OnControlOpacityChanged(double value)
    {
        ApplyThemeSettings();
    }

    // 预设主题
    [ObservableProperty]
    private bool _isPreset1Selected = false;

    [ObservableProperty]
    private bool _isPreset2Selected = false;

    [ObservableProperty]
    private bool _isPreset3Selected = false;

    [ObservableProperty]
    private bool _isPreset4Selected = false;

    [ObservableProperty]
    private bool _isCustomSelected = true;

    // 预设图片本地路径（缩略图）
    [ObservableProperty]
    private string _preset1ImagePath = "";

    [ObservableProperty]
    private string _preset2ImagePath = "";

    [ObservableProperty]
    private string _preset3ImagePath = "";

    [ObservableProperty]
    private string _preset4ImagePath = "";

    // 预设图片Bitmap（用于显示缩略图）
    [ObservableProperty]
    private Bitmap? _preset1Image;

    [ObservableProperty]
    private Bitmap? _preset2Image;

    [ObservableProperty]
    private Bitmap? _preset3Image;

    [ObservableProperty]
    private Bitmap? _preset4Image;

    // 预设图片原图URL
    private const string Preset1OriginalUrl = "https://api.mct.mczlf.loft.games/WebResource/shouanren.png";
    private const string Preset2OriginalUrl = "https://api.mct.mczlf.loft.games/WebResource/qianxiao.jpg";
    private const string Preset3OriginalUrl = "https://api.mct.mczlf.loft.games/WebResource/bg1.jpeg";
    private const string Preset4OriginalUrl = "https://api.mct.mczlf.loft.games/WebResource/bg2.jpeg";

    #endregion

    #region 第四页：用户须知

    [ObservableProperty]
    private string _userNoticeTitle = "用户须知";

    [ObservableProperty]
    private string _userNoticeContent = "";

    #endregion

    #region 命令

    [RelayCommand]
    private void NextPage()
    {
        // 保存当前页数据
        SaveCurrentPageData();

        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
        }
        else
        {
            // 完成向导
            CompleteWizard();
        }
    }

    [RelayCommand]
    private void PreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
        }
    }

    [RelayCommand]
    private async Task ApplyPreset1()
    {
        // 预设1：鸣潮_守岸人 - 深色模式，紫色偏深主题，背景模糊10%，控件不模糊50%
        IsPreset1Selected = true;
        IsPreset2Selected = false;
        IsPreset3Selected = false;
        IsPreset4Selected = false;
        IsCustomSelected = false;

        IsDarkMode = true;
        EnableColorMode = true;
        AccentColor = Color.Parse("#7B1FA2"); // 紫色偏深
        MixIntensity = 0.12;
        EnablePhotoBackground = true;
        // 下载并应用完整图片
        PhotoBackgroundPath = await DownloadFullImageAsync(Preset1OriginalUrl, "shouanren_full.png");
        BackgroundOpacity = 0.10;
        ControlOpacity = 0.50;

        ApplyThemeSettings();
    }

    [RelayCommand]
    private void ApplyPreset2()
    {
        // 预设2：鸣潮_千咲 - 亮色模式，浅蓝色（偏白，晴天），背景模糊10%，控件不模糊35%
        IsPreset1Selected = false;
        IsPreset2Selected = true;
        IsPreset3Selected = false;
        IsPreset4Selected = false;
        IsCustomSelected = false;

        IsDarkMode = false;
        EnableColorMode = true;
        AccentColor = Color.Parse("#64B5F6"); // 浅蓝色加深（晴天蓝）
        MixIntensity = 0.10;
        EnablePhotoBackground = true;
        // 千咲图片太大，直接使用缩略图
        PhotoBackgroundPath = Preset2ImagePath;
        BackgroundOpacity = 0.10;
        ControlOpacity = 0.35;

        ApplyThemeSettings();
    }

    [RelayCommand]
    private async Task ApplyPreset3()
    {
        // 预设3：有兽焉_天禄 - 亮色浅蓝主题，照片背景bg1.jpeg，透明度10%，控件不透明度40%，混色浓度0.12
        IsPreset1Selected = false;
        IsPreset2Selected = false;
        IsPreset3Selected = true;
        IsPreset4Selected = false;
        IsCustomSelected = false;

        IsDarkMode = false;
        EnableColorMode = true;
        AccentColor = Color.Parse("#87CEEB"); // 浅蓝
        MixIntensity = 0.12;
        EnablePhotoBackground = true;
        // 下载并应用完整图片
        PhotoBackgroundPath = await DownloadFullImageAsync(Preset3OriginalUrl, "bg1_full.jpeg");
        BackgroundOpacity = 0.10;
        ControlOpacity = 0.40;

        ApplyThemeSettings();
    }

    [RelayCommand]
    private async Task ApplyPreset4()
    {
        // 预设4：有兽焉_辟邪 - 亮色标准浅红主题，照片背景bg2.jpeg，透明度10%，控件不透明度40%，混色浓度0.12
        IsPreset1Selected = false;
        IsPreset2Selected = false;
        IsPreset3Selected = false;
        IsPreset4Selected = true;
        IsCustomSelected = false;

        IsDarkMode = false;
        EnableColorMode = true;
        AccentColor = Color.Parse("#FF6B6B"); // 标准浅红
        MixIntensity = 0.12;
        EnablePhotoBackground = true;
        // 下载并应用完整图片
        PhotoBackgroundPath = await DownloadFullImageAsync(Preset4OriginalUrl, "bg2_full.jpeg");
        BackgroundOpacity = 0.10;
        ControlOpacity = 0.40;

        ApplyThemeSettings();
    }

    [RelayCommand]
    private void ApplyCustomTheme()
    {
        IsPreset1Selected = false;
        IsPreset2Selected = false;
        IsPreset3Selected = false;
        IsPreset4Selected = false;
        IsCustomSelected = true;
    }

    [RelayCommand]
    private void ToggleDarkMode()
    {
        IsDarkMode = !IsDarkMode;
        ApplyThemeSettings();
    }

    [RelayCommand]
    private void CloseWindow()
    {
        // 仅关闭窗口，不保存任何设置
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            foreach (var window in desktop.Windows)
            {
                if (window is FirstLaunchWizardWindow wizardWindow)
                {
                    wizardWindow.Close();
                    break;
                }
            }
        }
    }

    [RelayCommand]
    private async Task SkipWizard()
    {
        // 显示二次确认对话框
        var result = await ShowSkipConfirmationDialogAsync();
        if (result)
        {
            // 使用默认设置完成向导
            ApplyDefaultSettings();
            CompleteWizard();
        }
    }

    [RelayCommand]
    private void SkipThemeSetup()
    {
        // 跳过主题设置，使用默认主题
        ApplyDefaultSettings();
        // 直接进入下一页（完成）
        CompleteWizard();
    }

    [RelayCommand]
    private void OpenWiki()
    {
        // 打开官方知识库
        try
        {
            var url = "https://mct.mczlf.loft.games";
            if (OperatingSystem.IsWindows())
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                System.Diagnostics.Process.Start("xdg-open", url);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"打开知识库失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenBilibili()
    {
        // 打开哔哩哔哩教程视频
        try
        {
            var url = "https://www.bilibili.com/video/BV1sBXyYgE1j";
            if (OperatingSystem.IsWindows())
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                System.Diagnostics.Process.Start("xdg-open", url);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"打开哔哩哔哩失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 显示跳过确认对话框
    /// </summary>
    private async Task<bool> ShowSkipConfirmationDialogAsync()
    {
        try
        {
            // 获取当前窗口作为对话框的父窗口
            Window? parentWindow = null;
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                parentWindow = desktop.MainWindow;
            }

            if (parentWindow == null) return false;

            // 使用现有的MD3MessageDialog.ShowAsync方法显示确认对话框
            // 由于ShowAsync是void返回，我们需要创建自定义对话框
            var tcs = new TaskCompletionSource<bool>();

            // 创建对话框窗口
            var dialogWindow = new Window
            {
                Width = 380,
                Height = double.NaN,
                MinHeight = 180,
                MaxHeight = 250,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                SystemDecorations = SystemDecorations.None,
                Background = Avalonia.Media.Brushes.Transparent,
                ShowInTaskbar = false
            };

            // 创建对话框内容
            var dialogContent = new ExtensionUI.MD3MessageDialog
            {
                Title = "确认跳过",
                Message = "一键跳过将使用默认设置完成初始化。\n\n您可以在「设置」中随时修改这些选项。",
                IconKind = Material.Icons.MaterialIconKind.HelpCircle
            };

            dialogWindow.Content = dialogContent;

            // 获取按钮并设置事件
            var confirmButton = dialogContent.FindControl<Avalonia.Controls.Button>("ConfirmButton");
            var closeButton = dialogContent.FindControl<Avalonia.Controls.Button>("CloseButton");

            if (confirmButton != null)
            {
                confirmButton.Content = "确认跳过";
                confirmButton.Click += (s, e) =>
                {
                    tcs.TrySetResult(true);
                    dialogWindow.Close();
                };
            }

            if (closeButton != null)
            {
                closeButton.Click += (s, e) =>
                {
                    tcs.TrySetResult(false);
                    dialogWindow.Close();
                };
            }

            // 窗口关闭时如果没有设置结果，设为false
            dialogWindow.Closed += (s, e) =>
            {
                tcs.TrySetResult(false);
            };

            // 显示对话框
            await dialogWindow.ShowDialog(parentWindow);

            return await tcs.Task;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"显示确认对话框失败: {ex.Message}");
            // 如果对话框显示失败，直接返回true允许跳过
            return true;
        }
    }

    /// <summary>
    /// 应用默认设置
    /// </summary>
    private void ApplyDefaultSettings()
    {
        // 使用系统默认主题设置
        IsDarkMode = true;
        EnableColorMode = false;
        EnablePhotoBackground = false;
        ApplyThemeSettings();
    }

    #endregion

    // 所有预设图片文件名列表（缩略图）
    private static readonly string[] AllPresetImageFiles = new[]
    {
        "shouanren.png",
        "qianxiao.jpg",
        "bg1.jpeg",
        "bg2.jpeg"
    };

    // 所有完整图片文件名列表
    private static readonly string[] AllFullImageFiles = new[]
    {
        "shouanren_full.png",
        "qianxiao_full.jpg",
        "bg1_full.jpeg",
        "bg2_full.jpeg"
    };

    public FirstLaunchWizardViewModel()
    {
        // 先设置默认图片路径
        var configDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP") ?? Path.GetTempPath(), "MCZLFAPP", "Temp");
        Preset1ImagePath = Path.Combine(configDir, "shouanren.png");
        Preset2ImagePath = Path.Combine(configDir, "qianxiao.jpg");
        Preset3ImagePath = Path.Combine(configDir, "bg1.jpeg");
        Preset4ImagePath = Path.Combine(configDir, "bg2.jpeg");

        // 初始化时加载当前主题设置
        LoadCurrentThemeSettings();
        // 下载预设图片（如果本地不存在）
        _ = DownloadPresetImagesAsync();
    }

    /// <summary>
    /// 下载预设主题图片到本地
    /// </summary>
    private async Task DownloadPresetImagesAsync()
    {
        try
        {
            var tempPath = Environment.GetEnvironmentVariable("TEMP") ?? Path.GetTempPath();
            var configDir = Path.Combine(tempPath, "MCZLFAPP", "Temp");
            Directory.CreateDirectory(configDir);

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            // 下载预设1：鸣潮_守岸人
            var preset1Path = Path.Combine(configDir, "shouanren.png");
            if (!File.Exists(preset1Path))
            {
                var url = "https://api.mct.mczlf.loft.games/WebResource/shouanren.png";
                var bytes = await httpClient.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(preset1Path, bytes);
            }
            Preset1ImagePath = preset1Path;
            if (File.Exists(preset1Path))
            {
                Preset1Image = await LoadThumbnailAsync(preset1Path, 300, 80);
            }

            // 下载预设2：鸣潮_千咲
            var preset2Path = Path.Combine(configDir, "qianxiao.jpg");
            if (!File.Exists(preset2Path))
            {
                var url = "https://api.mct.mczlf.loft.games/WebResource/qianxiao.jpg";
                var bytes = await httpClient.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(preset2Path, bytes);
            }
            Preset2ImagePath = preset2Path;
            if (File.Exists(preset2Path))
            {
                Preset2Image = await LoadThumbnailAsync(preset2Path, 300, 80);
            }

            // 下载预设3：有兽焉_天禄
            var preset3Path = Path.Combine(configDir, "bg1.jpeg");
            if (!File.Exists(preset3Path))
            {
                var url = "https://api.mct.mczlf.loft.games/WebResource/bg1.jpeg";
                var bytes = await httpClient.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(preset3Path, bytes);
            }
            Preset3ImagePath = preset3Path;
            if (File.Exists(preset3Path))
            {
                Preset3Image = await LoadThumbnailAsync(preset3Path, 300, 80);
            }

            // 下载预设4：有兽焉_辟邪
            var preset4Path = Path.Combine(configDir, "bg2.jpeg");
            if (!File.Exists(preset4Path))
            {
                var url = "https://api.mct.mczlf.loft.games/WebResource/bg2.jpeg";
                var bytes = await httpClient.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(preset4Path, bytes);
            }
            Preset4ImagePath = preset4Path;
            if (File.Exists(preset4Path))
            {
                Preset4Image = await LoadThumbnailAsync(preset4Path, 300, 80);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"下载预设图片失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 加载缩略图（降低画质）
    /// </summary>
    private async Task<Bitmap?> LoadThumbnailAsync(string imagePath, int targetWidth, int targetHeight)
    {
        try
        {
            // 直接加载原图，但可以在需要时添加缩放逻辑
            // 由于Avalonia的Bitmap不支持直接缩放，这里先直接加载
            // 后续可以考虑使用SkiaSharp等库进行高质量缩放
            await Task.Yield(); // 让出线程
            return new Bitmap(imagePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载缩略图失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 下载完整图片用于应用背景
    /// </summary>
    private async Task<string> DownloadFullImageAsync(string imageUrl, string fileName)
    {
        try
        {
            var tempPath = Environment.GetEnvironmentVariable("TEMP") ?? Path.GetTempPath();
            var configDir = Path.Combine(tempPath, "MCZLFAPP", "Temp");
            Directory.CreateDirectory(configDir);

            var fullImagePath = Path.Combine(configDir, fileName);

            // 如果完整图片已存在，直接返回
            if (File.Exists(fullImagePath))
            {
                return fullImagePath;
            }

            // 下载完整图片
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(60);
            var bytes = await httpClient.GetByteArrayAsync(imageUrl);
            await File.WriteAllBytesAsync(fullImagePath, bytes);
            Console.WriteLine($"已下载完整图片: {fileName}");

            return fullImagePath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"下载完整图片失败: {ex.Message}");
            // 失败时返回缩略图路径作为备用
            var tempPath = Environment.GetEnvironmentVariable("TEMP") ?? Path.GetTempPath();
            var configDir = Path.Combine(tempPath, "MCZLFAPP", "Temp");
            return Path.Combine(configDir, fileName.Replace("_full", ""));
        }
    }

    private void LoadCurrentThemeSettings()
    {
        var themeService = ThemeService.Instance;
        IsDarkMode = themeService.IsDarkMode;
        EnableColorMode = themeService.EnableColorMode;
        AccentColor = themeService.AccentColor;
        MixIntensity = themeService.MixIntensity;
        EnablePhotoBackground = themeService.EnablePhotoBackground;
        PhotoBackgroundPath = themeService.PhotoBackgroundPath;
        BackgroundOpacity = themeService.BackgroundOpacity;
        ControlOpacity = themeService.ControlOpacity;
    }

    private void ApplyThemeSettings()
    {
        var themeService = ThemeService.Instance;
        themeService.IsDarkMode = IsDarkMode;
        themeService.EnableColorMode = EnableColorMode;
        themeService.AccentColor = AccentColor;
        themeService.MixIntensity = MixIntensity;
        themeService.EnablePhotoBackground = EnablePhotoBackground;
        themeService.PhotoBackgroundPath = PhotoBackgroundPath;
        themeService.BackgroundOpacity = BackgroundOpacity;
        themeService.ControlOpacity = ControlOpacity;

        // 主题改变后触发预设选中状态的属性变更通知，使UI更新背景色
        if (IsPreset1Selected)
        {
            OnPropertyChanged(nameof(IsPreset1Selected));
        }
        if (IsPreset2Selected)
        {
            OnPropertyChanged(nameof(IsPreset2Selected));
        }
        if (IsPreset3Selected)
        {
            OnPropertyChanged(nameof(IsPreset3Selected));
        }
        if (IsPreset4Selected)
        {
            OnPropertyChanged(nameof(IsPreset4Selected));
        }
        if (IsCustomSelected)
        {
            OnPropertyChanged(nameof(IsCustomSelected));
        }
    }

    private void SaveCurrentPageData()
    {
        switch (CurrentPage)
        {
            case 2:
                // 保存用户名
                if (!string.IsNullOrWhiteSpace(Username))
                {
                    ConfigService.Write("Username", Username.Trim());
                }
                // 保存性能模式设置
                ConfigService.Write("EnablePerformanceMode", EnablePerformanceMode);
                break;

            case 3:
                // 保存主题设置
                ApplyThemeSettings();
                break;
        }
    }

    private void CompleteWizard()
    {
        // 标记已完成首次引导
        ConfigService.Write("AlreadyFirstGuild", true);

        // 清理未使用的临时图片
        CleanupUnusedImages();

        // 关闭向导窗口并重启应用
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 获取当前窗口并关闭
            foreach (var window in desktop.Windows)
            {
                if (window is FirstLaunchWizardWindow wizardWindow)
                {
                    wizardWindow.Close();
                    break;
                }
            }

            // 重启应用
            RestartApplication();
        }
    }

    /// <summary>
    /// 清理未使用的临时图片
    /// </summary>
    private void CleanupUnusedImages()
    {
        try
        {
            var tempPath = Environment.GetEnvironmentVariable("TEMP") ?? Path.GetTempPath();
            var configDir = Path.Combine(tempPath, "MCZLFAPP", "Temp");

            // 获取当前选中的图片文件名
            var selectedImageFile = Path.GetFileName(PhotoBackgroundPath ?? "");

            // 删除未使用的缩略图
            foreach (var fileName in AllPresetImageFiles)
            {
                if (!string.Equals(fileName, selectedImageFile, StringComparison.OrdinalIgnoreCase))
                {
                    var filePath = Path.Combine(configDir, fileName);
                    if (File.Exists(filePath))
                    {
                        try
                        {
                            File.Delete(filePath);
                            Console.WriteLine($"已删除未使用的缩略图: {fileName}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"删除缩略图失败 {fileName}: {ex.Message}");
                        }
                    }
                }
            }

            // 删除未使用的完整图片
            foreach (var fileName in AllFullImageFiles)
            {
                if (!string.Equals(fileName, selectedImageFile, StringComparison.OrdinalIgnoreCase))
                {
                    var filePath = Path.Combine(configDir, fileName);
                    if (File.Exists(filePath))
                    {
                        try
                        {
                            File.Delete(filePath);
                            Console.WriteLine($"已删除未使用的完整图片: {fileName}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"删除完整图片失败 {fileName}: {ex.Message}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"清理临时图片失败: {ex.Message}");
        }
    }

    private void RestartApplication()
    {
        try
        {
            // 启动新进程
            var processPath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(processPath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = processPath,
                    UseShellExecute = true
                });
            }

            // 退出当前应用
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"重启应用失败: {ex.Message}");
        }
    }
}
