using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Material.Icons;
using MinecraftConnectTool.Services;

namespace MinecraftConnectTool.ExtensionUI;

public partial class MD3InputDialog : UserControl
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<MD3InputDialog, string>(nameof(Title), "请输入");

    public static readonly StyledProperty<string> PromptTextProperty =
        AvaloniaProperty.Register<MD3InputDialog, string>(nameof(PromptText), "");

    public static readonly StyledProperty<string> PlaceholderTextProperty =
        AvaloniaProperty.Register<MD3InputDialog, string>(nameof(PlaceholderText), "请输入内容");

    public static readonly StyledProperty<string> InputTextProperty =
        AvaloniaProperty.Register<MD3InputDialog, string>(nameof(InputText), "");

    public static readonly StyledProperty<MaterialIconKind> IconKindProperty =
        AvaloniaProperty.Register<MD3InputDialog, MaterialIconKind>(nameof(IconKind), MaterialIconKind.Pencil);

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string PromptText
    {
        get => GetValue(PromptTextProperty);
        set => SetValue(PromptTextProperty, value);
    }

    public string PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public string InputText
    {
        get => GetValue(InputTextProperty);
        set => SetValue(InputTextProperty, value);
    }

    public MaterialIconKind IconKind
    {
        get => GetValue(IconKindProperty);
        set => SetValue(IconKindProperty, value);
    }

    private bool _isConfirmed;

    public MD3InputDialog()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// 显示输入对话框（类似AntdUI Modal）
    /// </summary>
    /// <param name="parent">父窗口</param>
    /// <param name="promptText">提示文本</param>
    /// <param name="title">标题</param>
    /// <param name="placeholder">输入框占位符</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>用户输入的文本，如果取消则返回null</returns>
    public static async Task<string?> ShowAsync(
        Window parent,
        string promptText,
        string title = "请输入",
        string placeholder = "请输入内容",
        string defaultValue = "")
    {
        // 创建遮罩层窗口
        var overlayWindow = new Window
        {
            Width = parent.Width,
            Height = parent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            SystemDecorations = SystemDecorations.None,
            Background = new SolidColorBrush(Color.Parse(ThemeService.Instance.IsDarkMode ? "#B3000000" : "#B3FFFFFF")),
            TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent },
            ShowInTaskbar = false,
            Opacity = 0
        };

        // 创建对话框窗口
        var dialogWindow = new Window
        {
            Width = 316,
            Height = double.NaN,
            MinHeight = 180,
            MaxHeight = 300,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            SystemDecorations = SystemDecorations.None,
            Background = Brushes.Transparent,
            TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent },
            ShowInTaskbar = false
        };

        var dialogContent = new MD3InputDialog
        {
            Title = title,
            PromptText = promptText,
            PlaceholderText = placeholder,
            InputText = defaultValue
        };

        // 绑定输入框
        var inputTextBox = dialogContent.FindControl<TextBox>("InputTextBox");
        if (inputTextBox != null)
        {
            inputTextBox.Text = defaultValue;
            inputTextBox.KeyDown += (sender, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    dialogContent._isConfirmed = true;
                    dialogWindow.Close();
                }
                else if (e.Key == Key.Escape)
                {
                    dialogWindow.Close();
                }
            };
        }

        // 关闭按钮事件
        var closeButton = dialogContent.FindControl<Button>("CloseButton");
        if (closeButton != null)
        {
            closeButton.Click += (_, _) => dialogWindow.Close();
        }

        // 取消按钮事件
        var cancelButton = dialogContent.FindControl<Button>("CancelButton");
        if (cancelButton != null)
        {
            cancelButton.Click += (_, _) => dialogWindow.Close();
        }

        // 确定按钮事件
        var confirmButton = dialogContent.FindControl<Button>("ConfirmButton");
        if (confirmButton != null)
        {
            confirmButton.Click += (_, _) =>
            {
                dialogContent._isConfirmed = true;
                dialogWindow.Close();
            };
        }

        dialogWindow.Content = dialogContent;

        // 显示遮罩层并淡入
        overlayWindow.Show(parent);
        await Task.Delay(10);
        await AnimateOpacityAsync(overlayWindow, 0, 1, 150);

        // 显示对话框
        await dialogWindow.ShowDialog(overlayWindow);

        // 获取输入结果
        string? result = dialogContent._isConfirmed ? inputTextBox?.Text : null;

        // 关闭遮罩层并淡出
        await AnimateOpacityAsync(overlayWindow, 1, 0, 150);
        overlayWindow.Close();

        return result;
    }

    /// <summary>
    /// 简单的透明度动画
    /// </summary>
    private static async Task AnimateOpacityAsync(Window window, double from, double to, int durationMs)
    {
        var startTime = DateTime.Now;
        while (true)
        {
            var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
            var progress = Math.Min(elapsed / durationMs, 1.0);
            window.Opacity = from + (to - from) * progress;

            if (progress >= 1.0) break;
            await Task.Delay(16);
        }
    }
}
