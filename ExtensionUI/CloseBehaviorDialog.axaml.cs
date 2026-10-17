using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace MinecraftConnectTool.ExtensionUI;

public sealed record CloseBehaviorDialogResult(string Behavior, bool RememberChoice);

public partial class CloseBehaviorDialog : UserControl
{
    private readonly TaskCompletionSource<CloseBehaviorDialogResult?> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public CloseBehaviorDialog()
    {
        AvaloniaXamlLoader.Load(this);
        var rememberChoice = this.FindControl<CheckBox>("RememberChoiceCheckBox")!;
        DrawnCheckMark? checkMark = null;
        rememberChoice.TemplateApplied += (_, e) =>
        {
            if (e.NameScope.Find<Avalonia.Controls.Shapes.Path>("Graphic")?.Parent is Panel panel)
            {
                checkMark = new DrawnCheckMark { Progress = rememberChoice.IsChecked == true ? 1 : 0 };
                panel.Children.Insert(1, checkMark);
            }
        };
        rememberChoice.PropertyChanged += (_, e) =>
        {
            if (e.Property == CheckBox.IsCheckedProperty && checkMark != null)
                checkMark.Progress = rememberChoice.IsChecked == true ? 1 : 0;
        };
        if (!OperatingSystem.IsWindows())
        {
            this.FindControl<Button>("HideButton")!.Content = "最小化窗口";
            this.FindControl<TextBlock>("HideDescription")!.Text = "最小化后联机核心将继续运行，可通过系统窗口入口恢复。";
        }
    }

    public static async Task<CloseBehaviorDialogResult?> ShowAsync(Window owner, IReadOnlyList<string> activeCoreNames)
    {
        if (owner.Content is not Grid root)
            throw new InvalidOperationException("主窗口缺少全局弹层容器。");

        var content = new CloseBehaviorDialog();
        content.FindControl<TextBlock>("CoreRunningMessage")!.Text =
            $"[{string.Join("/", activeCoreNames)}]模式的联机核心仍在运行，你希望如何处理？";
        var previousFocus = owner.FocusManager?.GetFocusedElement();
        var disabledControls = new System.Collections.Generic.List<Control>();
        foreach (var child in root.Children)
        {
            if (child.IsEnabled)
            {
                disabledControls.Add(child);
                child.IsEnabled = false;
            }
        }

        content.ZIndex = 20000;
        void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                content._completion.TrySetResult(null);
            }
        }
        void OnOwnerClosed(object? sender, EventArgs e) => content._completion.TrySetResult(null);

        owner.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        owner.Closed += OnOwnerClosed;
        root.Children.Add(content);
        content.FindControl<Button>("HideButton")?.Focus();
        try
        {
            return await content._completion.Task;
        }
        finally
        {
            root.Children.Remove(content);
            owner.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
            owner.Closed -= OnOwnerClosed;
            foreach (var control in disabledControls)
                control.IsEnabled = true;
            previousFocus?.Focus();
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        _completion.TrySetResult(null);
    }

    private void OnHideClick(object? sender, RoutedEventArgs e)
    {
        Complete("MinimizeToTray");
    }

    private void OnExitClick(object? sender, RoutedEventArgs e)
    {
        Complete("Exit");
    }

    private void Complete(string behavior)
    {
        var remember = this.FindControl<CheckBox>("RememberChoiceCheckBox")?.IsChecked == true;
        _completion.TrySetResult(new CloseBehaviorDialogResult(behavior, remember));
    }
}
