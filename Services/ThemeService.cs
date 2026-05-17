using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Platform;

namespace MinecraftConnectTool.Services;

/// <summary>
/// 动画速度枚举
/// </summary>
public enum AnimationSpeed
{
    /// <summary>快速 - 100ms</summary>
    Fast,
    /// <summary>适中 - 300ms (默认)</summary>
    Medium,
    /// <summary>慢速 - 500ms</summary>
    Slow,
    /// <summary>自定义</summary>
    Custom
}

/// <summary>
/// 渲染方式枚举
/// </summary>
public enum RenderingMode
{
    /// <summary>系统默认 - 自动选择最佳渲染方式</summary>
    SystemDefault,
    /// <summary>GPU 渲染 - 使用硬件加速，性能更好</summary>
    Gpu,
    /// <summary>CPU 渲染 - 软件渲染，兼容性更好</summary>
    Cpu
}

/// <summary>
/// 主题服务 - 管理应用程序的主题和视觉风格
/// 
/// 【功能说明】
/// 1. 支持深色/浅色模式切换 (IsDarkMode)
/// 2. 支持 Fluent Design (Win11) 风格模拟 (SimulateFluentDesign)
/// 3. 支持彩色模式和自定义主题色 (EnableColorMode, AccentColor)
/// 
/// 【使用方式】
/// - 获取实例: ThemeService.Instance
/// - 切换深色模式: ThemeService.Instance.IsDarkMode = true/false
/// - 切换 Fluent 风格: ThemeService.Instance.SimulateFluentDesign = true/false
/// - 切换彩色模式: ThemeService.Instance.EnableColorMode = true/false
/// - 设置主题色: ThemeService.Instance.AccentColor = Color.Parse("#b35031ff")
/// 
/// 【XAML 资源】
/// - 颜色资源: MaterialPrimaryBrush, MaterialSurfaceBrush, MaterialBackgroundBrush, AccentBrush, AccentLightBrush, AccentDarkBrush 等
/// - 圆角资源: CardCornerRadius (卡片), ButtonCornerRadius (按钮), InputCornerRadius (输入框)
/// - 透明度资源: OverlayOpacity
/// - 标志资源: IsFluentDesignEnabled (bool), IsColorModeEnabled (bool) - 可用于条件样式
/// 
/// 【风格对比】
/// | 特性 | Material Design | Fluent Design (Win11) |
/// |------|-----------------|----------------------|
/// | 主色调 | 紫色 #6750A4 | Win11蓝 #0078D4 |
/// | 深色背景 | #1C1B1F | #1A1A1A |
/// | 亮色背景 | #FFFBFE | #F2F2F2 |
/// | 卡片圆角 | 16px | 8px |
/// | 按钮圆角 | 20px | 4px |
/// </summary>

