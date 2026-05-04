using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MinecraftConnectTool.ViewModels.Pages;

namespace MinecraftConnectTool.Views.Pages;

public partial class UpdatePage : UserControl
{
    private readonly UpdatePageViewModel? _viewModel;

    public UpdatePage()
    {
        InitializeComponent();
        _viewModel = new UpdatePageViewModel();
        DataContext = _viewModel;
        
        // 订阅加载事件
        Loaded += OnLoaded;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // 页面加载时自动检查更新
        _viewModel?.OnPageLoaded();
    }
}
