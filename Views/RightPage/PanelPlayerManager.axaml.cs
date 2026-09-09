using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MinecraftConnectTool.ViewModels.RightPage;

namespace MinecraftConnectTool.Views.RightPage;

public partial class PanelPlayerManager : UserControl, IDisposable
{
    private readonly PanelPlayerManagerViewModel _viewModel;

    public event EventHandler? CloseRequested;

    public PanelPlayerManager()
    {
        InitializeComponent();
        _viewModel = new PanelPlayerManagerViewModel();
        DataContext = _viewModel;
        _viewModel.CloseRequested += OnViewModelCloseRequested;
        DetachedFromVisualTree += OnDetachedFromVisualTree;
    }

    private void OnViewModelCloseRequested(object? sender, EventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnDetachedFromVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        Dispose();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void Dispose()
    {
        DetachedFromVisualTree -= OnDetachedFromVisualTree;
        _viewModel.CloseRequested -= OnViewModelCloseRequested;
        _viewModel.Dispose();
    }
}
