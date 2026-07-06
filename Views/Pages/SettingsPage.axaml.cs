using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using MinecraftConnectTool.ViewModels.Pages;

namespace MinecraftConnectTool.Views.Pages;

public partial class SettingsPage : UserControl
{
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
        if (sender is ComboBox comboBox && !comboBox.IsDropDownOpen)
        {
            e.Handled = true;
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
