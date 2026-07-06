using System;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MinecraftConnectTool.ViewModels;
using MinecraftConnectTool.ViewModels.Pages;
using MinecraftConnectTool.Views;

namespace MinecraftConnectTool.Views.Pages;

public partial class HomePage : UserControl
{
    public HomePage()
    {
        InitializeComponent();
        DataContext = new HomePageViewModel();
        
        Loaded += OnLoaded;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _ = LoadCloudAlertAsync();
    }

    private void OnCardPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control || control.Tag is not string pageKey)
            return;

        if (e.Pointer.Type == PointerType.Mouse)
        {
            var properties = e.GetCurrentPoint(this).Properties;
            if (!properties.IsLeftButtonPressed)
                return;
        }

        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow?.DataContext is MainWindowViewModel viewModel)
        {
            viewModel.NavigateToCommand.Execute(pageKey);
        }
    }

    private void OnCloudAlertPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Pointer.Type == PointerType.Mouse)
        {
            var properties = e.GetCurrentPoint(this).Properties;
            if (!properties.IsLeftButtonPressed)
                return;
        }

        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is MainWindow mainWindow)
        {
            mainWindow.ShowPanelAlert();
        }
    }

    private async Task LoadCloudAlertAsync()
    {
        var cloudAlertBorder = this.FindControl<Border>("CloudAlertBorder");
        var cloudAlertText = this.FindControl<TextBlock>("CloudAlertText");
        
        if (cloudAlertBorder == null || cloudAlertText == null) return;

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var text = await client.GetStringAsync("https://api.mct.mczlf.loft.games/cloudalert");
            
            if (!string.IsNullOrWhiteSpace(text))
            {
                cloudAlertText.Text = text.Trim();
                cloudAlertBorder.IsVisible = true;
            }
        }
        catch
        {
            // 获取失败时不显示公告
            cloudAlertBorder.IsVisible = false;
        }
    }
}
