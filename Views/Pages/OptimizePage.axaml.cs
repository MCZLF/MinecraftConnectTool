using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MinecraftConnectTool.ViewModels.Pages;

namespace MinecraftConnectTool.Views.Pages;

public partial class OptimizePage : UserControl
{
    public OptimizePage()
    {
        InitializeComponent();
        DataContext = new OptimizePageViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
