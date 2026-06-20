using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MinecraftConnectTool.ViewModels.Pages;

namespace MinecraftConnectTool.Views.Pages;

public partial class OptimizePage : UserControl, IDisposable
{
    private readonly OptimizePageViewModel _viewModel;

    public OptimizePage()
    {
        InitializeComponent();
        _viewModel = new OptimizePageViewModel();
        DataContext = _viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void Dispose()
    {
        _viewModel.Dispose();
    }
}
