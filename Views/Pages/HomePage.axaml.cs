using System;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MinecraftConnectTool.ViewModels.Pages;

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
