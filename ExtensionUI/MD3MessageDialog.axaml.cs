using System;
using System.Collections.Generic;
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
    public static async Task ShowAsync(Window parent, string message, string title = "提示",
        MaterialIconKind icon = MaterialIconKind.InformationCircle)
    {
        await ShowCoreAsync(parent, message, title, icon);
    }

    public static Task<bool> ConfirmAsync(Window parent, string message, string title, string confirmText,
        bool expanded = false, string? cancelText = null, bool isCloseButtonEnabled = true, string? emphasisText = null)
        => ShowCoreAsync(parent, message, title, MaterialIconKind.InformationCircle, confirmText,
            expanded: expanded, cancelText: cancelText, isCloseButtonEnabled: isCloseButtonEnabled, emphasisText: emphasisText);

    internal static Task<bool> ShowJoinReportAsync(Window parent, string title, string summary,
        IReadOnlyList<MD3MessageSection> sections, bool warning)
    {
        var icon = warning ? MaterialIconKind.AlertCircle : MaterialIconKind.InformationCircle;
        return ShowCoreAsync(parent, summary, title, icon, "继续加入", sections, expanded: true, cancelText: "取消加入");
    }

    private static async Task<bool> ShowCoreAsync(Window parent, string message, string title,
        MaterialIconKind icon, string? confirmText = null, IReadOnlyList<MD3MessageSection>? sections = null,
        bool expanded = false, string? cancelText = null, bool isCloseButtonEnabled = true, string? emphasisText = null)
    {
        // 计算对话框尺寸 - 宽度为父窗口的37.8%（总共缩小37%），最小302最大454
        double dialogWidth = Math.Max(302, Math.Min(454, parent.Width * 0.378));
        double dialogMaxHeight = 315;
        if (expanded)
        {
            var isReport = sections != null;
            dialogWidth = isReport
                ? Math.Clamp(parent.Bounds.Width * 0.86, 580, 760)
                : Math.Clamp(parent.Bounds.Width * 0.70, 540, 640);
            dialogMaxHeight = isReport ? 680 : 480;

            var screen = parent.Screens.ScreenFromWindow(parent) ?? parent.Screens.Primary;
            if (screen != null)
            {
                var workingWidth = screen.WorkingArea.Width / screen.Scaling;
                var workingHeight = screen.WorkingArea.Height / screen.Scaling;
                dialogWidth = Math.Clamp(workingWidth - 48, 302, dialogWidth);
                dialogMaxHeight = Math.Clamp(workingHeight - 64, 230, dialogMaxHeight);
            }
        }
        
        // 根据主题设置背景色
        var backgroundColor = ThemeService.Instance.IsDarkMode 
            ? Color.Parse("#FF1F1F1F")  // 暗色背景
            : Color.Parse("#FFF5F5F5"); // 亮色背景
        
        var dialogWindow = new Window
        {
            Width = dialogWidth,
            Height = double.NaN, // Auto
            MinHeight = 126,
            MaxHeight = dialogMaxHeight,
            SizeToContent = expanded ? SizeToContent.Height : SizeToContent.Manual,
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

        if (expanded && dialogContent.FindControl<ScrollViewer>("MessageScrollViewer") is { } messageScroll)
            messageScroll.MaxHeight = dialogMaxHeight - 144;

        if (!string.IsNullOrWhiteSpace(emphasisText) && dialogContent.FindControl<TextBlock>("EmphasisText") is { } emphasis)
        {
            emphasis.Text = emphasisText;
            emphasis.IsVisible = true;
        }

        if (sections != null)
        {
            var messageText = dialogContent.FindControl<TextBlock>("MessageText")!;
            var reportSections = dialogContent.FindControl<ItemsControl>("ReportSections")!;
            messageText.IsVisible = message.Length != 0;
            reportSections.ItemsSource = sections;
            reportSections.IsVisible = true;
        }

        // 关闭按钮事件
        var closeButton = dialogContent.FindControl<Button>("CloseButton");
        if (closeButton != null)
        {
            closeButton.IsEnabled = isCloseButtonEnabled;
            if (isCloseButtonEnabled)
                closeButton.Click += (_, _) => dialogWindow.Close(false);
            else
                closeButton.Opacity = 0.45;
        }

        // 确定按钮事件
        var confirmButton = dialogContent.FindControl<Button>("ConfirmButton");
        if (confirmButton != null)
        {
            if (confirmText != null)
                confirmButton.Content = confirmText;
            confirmButton.Click += (_, _) => dialogWindow.Close(true);
        }

        if (confirmText != null && dialogContent.FindControl<Button>("CancelButton") is { } cancelButton)
        {
            if (cancelText != null)
                cancelButton.Content = cancelText;
            cancelButton.IsVisible = true;
            cancelButton.Click += (_, _) => dialogWindow.Close(false);
            dialogWindow.Opened += (_, _) => cancelButton.Focus();
        }

        dialogWindow.Content = dialogContent;
        dialogWindow.Icon = parent.Icon;

        return await dialogWindow.ShowDialog<bool?>(parent) == true;
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

internal sealed record MD3MessageSection(string Title, IReadOnlyList<MD3MessageRow> Rows);

internal sealed record MD3MessageRow(string Text, string? Details = null)
{
    public bool HasDetails => !string.IsNullOrEmpty(Details);
}
