using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MinecraftConnectTool.ViewModels.RightPage;

namespace MinecraftConnectTool.Views.RightPage;

public partial class PanelInviteEdit : UserControl
{
    // 关闭事件
    public event EventHandler? CloseRequested;

    public PanelInviteEdit()
    {
        InitializeComponent();
        DataContext = new PanelInviteEditViewModel();

        Loaded += OnLoaded;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // 绑定返回按钮事件
        var backButton = this.FindControl<Button>("BackButton");
        if (backButton != null)
        {
            backButton.Click += (s, ev) =>
            {
                // 触发关闭事件
                CloseRequested?.Invoke(this, EventArgs.Empty);
            };
        }

        // 加载数据
        if (DataContext is PanelInviteEditViewModel vm)
        {
            vm.LoadSettings();
        }
    }
}
