using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MinecraftConnectTool.ViewModels.Pages;

namespace MinecraftConnectTool.Views.Pages;

public partial class ETPage : UserControl
{
    public ETPage()
    {
        InitializeComponent();
        DataContext = new ETPageViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
