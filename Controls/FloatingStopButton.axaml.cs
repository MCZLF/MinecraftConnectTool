using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace MinecraftConnectTool.Controls;

public partial class FloatingStopButton : UserControl
{
    private Button? _stopButton;

    // 定义停止命令事件（向后兼容）
    public event EventHandler? StopRequested;

    public static readonly StyledProperty<bool> IsP2PRunningProperty =
        AvaloniaProperty.Register<FloatingStopButton, bool>(nameof(IsP2PRunning));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<FloatingStopButton, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<FloatingStopButton, object?>(nameof(CommandParameter));

    public static readonly StyledProperty<string> ToolTipTextProperty =
        AvaloniaProperty.Register<FloatingStopButton, string>(nameof(ToolTipText), "关闭核心");

    public bool IsP2PRunning
    {
        get => GetValue(IsP2PRunningProperty);
        set => SetValue(IsP2PRunningProperty, value);
    }

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public string ToolTipText
    {
        get => GetValue(ToolTipTextProperty);
        set => SetValue(ToolTipTextProperty, value);
    }

    public FloatingStopButton()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        _stopButton = this.FindControl<Button>("StopButton");
        if (_stopButton != null)
        {
            _stopButton.Click += OnStopButtonClick;
            // 初始化ToolTip
            ToolTip.SetTip(_stopButton, ToolTipText);
        }

        // 初始状态
        UpdateVisibility();
    }

    private void OnStopButtonClick(object? sender, RoutedEventArgs e)
    {
        // 优先执行Command
        if (Command != null)
        {
            if (Command.CanExecute(CommandParameter))
            {
                Command.Execute(CommandParameter);
            }
        }
        // 触发事件（向后兼容）
        StopRequested?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateVisibility()
    {
        IsVisible = IsP2PRunning;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        
        if (change.Property == IsP2PRunningProperty)
        {
            UpdateVisibility();
        }
        else if (change.Property == ToolTipTextProperty)
        {
            UpdateToolTip();
        }
    }

    private void UpdateToolTip()
    {
        if (_stopButton != null)
        {
            ToolTip.SetTip(_stopButton, ToolTipText);
        }
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        
        if (_stopButton != null)
        {
            _stopButton.Click -= OnStopButtonClick;
        }
    }
}
