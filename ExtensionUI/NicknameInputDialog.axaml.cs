using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace MinecraftConnectTool.ExtensionUI;

public partial class NicknameInputDialog : UserControl
{
    private Window? _parentWindow;
    private TaskCompletionSource<string?>? _tcs;

    public NicknameInputDialog()
    {
        InitializeComponent();

        var skipButton = this.FindControl<Button>("SkipButton");
        var cancelButton = this.FindControl<Button>("CancelButton");
        var confirmButton = this.FindControl<Button>("ConfirmButton");
        var nicknameTextBox = this.FindControl<TextBox>("NicknameTextBox");

        skipButton!.Click += OnSkipClick;
        cancelButton!.Click += OnCancelClick;
        confirmButton!.Click += OnConfirmClick;

        // 回车确认
        nicknameTextBox!.KeyDown += (s, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter)
            {
                OnConfirmClick(s, e);
            }
        };

        // 自动聚焦
        Loaded += (s, e) => nicknameTextBox.Focus();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// 显示对话框并等待用户输入
    /// </summary>
    public static async Task<string?> ShowAsync(Window parent)
    {
        var dialog = new NicknameInputDialog();
        dialog._tcs = new TaskCompletionSource<string?>();

        // 创建对话框窗口
        var dialogWindow = new Window
        {
            Width = 440,
            Height = 280,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            CanResize = false,
            ShowInTaskbar = false,
            SystemDecorations = SystemDecorations.None,
            Background = Avalonia.Media.Brushes.Transparent,
            Content = dialog
        };
        
        // 显示对话框
        dialogWindow.Show();

        dialog._parentWindow = dialogWindow;

        // 等待结果
        var result = await dialog._tcs.Task;
        
        // 关闭窗口
        dialogWindow.Close();

        return result;
    }

    private void OnSkipClick(object? sender, RoutedEventArgs e)
    {
        // 使用电脑名称作为昵称
        var computerName = Environment.MachineName;
        _tcs?.TrySetResult(computerName);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        _tcs?.TrySetResult(null);
    }

    private void OnConfirmClick(object? sender, RoutedEventArgs e)
    {
        var nicknameTextBox = this.FindControl<TextBox>("NicknameTextBox");
        var nickname = nicknameTextBox?.Text?.Trim();

        if (string.IsNullOrWhiteSpace(nickname))
        {
            // 显示错误提示
            return;
        }

        _tcs?.TrySetResult(nickname);
    }
}
