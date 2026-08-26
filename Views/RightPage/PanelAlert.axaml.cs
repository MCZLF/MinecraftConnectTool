using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MinecraftConnectTool.ViewModels.RightPage;

namespace MinecraftConnectTool.Views.RightPage;

public partial class PanelAlert : UserControl
{
    private string _announcementUrl = string.Empty;

    public event EventHandler? CloseRequested;

    public PanelAlert()
    {
        InitializeComponent();
        DataContext = new PanelAlertViewModel();
        
        Loaded += OnLoaded;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        var backButton = this.FindControl<Button>("BackButton");
        if (backButton != null)
        {
            backButton.Click += (s, ev) =>
            {
                CloseRequested?.Invoke(this, EventArgs.Empty);
            };
        }

        var titleArea = this.FindControl<Grid>("TitleArea");
        var tagIdText = this.FindControl<TextBlock>("TagIdText");
        if (titleArea != null && tagIdText != null)
        {
            titleArea.PointerPressed += (s, ev) =>
            {
                if (ev.Pointer.Type == PointerType.Mouse && !ev.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                {
                    return;
                }

                tagIdText.IsVisible = true;
            };
        }

        var urlButton = this.FindControl<Button>("UrlButton");
        if (urlButton != null)
        {
            urlButton.Click += (s, ev) => OpenAnnouncementUrl();
        }

        var refreshButton = this.FindControl<Button>("RefreshButton");
        if (refreshButton != null)
        {
            refreshButton.Click += async (s, ev) =>
            {
                var alertContent = this.FindControl<TextBlock>("AlertContent");
                if (alertContent != null)
                {
                    alertContent.Text = "正在刷新公告...";
                }
                await LoadAnnouncementAsync();
            };
        }

        _ = LoadAnnouncementAsync();
    }

    private async Task LoadAnnouncementAsync()
    {
        var tagIdText = this.FindControl<TextBlock>("TagIdText");
        var alertContent = this.FindControl<TextBlock>("AlertContent");
        var urlButton = this.FindControl<Button>("UrlButton");

        if (tagIdText == null || alertContent == null) return;

        tagIdText.IsVisible = false;

        if (urlButton != null)
        {
            urlButton.IsVisible = false;
        }

        _announcementUrl = string.Empty;

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var json = await client.GetStringAsync(
                "https://api.mct.mczlf.loft.games/PanelAlert"
            );

            var config = JsonNode.Parse(json);
            string tagId = GetStringValue(config, "TagID", "未获取");
            string text = GetStringValue(config, "Text", "暂无公告内容");
            bool showUrlButton = GetBoolValue(config, "ShowUrlButton");
            string url = GetStringValue(config, "Url", string.Empty).Trim();

            text = text.Replace("\\n", Environment.NewLine)
                       .Replace("\n", Environment.NewLine);

            _announcementUrl = NormalizeWebUrl(url);

            if (urlButton != null)
            {
                urlButton.IsVisible = showUrlButton && !string.IsNullOrWhiteSpace(_announcementUrl);
            }

            tagIdText.Text = "TagID:" + tagId;
            alertContent.Text = text;
        }
        catch (Exception ex)
        {
            tagIdText.Text = "TagID:获取失败";
            alertContent.Text = $"公告获取失败\n【{ex.GetType().Name}】\n{ex.Message}";
        }
    }

    private void OpenAnnouncementUrl()
    {
        if (string.IsNullOrWhiteSpace(_announcementUrl)) return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _announcementUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            var alertContent = this.FindControl<TextBlock>("AlertContent");
            if (alertContent != null)
            {
                alertContent.Text += $"\n\n链接打开失败: {ex.Message}";
            }
        }
    }

    private static string GetStringValue(JsonNode? config, string key, string fallback)
    {
        try
        {
            var value = config?[key]?.ToString();
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
        catch
        {
            return fallback;
        }
    }

    private static bool GetBoolValue(JsonNode? config, string key)
    {
        try
        {
            var value = config?[key];
            if (value == null) return false;

            if (bool.TryParse(value.ToString(), out bool result))
            {
                return result;
            }

            if (int.TryParse(value.ToString(), out int number))
            {
                return number != 0;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static string NormalizeWebUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return string.Empty;
        }

        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps
            ? uri.ToString()
            : string.Empty;
    }
}
