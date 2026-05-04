using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MinecraftConnectTool.ViewModels.RightPage;

namespace MinecraftConnectTool.Views.RightPage;

public partial class PanelAlert : UserControl
{
    // 关闭事件
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
        // 绑定返回按钮事件
        var backButton = this.FindControl<Button>("BackButton");
        if (backButton != null)
        {
            backButton.Click += (s, ev) =>
            {
                // 触发关闭事件
                CloseRequested?.Invoke(this, EventArgs.Empty);
            };
        }

        // 绑定刷新按钮事件
        var refreshButton = this.FindControl<Button>("RefreshButton");
        if (refreshButton != null)
        {
            refreshButton.Click += async (s, ev) =>
            {
                // 显示加载中
                var alertContent = this.FindControl<TextBlock>("AlertContent");
                if (alertContent != null)
                {
                    alertContent.Text = "正在刷新公告...";
                }
                // 重新加载公告
                await LoadAnnouncementAsync();
            };
        }

        // 加载公告
        _ = LoadAnnouncementAsync();
    }

    private async Task LoadAnnouncementAsync()
    {
        var titleText = this.FindControl<TextBlock>("TitleText");
        var alertContent = this.FindControl<TextBlock>("AlertContent");

        if (titleText == null || alertContent == null) return;

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var json = await client.GetStringAsync(
                "https://api.mct.mczlf.loft.games/PanelAlert"
            );

            var config = JsonNode.Parse(json);
            string tagId = config?["TagID"]?.ToString() ?? "未获取";
            string text = config?["Text"]?.ToString() ?? "暂无公告内容";

            // 处理换行符
            text = text.Replace("\\n", Environment.NewLine)
                       .Replace("\n", Environment.NewLine);

            titleText.Text = "重要云公告|TagID:" + tagId;
            alertContent.Text = text;
        }
        catch (Exception ex)
        {
            titleText.Text = "重要云公告|TagID:获取失败";
            alertContent.Text = $"公告获取失败\n【{ex.GetType().Name}】\n{ex.Message}";
        }
    }
}
