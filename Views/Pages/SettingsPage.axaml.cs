using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using MinecraftConnectTool.ViewModels.Pages;

namespace MinecraftConnectTool.Views.Pages;

public partial class SettingsPage : UserControl
{
    private const double WheelScrollStep = 48;

    public SettingsPage()
    {
        InitializeComponent();
        DataContext = new SettingsPageViewModel();
        DisableComboBoxWheelSelection();
    }

    private void DisableComboBoxWheelSelection()
    {
        foreach (var comboBox in this.GetLogicalDescendants().OfType<ComboBox>())
        {
            comboBox.AddHandler(PointerWheelChangedEvent, OnComboBoxPointerWheelChanged, RoutingStrategies.Tunnel);
        }
    }

    private void OnComboBoxPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (sender is not ComboBox comboBox || comboBox.IsDropDownOpen)
        {
            return;
        }

        e.Handled = true;

        var scrollViewer = comboBox.FindAncestorOfType<ScrollViewer>();
        if (scrollViewer is null || e.Delta.Y == 0)
        {
            return;
        }

        var maxOffsetY = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);
        var targetOffsetY = Math.Clamp(scrollViewer.Offset.Y - e.Delta.Y * WheelScrollStep, 0, maxOffsetY);
        scrollViewer.Offset = new Vector(scrollViewer.Offset.X, targetOffsetY);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
