using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Material.Icons;
using System.Threading.Tasks;
using MinecraftConnectTool.Services;

namespace MinecraftConnectTool.ExtensionUI;

public partial class MD3MessageDialog : UserControl
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<MD3MessageDialog, string>(nameof(Title), "提示");

    public static readonly StyledProperty<string> MessageProperty =
        AvaloniaProperty.Register<MD3MessageDialog, string>(nameof(Message), "");

    public static readonly StyledProperty<MaterialIconKind> IconKindProperty =
        AvaloniaProperty.Register<MD3MessageDialog, MaterialIconKind>(nameof(IconKind), MaterialIconKind.InformationCircle);

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public MaterialIconKind IconKind
    {
        get => GetValue(IconKindProperty);
        set => SetValue(IconKindProperty, value);
    }

    public MD3MessageDialog()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// 显示消息对话框（类似Windows提示框）
    /// </summary>
    /// <param name="parent">父窗口</param>
    /// <param name="message">消息内容</param>
    /// <param name="title">标题</param>
    /// <param name="icon">图标类型</param>
    public static async Task ShowAsync(Window parent, string message, string title = "提示", MaterialIconKind icon = MaterialIconKind.InformationCircle)
    {
        // 计算对话框尺寸 - 宽度为父窗口的37.8%（总共缩小37%），最小302最大454
        double dialogWidth = Math.Max(302, Math.Min(454, parent.Width * 0.378));
        
        // 根据主题设置背景色
        var backgroundColor = ThemeService.Instance.IsDarkMode 
            ? Color.Parse("#FF1F1F1F")  // 暗色背景
            : Color.Parse("#FFF5F5F5"); // 亮色背景
        
        var dialogWindow = new Window
        {
            Width = dialogWidth,
            Height = double.NaN, // Auto
            MinHeight = 126,
            MaxHeight = 315,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            SystemDecorations = SystemDecorations.None,
            Background = new SolidColorBrush(backgroundColor),
            TransparencyLevelHint = new[] { WindowTransparencyLevel.None },
            ExtendClientAreaToDecorationsHint = true,
            ShowInTaskbar = false
        };

        var dialogContent = new MD3MessageDialog
        {
            Title = title,
            Message = message,
            IconKind = icon
        };

        // 关闭按钮事件
        var closeButton = dialogContent.FindControl<Button>("CloseButton");
        if (closeButton != null)
        {
            closeButton.Click += (_, _) => dialogWindow.Close();
        }

        // 确定按钮事件
        var confirmButton = dialogContent.FindControl<Button>("ConfirmButton");
        if (confirmButton != null)
        {
            confirmButton.Click += (_, _) => dialogWindow.Close();
        }

        dialogWindow.Content = dialogContent;
        dialogWindow.Icon = parent.Icon;

        await dialogWindow.ShowDialog(parent);
    }

    /// <summary>
    /// 显示信息提示
    /// </summary>
    public static Task ShowInfoAsync(Window parent, string message, string title = "提示")
    {
        return ShowAsync(parent, message, title, MaterialIconKind.InformationCircle);
    }

    /// <summary>
    /// 显示成功提示
    /// </summary>
    public static Task ShowSuccessAsync(Window parent, string message, string title = "成功")
    {
        return ShowAsync(parent, message, title, MaterialIconKind.CheckCircle);
    }

    /// <summary>
    /// 显示警告提示
    /// </summary>
    public static Task ShowWarningAsync(Window parent, string message, string title = "警告")
    {
        return ShowAsync(parent, message, title, MaterialIconKind.AlertCircle);
    }

    /// <summary>
    /// 显示错误提示
    /// </summary>
    public static Task ShowErrorAsync(Window parent, string message, string title = "错误")
    {
        return ShowAsync(parent, message, title, MaterialIconKind.CloseCircle);
    }
}