public class ThemeService
{
    private static ThemeService? _instance;
    public static ThemeService Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new ThemeService();
                // 初始化主题
                _instance.InitializeTheme();
            }
            return _instance;
        }
    }

    private ThemeService() { }

    /// <summary>
    /// 照片背景设置变更事件
    /// </summary>
    public event EventHandler? PhotoBackgroundChanged;

    /// <summary>
    /// 动画速度设置变更事件
    /// </summary>
    public event EventHandler? AnimationSpeedChanged;

    /// <summary>
    /// 全局文字加粗设置变更事件
    /// </summary>
    public event EventHandler? GlobalBoldTextChanged;

    private bool _isDarkMode = true;
    private bool _simulateFluentDesign = false;
    private bool _enableColorMode = false;
    private Color _accentColor = Color.Parse("#6750A4");
    private double _mixIntensity = 0.15;
    private bool _enablePhotoBackground = false;
    private string? _photoBackgroundPath = null;
    private double _backgroundOpacity = 0.30;
    private double _controlOpacity = 0.30;
    private AnimationSpeed _animationSpeed = AnimationSpeed.Medium;
    private double _customAnimationDuration = 200;
    private RenderingMode _renderingMode = RenderingMode.SystemDefault;
    private bool _enableGlobalBoldText = false;

    private void InitializeTheme()
    {
        // 检查配置项是否存在
        if (ConfigService.Exists("IsDarkMode"))
        {
            // 配置项存在，使用配置值
            _isDarkMode = ConfigService.Read<bool>("IsDarkMode", true);
        }
        else
        {
            // 配置项不存在，根据系统设置决定
            _isDarkMode = GetSystemTheme();
            // 保存到配置
            ConfigService.Write("IsDarkMode", _isDarkMode);
        }

        // 读取 Fluent Design 模拟设置
        _simulateFluentDesign = ConfigService.Read<bool>("SimulateFluentDesign", false);

        // 读取彩色模式设置
        _enableColorMode = ConfigService.Read<bool>("EnableColorMode", false);

        // 读取主题色设置
        var accentColorHex = ConfigService.Read<string>("AccentColor", "#6750A4");
        if (Color.TryParse(accentColorHex, out var parsedColor))
        {
            _accentColor = parsedColor;
        }
        
        // 读取混色浓度设置
        _mixIntensity = ConfigService.Read<double>("MixIntensity", 0.15);

        // 读取照片背景设置
        _enablePhotoBackground = ConfigService.Read<bool>("EnablePhotoBackground", false);
        _photoBackgroundPath = ConfigService.Read<string?>("PhotoBackgroundPath", null);
        _backgroundOpacity = ConfigService.Read<double>("BackgroundOpacity", 0.30);
        _controlOpacity = ConfigService.Read<double>("ControlOpacity", 0.30);

        // 读取动画速度设置
        var animationSpeedStr = ConfigService.Read<string>("AnimationSpeed", "Medium");
        if (Enum.TryParse<AnimationSpeed>(animationSpeedStr, out var parsedSpeed))
        {
            _animationSpeed = parsedSpeed;
        }
        _customAnimationDuration = ConfigService.Read<double>("CustomAnimationDuration", 200);

        // 读取渲染方式设置
        var renderingModeStr = ConfigService.Read<string>("RenderingMode", "SystemDefault");
        if (Enum.TryParse<RenderingMode>(renderingModeStr, out var parsedRenderingMode))
        {
            _renderingMode = parsedRenderingMode;
        }

        // 读取全局文字加粗设置
        _enableGlobalBoldText = ConfigService.Read<bool>("BoldTextOnPrint", false);
    }

    private static bool GetSystemTheme()
    {
        try
        {
            // 使用 Avalonia 的方式检测系统主题
            if (Application.Current?.ActualThemeVariant == ThemeVariant.Dark)
            {
                return true;
            }
            return false;
        }
        catch
        {
            // 默认使用暗色
            return true;
        }
    }

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (_isDarkMode != value)
            {
                _isDarkMode = value;
                // 保存到配置
                ConfigService.Write("IsDarkMode", value);
                ApplyTheme();
            }
        }
    }

    public bool SimulateFluentDesign
    {
        get => _simulateFluentDesign;
        set
        {
            if (_simulateFluentDesign != value)
            {
                _simulateFluentDesign = value;
                // 保存到配置
                ConfigService.Write("SimulateFluentDesign", value);
                ApplyTheme();
            }
        }
    }

    /// <summary>
    /// 是否启用彩色模式
    /// </summary>
    public bool EnableColorMode
    {
        get => _enableColorMode;
        set
        {
            if (_enableColorMode != value)
            {
                _enableColorMode = value;
                // 保存到配置
                ConfigService.Write("EnableColorMode", value);
                ApplyTheme();
            }
        }
    }

    /// <summary>
    /// 主题色（强调色）
    /// </summary>
    public Color AccentColor
    {
        get => _accentColor;
        set
        {
            if (_accentColor != value)
            {
                _accentColor = value;
                // 保存到配置
                ConfigService.Write("AccentColor", value.ToString());
                ApplyTheme();
            }
        }
    }

    /// <summary>
    /// 混色浓度 (0.01 - 1.00)
    /// </summary>
    public double MixIntensity
    {
        get => _mixIntensity;
        set
        {
            if (_mixIntensity != value)
            {
                _mixIntensity = Math.Clamp(value, 0.01, 1.00);
                // 保存到配置
                ConfigService.Write("MixIntensity", _mixIntensity);
                ApplyTheme();
            }
        }
    }

    /// <summary>
    /// 是否启用照片背景
    /// </summary>
    public bool EnablePhotoBackground
    {
        get => _enablePhotoBackground;
        set
        {
            if (_enablePhotoBackground != value)
            {
                _enablePhotoBackground = value;
                // 保存到配置
                ConfigService.Write("EnablePhotoBackground", value);
                ApplyTheme();
                // 触发照片背景变更事件
                PhotoBackgroundChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// 照片背景图片路径
    /// </summary>
    public string? PhotoBackgroundPath
    {
        get => _photoBackgroundPath;
        set
        {
            if (_photoBackgroundPath != value)
            {
                _photoBackgroundPath = value;
                // 保存到配置
                ConfigService.Write("PhotoBackgroundPath", (object?)value ?? string.Empty);
                ApplyTheme();
                // 触发照片背景变更事件
                PhotoBackgroundChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// 背景透明度 (0.1 - 0.9)
    /// </summary>
    public double BackgroundOpacity
    {
        get => _backgroundOpacity;
        set
        {
            if (_backgroundOpacity != value)
            {
                _backgroundOpacity = Math.Clamp(value, 0.1, 0.9);
                // 保存到配置
                ConfigService.Write("BackgroundOpacity", _backgroundOpacity);
                ApplyTheme();
                // 触发照片背景变更事件
                PhotoBackgroundChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// 控件区域不透明度 (0.0 - 0.8)，用于增强控件可读性
    /// </summary>
    public double ControlOpacity
    {
        get => _controlOpacity;
        set
        {
            if (_controlOpacity != value)
            {
                _controlOpacity = Math.Clamp(value, 0.0, 0.8);
                // 保存到配置
                ConfigService.Write("ControlOpacity", _controlOpacity);
                // 触发照片背景变更事件
                PhotoBackgroundChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// 动画速度设置
    /// </summary>
    public AnimationSpeed AnimationSpeed
    {
        get => _animationSpeed;
        set
        {
            if (_animationSpeed != value)
            {
                _animationSpeed = value;
                ConfigService.Write("AnimationSpeed", value.ToString());
                AnimationSpeedChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// 自定义动画时长（毫秒）
    /// </summary>
    public double CustomAnimationDuration
    {
        get => _customAnimationDuration;
        set
        {
            if (_customAnimationDuration != value)
            {
                // 支持 0（关闭动画）或 50-2000
                if (value == 0)
                {
                    _customAnimationDuration = 0;
                }
                else
                {
                    _customAnimationDuration = Math.Clamp(value, 50, 2000);
                }
                ConfigService.Write("CustomAnimationDuration", _customAnimationDuration);
                if (_animationSpeed == AnimationSpeed.Custom)
                {
                    AnimationSpeedChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }

    /// <summary>
    /// 获取当前动画速度对应的持续时间（毫秒）
    /// </summary>
    public double AnimationDurationMs => _animationSpeed switch
    {
        AnimationSpeed.Fast => 100,
        AnimationSpeed.Medium => 300,
        AnimationSpeed.Slow => 500,
        AnimationSpeed.Custom => _customAnimationDuration,
        _ => 100
    };

    /// <summary>
    /// 获取当前动画速度对应的 TimeSpan
    /// </summary>
    public TimeSpan AnimationDuration => TimeSpan.FromMilliseconds(AnimationDurationMs);

    /// <summary>
    /// 渲染方式
    /// </summary>
    public RenderingMode RenderingMode
    {
        get => _renderingMode;
        set
        {
            if (_renderingMode != value)
            {
                _renderingMode = value;
                ConfigService.Write("RenderingMode", value.ToString());
                RenderingModeChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// 渲染方式变更事件
    /// </summary>
    public event EventHandler? RenderingModeChanged;

    /// <summary>
    /// 是否启用全局文字加粗
    /// </summary>
    public bool EnableGlobalBoldText
    {
        get => _enableGlobalBoldText;
        set
        {
            if (_enableGlobalBoldText != value)
            {
                _enableGlobalBoldText = value;
                ConfigService.Write("BoldTextOnPrint", value);
                GlobalBoldTextChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public void Initialize()
    {
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        var app = Application.Current;
        if (app == null) return;

        // 设置 Avalonia 主题变体
        app.RequestedThemeVariant = _isDarkMode ? ThemeVariant.Dark : ThemeVariant.Light;

        // 更新资源颜色
        UpdateMaterialColors(app);
        
        // 更新动画时长资源
        UpdateAnimationDuration(app);
    }
    
    /// <summary>
    /// 更新动画时长资源
    /// </summary>
    private void UpdateAnimationDuration(Application app)
    {
        var resources = app.Resources;
        var duration = AnimationDuration;
        resources["PageTransitionDuration"] = duration;
        resources["PageTransitionDurationMs"] = AnimationDurationMs;
    }

    private void UpdateMaterialColors(Application app)
    {
        var resources = app.Resources;

        // 设置彩色模式标志
        resources["IsColorModeEnabled"] = _enableColorMode;

        if (_enableColorMode)
        {
            // 应用彩色模式主题色
            ApplyAccentColors(resources);
        }
        else if (_simulateFluentDesign)
        {
            ApplyFluentDesignColors(resources);
        }
        else
        {
            ApplyMaterialDesignColors(resources);
        }
    }

    /// <summary>
    /// 应用自定义主题色（彩色模式）
    /// </summary>
    private void ApplyAccentColors(Avalonia.Controls.IResourceDictionary resources)
    {
        var primary = _accentColor;
        var primaryLight = LightenColor(primary, 0.2);
        var primaryDark = DarkenColor(primary, 0.2);
        var onPrimary = GetContrastColor(primary);

        if (_isDarkMode)
        {
            // 彩色模式 - 深色主题
            resources["MaterialPrimaryBrush"] = new SolidColorBrush(primary);
            resources["MaterialOnPrimaryBrush"] = new SolidColorBrush(onPrimary);
            resources["MaterialPrimaryContainerBrush"] = new SolidColorBrush(primaryDark);
            resources["MaterialOnPrimaryContainerBrush"] = new SolidColorBrush(primary);

            resources["MaterialSecondaryBrush"] = new SolidColorBrush(primaryLight);
            resources["MaterialOnSecondaryBrush"] = new SolidColorBrush(onPrimary);
            resources["MaterialSecondaryContainerBrush"] = new SolidColorBrush(DarkenColor(primary, 0.3));
            resources["MaterialOnSecondaryContainerBrush"] = new SolidColorBrush(primaryLight);

            resources["MaterialTertiaryBrush"] = new SolidColorBrush(primaryLight);
            resources["MaterialOnTertiaryBrush"] = new SolidColorBrush(onPrimary);
            resources["MaterialTertiaryContainerBrush"] = new SolidColorBrush(DarkenColor(primary, 0.4));
            resources["MaterialOnTertiaryContainerBrush"] = new SolidColorBrush(primaryLight);

            // 背景使用主题色的暗色调（使用动态混色浓度）
            var bgColor = MixColor(Color.Parse("#1C1B1F"), primary, _mixIntensity);
            var surfaceColor = MixColor(Color.Parse("#2D2D3D"), primary, Math.Min(_mixIntensity * 1.3, 1.0));

            resources["MaterialSurfaceBrush"] = new SolidColorBrush(surfaceColor);
            resources["MaterialOnSurfaceBrush"] = new SolidColorBrush(Color.Parse("#E6E1E5"));
            resources["MaterialOnSurfaceVariantBrush"] = new SolidColorBrush(Color.Parse("#49454F"));
            resources["MaterialSurfaceVariantBrush"] = new SolidColorBrush(MixColor(Color.Parse("#49454F"), primary, Math.Min(_mixIntensity * 1.5, 1.0)));

            resources["MaterialBackgroundBrush"] = new SolidColorBrush(bgColor);
            resources["MaterialOnBackgroundBrush"] = new SolidColorBrush(Color.Parse("#E6E1E5"));

            resources["MaterialErrorBrush"] = new SolidColorBrush(Color.Parse("#F2B8B5"));
            resources["MaterialOnErrorBrush"] = new SolidColorBrush(Color.Parse("#601410"));
            resources["MaterialErrorContainerBrush"] = new SolidColorBrush(Color.Parse("#8C1D18"));
            resources["MaterialOnErrorContainerBrush"] = new SolidColorBrush(Color.Parse("#F9DEDC"));

            resources["MaterialOutlineBrush"] = new SolidColorBrush(MixColor(Color.Parse("#938F99"), primary, Math.Min(_mixIntensity * 2.0, 1.0)));
            resources["MaterialOutlineVariantBrush"] = new SolidColorBrush(MixColor(Color.Parse("#49454F"), primary, Math.Min(_mixIntensity * 1.8, 1.0)));
            resources["MaterialDividerBrush"] = new SolidColorBrush(MixColor(Color.Parse("#49454F"), primary, Math.Min(_mixIntensity * 1.5, 1.0)));

            resources["MaterialPaperBrush"] = new SolidColorBrush(MixColor(Color.Parse("#141218"), primary, _mixIntensity * 0.8));

            // 彩色模式特有的强调色资源
            resources["AccentBrush"] = new SolidColorBrush(primary);
            resources["AccentLightBrush"] = new SolidColorBrush(primaryLight);
            resources["AccentDarkBrush"] = new SolidColorBrush(primaryDark);
            resources["AccentContainerBrush"] = new SolidColorBrush(DarkenColor(primary, 0.3));
            resources["OnAccentBrush"] = new SolidColorBrush(onPrimary);

            // 圆角保持 Material Design 风格
            resources["CardCornerRadius"] = new CornerRadius(16);
            resources["ButtonCornerRadius"] = new CornerRadius(20);
            resources["InputCornerRadius"] = new CornerRadius(4);
            resources["OverlayOpacity"] = 0.12;
        }
        else
        {
            // 彩色模式 - 浅色主题
            resources["MaterialPrimaryBrush"] = new SolidColorBrush(primary);
            resources["MaterialOnPrimaryBrush"] = new SolidColorBrush(onPrimary);
            resources["MaterialPrimaryContainerBrush"] = new SolidColorBrush(LightenColor(primary, 0.3));
            resources["MaterialOnPrimaryContainerBrush"] = new SolidColorBrush(primaryDark);

            resources["MaterialSecondaryBrush"] = new SolidColorBrush(primaryDark);
            resources["MaterialOnSecondaryBrush"] = new SolidColorBrush(onPrimary);
            resources["MaterialSecondaryContainerBrush"] = new SolidColorBrush(LightenColor(primary, 0.4));
            resources["MaterialOnSecondaryContainerBrush"] = new SolidColorBrush(primaryDark);

            resources["MaterialTertiaryBrush"] = new SolidColorBrush(primaryDark);
            resources["MaterialOnTertiaryBrush"] = new SolidColorBrush(onPrimary);
            resources["MaterialTertiaryContainerBrush"] = new SolidColorBrush(LightenColor(primary, 0.5));
            resources["MaterialOnTertiaryContainerBrush"] = new SolidColorBrush(primaryDark);

            // 背景使用主题色的浅色调（使用动态混色浓度）
            var bgColor = MixColor(Color.Parse("#FFFBFE"), primary, _mixIntensity);
            var surfaceColor = MixColor(Color.Parse("#FFFFFF"), primary, Math.Min(_mixIntensity * 1.3, 1.0));

            resources["MaterialSurfaceBrush"] = new SolidColorBrush(surfaceColor);
            resources["MaterialOnSurfaceBrush"] = new SolidColorBrush(Color.Parse("#1C1B1F"));
            resources["MaterialOnSurfaceVariantBrush"] = new SolidColorBrush(Color.Parse("#49454F"));
            resources["MaterialSurfaceVariantBrush"] = new SolidColorBrush(MixColor(Color.Parse("#E7E0EC"), primary, Math.Min(_mixIntensity * 1.5, 1.0)));

            resources["MaterialBackgroundBrush"] = new SolidColorBrush(bgColor);
            resources["MaterialOnBackgroundBrush"] = new SolidColorBrush(Color.Parse("#1C1B1F"));

            resources["MaterialErrorBrush"] = new SolidColorBrush(Color.Parse("#B3261E"));
            resources["MaterialOnErrorBrush"] = new SolidColorBrush(Color.Parse("#FFFFFF"));
            resources["MaterialErrorContainerBrush"] = new SolidColorBrush(Color.Parse("#F9DEDC"));
            resources["MaterialOnErrorContainerBrush"] = new SolidColorBrush(Color.Parse("#410E0B"));

            resources["MaterialOutlineBrush"] = new SolidColorBrush(MixColor(Color.Parse("#79747E"), primary, Math.Min(_mixIntensity * 2.0, 1.0)));
            resources["MaterialOutlineVariantBrush"] = new SolidColorBrush(MixColor(Color.Parse("#C4C7C5"), primary, Math.Min(_mixIntensity * 1.8, 1.0)));
            resources["MaterialDividerBrush"] = new SolidColorBrush(MixColor(Color.Parse("#C4C7C5"), primary, Math.Min(_mixIntensity * 1.5, 1.0)));

            resources["MaterialPaperBrush"] = new SolidColorBrush(MixColor(Color.Parse("#F3EDF7"), primary, _mixIntensity * 0.8));

            // 彩色模式特有的强调色资源
            resources["AccentBrush"] = new SolidColorBrush(primary);
            resources["AccentLightBrush"] = new SolidColorBrush(primaryLight);
            resources["AccentDarkBrush"] = new SolidColorBrush(primaryDark);
            resources["AccentContainerBrush"] = new SolidColorBrush(LightenColor(primary, 0.3));
            resources["OnAccentBrush"] = new SolidColorBrush(onPrimary);

            // 圆角保持 Material Design 风格
            resources["CardCornerRadius"] = new CornerRadius(16);
            resources["ButtonCornerRadius"] = new CornerRadius(20);
            resources["InputCornerRadius"] = new CornerRadius(4);
            resources["OverlayOpacity"] = 0.08;
        }

        // 清除 Fluent Design 标志
        resources["IsFluentDesignEnabled"] = false;
    }

    /// <summary>
    /// 将颜色变亮
    /// </summary>
    private static Color LightenColor(Color color, double factor)
    {
        factor = Math.Clamp(factor, 0, 1);
        return Color.FromRgb(
            (byte)Math.Min(255, color.R + (255 - color.R) * factor),
            (byte)Math.Min(255, color.G + (255 - color.G) * factor),
            (byte)Math.Min(255, color.B + (255 - color.B) * factor)
        );
    }

    /// <summary>
    /// 将颜色变暗
    /// </summary>
    private static Color DarkenColor(Color color, double factor)
    {
        factor = Math.Clamp(factor, 0, 1);
        return Color.FromRgb(
            (byte)(color.R * (1 - factor)),
            (byte)(color.G * (1 - factor)),
            (byte)(color.B * (1 - factor))
        );
    }

    /// <summary>
    /// 混合两种颜色
    /// </summary>
    private static Color MixColor(Color baseColor, Color mixColor, double factor)
    {
        factor = Math.Clamp(factor, 0, 1);
        return Color.FromRgb(
            (byte)(baseColor.R * (1 - factor) + mixColor.R * factor),
            (byte)(baseColor.G * (1 - factor) + mixColor.G * factor),
            (byte)(baseColor.B * (1 - factor) + mixColor.B * factor)
        );
    }

    /// <summary>
    /// 获取对比色（用于文字）
    /// </summary>
    private static Color GetContrastColor(Color color)
    {
        // 计算亮度
        double luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;
        return luminance > 0.5 ? Color.Parse("#000000") : Color.Parse("#FFFFFF");
    }

    private void ApplyFluentDesignColors(Avalonia.Controls.IResourceDictionary resources)
    {
        if (_isDarkMode)
        {
            // Fluent Design Dark Mode - Win11 风格
            // 使用更柔和的色调，更高的透明度，更小的圆角
            resources["MaterialPrimaryBrush"] = new SolidColorBrush(Color.Parse("#60CDFF"));
            resources["MaterialOnPrimaryBrush"] = new SolidColorBrush(Color.Parse("#000000"));
            resources["MaterialPrimaryContainerBrush"] = new SolidColorBrush(Color.Parse("#093D55"));
            resources["MaterialOnPrimaryContainerBrush"] = new SolidColorBrush(Color.Parse("#C0EBFF"));

            resources["MaterialSecondaryBrush"] = new SolidColorBrush(Color.Parse("#A6A6A6"));
            resources["MaterialOnSecondaryBrush"] = new SolidColorBrush(Color.Parse("#000000"));
            resources["MaterialSecondaryContainerBrush"] = new SolidColorBrush(Color.Parse("#3A3A3A"));
            resources["MaterialOnSecondaryContainerBrush"] = new SolidColorBrush(Color.Parse("#E0E0E0"));

            resources["MaterialTertiaryBrush"] = new SolidColorBrush(Color.Parse("#FF99A3"));
            resources["MaterialOnTertiaryBrush"] = new SolidColorBrush(Color.Parse("#000000"));
            resources["MaterialTertiaryContainerBrush"] = new SolidColorBrush(Color.Parse("#5C2B32"));
            resources["MaterialOnTertiaryContainerBrush"] = new SolidColorBrush(Color.Parse("#FFD1D5"));

            // 背景使用亚克力效果的深色调
            resources["MaterialSurfaceBrush"] = new SolidColorBrush(Color.Parse("#202020"));
            resources["MaterialOnSurfaceBrush"] = new SolidColorBrush(Color.Parse("#FFFFFF"));
            resources["MaterialOnSurfaceVariantBrush"] = new SolidColorBrush(Color.Parse("#A0A0A0"));
            resources["MaterialSurfaceVariantBrush"] = new SolidColorBrush(Color.Parse("#2D2D2D"));

            resources["MaterialBackgroundBrush"] = new SolidColorBrush(Color.Parse("#1A1A1A"));
            resources["MaterialOnBackgroundBrush"] = new SolidColorBrush(Color.Parse("#FFFFFF"));

            resources["MaterialErrorBrush"] = new SolidColorBrush(Color.Parse("#FF99A3"));
            resources["MaterialOnErrorBrush"] = new SolidColorBrush(Color.Parse("#000000"));
            resources["MaterialErrorContainerBrush"] = new SolidColorBrush(Color.Parse("#5C2B32"));
            resources["MaterialOnErrorContainerBrush"] = new SolidColorBrush(Color.Parse("#FFD1D5"));

            resources["MaterialOutlineBrush"] = new SolidColorBrush(Color.Parse("#5A5A5A"));
            resources["MaterialOutlineVariantBrush"] = new SolidColorBrush(Color.Parse("#3D3D3D"));
            resources["MaterialDividerBrush"] = new SolidColorBrush(Color.Parse("#3D3D3D"));

            resources["MaterialPaperBrush"] = new SolidColorBrush(Color.Parse("#1E1E1E"));

            // Fluent Design 特有的资源 - 统一圆角标准
            resources["CardCornerRadius"] = new CornerRadius(8);
            resources["ButtonCornerRadius"] = new CornerRadius(4);
            resources["InputCornerRadius"] = new CornerRadius(4);
            resources["OverlayOpacity"] = 0.08;
        }
        else
        {
            // Fluent Design Light Mode - Win11 风格
            resources["MaterialPrimaryBrush"] = new SolidColorBrush(Color.Parse("#0078D4"));
            resources["MaterialOnPrimaryBrush"] = new SolidColorBrush(Color.Parse("#FFFFFF"));
            resources["MaterialPrimaryContainerBrush"] = new SolidColorBrush(Color.Parse("#E6F2FB"));
            resources["MaterialOnPrimaryContainerBrush"] = new SolidColorBrush(Color.Parse("#004578"));

            resources["MaterialSecondaryBrush"] = new SolidColorBrush(Color.Parse("#616161"));
            resources["MaterialOnSecondaryBrush"] = new SolidColorBrush(Color.Parse("#FFFFFF"));
            resources["MaterialSecondaryContainerBrush"] = new SolidColorBrush(Color.Parse("#F0F0F0"));
            resources["MaterialOnSecondaryContainerBrush"] = new SolidColorBrush(Color.Parse("#2D2D2D"));

            resources["MaterialTertiaryBrush"] = new SolidColorBrush(Color.Parse("#D83B01"));
            resources["MaterialOnTertiaryBrush"] = new SolidColorBrush(Color.Parse("#FFFFFF"));
            resources["MaterialTertiaryContainerBrush"] = new SolidColorBrush(Color.Parse("#FDE7E2"));
            resources["MaterialOnTertiaryContainerBrush"] = new SolidColorBrush(Color.Parse("#6A1B00"));

            // 背景使用更纯净的白色和浅灰色
            resources["MaterialSurfaceBrush"] = new SolidColorBrush(Color.Parse("#F9F9F9"));
            resources["MaterialOnSurfaceBrush"] = new SolidColorBrush(Color.Parse("#1A1A1A"));
            resources["MaterialOnSurfaceVariantBrush"] = new SolidColorBrush(Color.Parse("#616161"));
            resources["MaterialSurfaceVariantBrush"] = new SolidColorBrush(Color.Parse("#F3F3F3"));

            resources["MaterialBackgroundBrush"] = new SolidColorBrush(Color.Parse("#F2F2F2"));
            resources["MaterialOnBackgroundBrush"] = new SolidColorBrush(Color.Parse("#1A1A1A"));

            resources["MaterialErrorBrush"] = new SolidColorBrush(Color.Parse("#D83B01"));
            resources["MaterialOnErrorBrush"] = new SolidColorBrush(Color.Parse("#FFFFFF"));
            resources["MaterialErrorContainerBrush"] = new SolidColorBrush(Color.Parse("#FDE7E2"));
            resources["MaterialOnErrorContainerBrush"] = new SolidColorBrush(Color.Parse("#6A1B00"));

            resources["MaterialOutlineBrush"] = new SolidColorBrush(Color.Parse("#E0E0E0"));
            resources["MaterialOutlineVariantBrush"] = new SolidColorBrush(Color.Parse("#EBEBEB"));
            resources["MaterialDividerBrush"] = new SolidColorBrush(Color.Parse("#EBEBEB"));

            resources["MaterialPaperBrush"] = new SolidColorBrush(Color.Parse("#FFFFFF"));

            // Fluent Design 特有的资源 - 统一圆角标准
            resources["CardCornerRadius"] = new CornerRadius(8);
            resources["ButtonCornerRadius"] = new CornerRadius(4);
            resources["InputCornerRadius"] = new CornerRadius(4);
            resources["OverlayOpacity"] = 0.04;
        }

        // 设置 Fluent Design 标志
        resources["IsFluentDesignEnabled"] = true;
    }

    private void ApplyMaterialDesignColors(Avalonia.Controls.IResourceDictionary resources)
    {
        if (_isDarkMode)
        {
            // Dark Mode Colors - Material Design 3
            resources["MaterialPrimaryBrush"] = new SolidColorBrush(Color.Parse("#D0BCFF"));
            resources["MaterialOnPrimaryBrush"] = new SolidColorBrush(Color.Parse("#381E72"));
            resources["MaterialPrimaryContainerBrush"] = new SolidColorBrush(Color.Parse("#4F378B"));
            resources["MaterialOnPrimaryContainerBrush"] = new SolidColorBrush(Color.Parse("#EADDFF"));

            resources["MaterialSecondaryBrush"] = new SolidColorBrush(Color.Parse("#CCC2DC"));
            resources["MaterialOnSecondaryBrush"] = new SolidColorBrush(Color.Parse("#332D41"));
            resources["MaterialSecondaryContainerBrush"] = new SolidColorBrush(Color.Parse("#4A4458"));
            resources["MaterialOnSecondaryContainerBrush"] = new SolidColorBrush(Color.Parse("#E8DEF8"));

            resources["MaterialTertiaryBrush"] = new SolidColorBrush(Color.Parse("#EFB8C8"));
            resources["MaterialOnTertiaryBrush"] = new SolidColorBrush(Color.Parse("#492532"));
            resources["MaterialTertiaryContainerBrush"] = new SolidColorBrush(Color.Parse("#633B48"));
            resources["MaterialOnTertiaryContainerBrush"] = new SolidColorBrush(Color.Parse("#FFD8E4"));

            resources["MaterialSurfaceBrush"] = new SolidColorBrush(Color.Parse("#1C1B1F"));
            resources["MaterialOnSurfaceBrush"] = new SolidColorBrush(Color.Parse("#E6E1E5"));
            resources["MaterialOnSurfaceVariantBrush"] = new SolidColorBrush(Color.Parse("#49454F"));
            resources["MaterialSurfaceVariantBrush"] = new SolidColorBrush(Color.Parse("#49454F"));

            resources["MaterialBackgroundBrush"] = new SolidColorBrush(Color.Parse("#1C1B1F"));
            resources["MaterialOnBackgroundBrush"] = new SolidColorBrush(Color.Parse("#E6E1E5"));

            resources["MaterialErrorBrush"] = new SolidColorBrush(Color.Parse("#F2B8B5"));
            resources["MaterialOnErrorBrush"] = new SolidColorBrush(Color.Parse("#601410"));
            resources["MaterialErrorContainerBrush"] = new SolidColorBrush(Color.Parse("#8C1D18"));
            resources["MaterialOnErrorContainerBrush"] = new SolidColorBrush(Color.Parse("#F9DEDC"));

            resources["MaterialOutlineBrush"] = new SolidColorBrush(Color.Parse("#938F99"));
            resources["MaterialOutlineVariantBrush"] = new SolidColorBrush(Color.Parse("#49454F"));
            resources["MaterialDividerBrush"] = new SolidColorBrush(Color.Parse("#49454F"));

            resources["MaterialPaperBrush"] = new SolidColorBrush(Color.Parse("#141218"));

            // Material Design 特有的资源 - 统一圆角标准
            resources["CardCornerRadius"] = new CornerRadius(16);
            resources["ButtonCornerRadius"] = new CornerRadius(20);
            resources["InputCornerRadius"] = new CornerRadius(4);
            resources["OverlayOpacity"] = 0.12;
        }
        else
        {
            // Light Mode Colors - Material Design 3
            resources["MaterialPrimaryBrush"] = new SolidColorBrush(Color.Parse("#6750A4"));
            resources["MaterialOnPrimaryBrush"] = new SolidColorBrush(Color.Parse("#FFFFFF"));
            resources["MaterialPrimaryContainerBrush"] = new SolidColorBrush(Color.Parse("#EADDFF"));
            resources["MaterialOnPrimaryContainerBrush"] = new SolidColorBrush(Color.Parse("#21005D"));

            resources["MaterialSecondaryBrush"] = new SolidColorBrush(Color.Parse("#625B71"));
            resources["MaterialOnSecondaryBrush"] = new SolidColorBrush(Color.Parse("#FFFFFF"));
            resources["MaterialSecondaryContainerBrush"] = new SolidColorBrush(Color.Parse("#E8DEF8"));
            resources["MaterialOnSecondaryContainerBrush"] = new SolidColorBrush(Color.Parse("#1D192B"));

            resources["MaterialTertiaryBrush"] = new SolidColorBrush(Color.Parse("#7D5260"));
            resources["MaterialOnTertiaryBrush"] = new SolidColorBrush(Color.Parse("#FFFFFF"));
            resources["MaterialTertiaryContainerBrush"] = new SolidColorBrush(Color.Parse("#FFD8E4"));
            resources["MaterialOnTertiaryContainerBrush"] = new SolidColorBrush(Color.Parse("#31111D"));

            resources["MaterialSurfaceBrush"] = new SolidColorBrush(Color.Parse("#FFFBFE"));
            resources["MaterialOnSurfaceBrush"] = new SolidColorBrush(Color.Parse("#1C1B1F"));
            resources["MaterialOnSurfaceVariantBrush"] = new SolidColorBrush(Color.Parse("#49454F"));
            resources["MaterialSurfaceVariantBrush"] = new SolidColorBrush(Color.Parse("#E7E0EC"));

            resources["MaterialBackgroundBrush"] = new SolidColorBrush(Color.Parse("#FFFBFE"));
            resources["MaterialOnBackgroundBrush"] = new SolidColorBrush(Color.Parse("#1C1B1F"));

            resources["MaterialErrorBrush"] = new SolidColorBrush(Color.Parse("#B3261E"));
            resources["MaterialOnErrorBrush"] = new SolidColorBrush(Color.Parse("#FFFFFF"));
            resources["MaterialErrorContainerBrush"] = new SolidColorBrush(Color.Parse("#F9DEDC"));
            resources["MaterialOnErrorContainerBrush"] = new SolidColorBrush(Color.Parse("#410E0B"));

            resources["MaterialOutlineBrush"] = new SolidColorBrush(Color.Parse("#79747E"));
            resources["MaterialOutlineVariantBrush"] = new SolidColorBrush(Color.Parse("#C4C7C5"));
            resources["MaterialDividerBrush"] = new SolidColorBrush(Color.Parse("#C4C7C5"));

            resources["MaterialPaperBrush"] = new SolidColorBrush(Color.Parse("#F3EDF7"));

            // Material Design 特有的资源 - 统一圆角标准
            resources["CardCornerRadius"] = new CornerRadius(16);
            resources["ButtonCornerRadius"] = new CornerRadius(20);
            resources["InputCornerRadius"] = new CornerRadius(4);
            resources["OverlayOpacity"] = 0.08;
        }

        // 清除 Fluent Design 标志
        resources["IsFluentDesignEnabled"] = false;
    }
}
