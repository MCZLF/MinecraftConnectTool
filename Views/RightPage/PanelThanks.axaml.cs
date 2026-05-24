using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Material.Icons.Avalonia;
using MinecraftConnectTool.Models;
using MinecraftConnectTool.Services;
using MinecraftConnectTool.ViewModels.RightPage;

namespace MinecraftConnectTool.Views.RightPage;

public partial class PanelThanks : UserControl
{
    public event EventHandler? CloseRequested;

    public PanelThanks()
    {
        InitializeComponent();
        DataContext = new PanelThanksViewModel();

        Loaded += OnLoaded;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private IBrush? GetBrush(string key) => this.FindResource(key) as IBrush;

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

        var refreshButton = this.FindControl<Button>("RefreshButton");
        if (refreshButton != null)
        {
            refreshButton.Click += async (s, ev) =>
            {
                refreshButton.IsEnabled = false;
                var loadingText = this.FindControl<TextBlock>("LoadingText");
                if (loadingText != null)
                {
                    loadingText.Text = "正在刷新...";
                    loadingText.IsVisible = true;
                }
                var panel = this.FindControl<StackPanel>("ContributorsPanel");
                if (panel != null)
                {
                    panel.Children.Clear();
                    panel.Children.Add(loadingText!);
                }
                await LoadAllDataAsync();
                refreshButton.IsEnabled = true;
            };
        }

        _ = LoadAllDataAsync();
    }

    private async Task LoadThanksDataAsync()
    {
        var loadingText = this.FindControl<TextBlock>("LoadingText");
        var panel = this.FindControl<StackPanel>("ContributorsPanel");

        if (loadingText == null || panel == null) return;

        try
        {
            var data = await ThanksService.FetchThanksDataAsync();

            if (loadingText.Parent == panel)
            {
                panel.Children.Remove(loadingText);
            }

            if (data == null || data.Contributors.Count == 0)
            {
                var emptyText = new TextBlock
                {
                    Text = "暂无贡献者数据",
                    FontSize = 13,
                    Foreground = GetBrush("MaterialOnSurfaceBrush"),
                    Opacity = 0.6,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Margin = new Avalonia.Thickness(0, 20, 0, 0)
                };
                panel.Children.Add(emptyText);
            }
            else
            {
                foreach (var contributor in data.Contributors)
                {
                    var card = CreateContributorCard(contributor);
                    panel.Children.Add(card);
                }
            }
        }
        catch (Exception ex)
        {
            if (loadingText.Parent == panel)
            {
                panel.Children.Remove(loadingText);
            }
            var errorText = new TextBlock
            {
                Text = $"加载失败: {ex.Message}",
                FontSize = 13,
                Foreground = GetBrush("MaterialErrorBrush"),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 20, 0, 0),
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            };
            panel.Children.Add(errorText);
        }
    }

    private async Task LoadAllDataAsync()
    {
        await LoadThanksDataAsync();
        await LoadSponsorDataAsync();
    }

    private async Task LoadSponsorDataAsync()
    {
        var panel = this.FindControl<StackPanel>("ContributorsPanel");
        if (panel == null) return;

        try
        {
            var sponsors = await AfdianService.FetchAllSponsorsAsync();

            panel.Children.Add(CreateSectionHeader());

            if (sponsors.Count == 0)
            {
                var emptyText = new TextBlock
                {
                    Text = "暂无赞助者数据",
                    FontSize = 13,
                    Foreground = GetBrush("MaterialOnSurfaceBrush"),
                    Opacity = 0.6,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Margin = new Avalonia.Thickness(0, 8, 0, 0)
                };
                panel.Children.Add(emptyText);
                return;
            }

            foreach (var sponsor in sponsors)
            {
                var card = CreateSponsorCard(sponsor);
                panel.Children.Add(card);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PanelThanks] 加载赞助者失败: {ex.Message}");
            var errorText = new TextBlock
            {
                Text = $"赞助者加载失败: {ex.Message}",
                FontSize = 12,
                Foreground = GetBrush("MaterialErrorBrush"),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 8, 0, 0),
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            };
            panel.Children.Add(errorText);
        }
    }

    private Border CreateSectionHeader()
    {
        return new Border
        {
            Margin = new Avalonia.Thickness(0, 16, 0, 4),
            Padding = new Avalonia.Thickness(0, 4, 0, 4),
            Child = new StackPanel
            {
                Spacing = 2,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Children =
                {
                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        Spacing = 8,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Children =
                        {
                            new MaterialIcon
                            {
                                Kind = Material.Icons.MaterialIconKind.HeartOutline,
                                Width = 14,
                                Height = 14,
                                Foreground = GetBrush("MaterialPrimaryBrush")
                            },
                            new TextBlock
                            {
                                Text = "赞助者 · 爱发电",
                                FontSize = 13,
                                FontWeight = Avalonia.Media.FontWeight.Medium,
                                Foreground = GetBrush("MaterialOnSurfaceBrush"),
                                Opacity = 0.6
                            },
                            new MaterialIcon
                            {
                                Kind = Material.Icons.MaterialIconKind.HeartOutline,
                                Width = 14,
                                Height = 14,
                                Foreground = GetBrush("MaterialPrimaryBrush")
                            }
                        }
                    },
                    new TextBlock
                    {
                        Text = "该列表包含 MCT联机工具箱与MCZLF服务器的所有数据",
                        FontSize = 10,
                        Foreground = GetBrush("MaterialOnSurfaceBrush"),
                        Opacity = 0.35,
                        TextAlignment = Avalonia.Media.TextAlignment.Center
                    }
                }
            }
        };
    }

    private Border CreateSponsorCard(AfdianSponsor sponsor)
    {
        var card = new Border
        {
            Background = GetBrush("MaterialSurfaceVariantBrush"),
            CornerRadius = new Avalonia.CornerRadius(12),
            Padding = new Avalonia.Thickness(16),
            Margin = new Avalonia.Thickness(0, 4, 0, 0)
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*")
        };

        var avatarBorder = new Border
        {
            Width = 48,
            Height = 48,
            CornerRadius = new Avalonia.CornerRadius(24),
            Background = GetBrush("MaterialPrimaryContainerBrush"),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 0, 12, 0)
        };

        if (!string.IsNullOrEmpty(sponsor.User.Avatar))
        {
            try
            {
                var image = new Image
                {
                    Width = 48,
                    Height = 48,
                    Stretch = Stretch.UniformToFill
                };
                RenderOptions.SetBitmapInterpolationMode(image, BitmapInterpolationMode.LowQuality);
                _ = LoadAvatarAsync(image, sponsor.User.Avatar);
                avatarBorder.Child = image;
            }
            catch
            {
                avatarBorder.Child = CreateAvatarFallback(sponsor.User.Name);
            }
        }
        else
        {
            avatarBorder.Child = CreateAvatarFallback(sponsor.User.Name);
        }

        avatarBorder.ClipToBounds = true;
        avatarBorder.CornerRadius = new Avalonia.CornerRadius(24);

        Grid.SetColumn(avatarBorder, 0);

        var infoPanel = new StackPanel
        {
            Spacing = 4,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        var nameText = new TextBlock
        {
            Text = sponsor.User.Name,
            FontSize = 15,
            FontWeight = Avalonia.Media.FontWeight.Medium,
            Foreground = GetBrush("MaterialOnSurfaceBrush")
        };

        var planName = !string.IsNullOrEmpty(sponsor.CurrentPlan.Name)
            ? sponsor.CurrentPlan.Name
            : "未指定方案";

        var detailText = new TextBlock
        {
            Text = $"累计: ¥{sponsor.AllSumAmount}  ·  {planName}",
            FontSize = 12,
            Foreground = GetBrush("MaterialOnSurfaceBrush"),
            Opacity = 0.7
        };

        infoPanel.Children.Add(nameText);
        infoPanel.Children.Add(detailText);

        Grid.SetColumn(infoPanel, 1);

        grid.Children.Add(avatarBorder);
        grid.Children.Add(infoPanel);

        card.Child = grid;
        return card;
    }

    private Border CreateContributorCard(ThanksContributor contributor)
    {
        var card = new Border
        {
            Background = GetBrush("MaterialSurfaceVariantBrush"),
            CornerRadius = new Avalonia.CornerRadius(12),
            Padding = new Avalonia.Thickness(16),
            Margin = new Avalonia.Thickness(0, 0, 0, 0)
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto")
        };

        var avatarBorder = new Border
        {
            Width = 48,
            Height = 48,
            CornerRadius = new Avalonia.CornerRadius(24),
            Background = GetBrush("MaterialPrimaryContainerBrush"),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 0, 12, 0)
        };

        if (!string.IsNullOrEmpty(contributor.Avatar))
        {
            try
            {
                var image = new Image
                {
                    Width = 48,
                    Height = 48,
                    Stretch = Stretch.UniformToFill
                };
                RenderOptions.SetBitmapInterpolationMode(image, BitmapInterpolationMode.LowQuality);
                _ = LoadAvatarAsync(image, contributor.Avatar);
                avatarBorder.Child = image;
            }
            catch
            {
                avatarBorder.Child = CreateAvatarFallback(contributor.Id);
            }
        }
        else
        {
            avatarBorder.Child = CreateAvatarFallback(contributor.Id);
        }

        avatarBorder.ClipToBounds = true;
        avatarBorder.CornerRadius = new Avalonia.CornerRadius(24);

        Grid.SetColumn(avatarBorder, 0);

        var infoPanel = new StackPanel
        {
            Spacing = 4,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        var idText = new TextBlock
        {
            Text = contributor.Id,
            FontSize = 15,
            FontWeight = Avalonia.Media.FontWeight.Medium,
            Foreground = GetBrush("MaterialOnSurfaceBrush")
        };

        var introduceText = new TextBlock
        {
            Text = contributor.Introduce,
            FontSize = 12,
            Foreground = GetBrush("MaterialOnSurfaceBrush"),
            Opacity = 0.7,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        };

        infoPanel.Children.Add(idText);
        infoPanel.Children.Add(introduceText);

        Grid.SetColumn(infoPanel, 1);

        grid.Children.Add(avatarBorder);
        grid.Children.Add(infoPanel);

        if (!string.IsNullOrEmpty(contributor.Github))
        {
            var githubButton = new Button
            {
                Classes = { "github-button" },
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Margin = new Avalonia.Thickness(8, 0, 0, 0)
            };

            var githubIcon = new MaterialIcon
            {
                Kind = Material.Icons.MaterialIconKind.Github,
                Width = 20,
                Height = 20,
                Foreground = GetBrush("MaterialOnSurfaceBrush")
            };

            githubButton.Content = githubIcon;

            var githubUrl = contributor.Github;
            githubButton.Click += (s, ev) =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = githubUrl,
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                }
                catch
                {
                }
            };

            Grid.SetColumn(githubButton, 2);
            grid.Children.Add(githubButton);
        }

        card.Child = grid;
        return card;
    }

    private static TextBlock CreateAvatarFallback(string id)
    {
        var firstChar = string.IsNullOrEmpty(id) ? "?" : id[..1].ToUpper();
        return new TextBlock
        {
            Text = firstChar,
            FontSize = 20,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            Foreground = Brushes.White,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
    }

    private async Task LoadAvatarAsync(Image image, string url)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var bytes = await client.GetByteArrayAsync(url);
            using var stream = new System.IO.MemoryStream(bytes);
            image.Source = new Bitmap(stream);
        }
        catch
        {
            image.Source = null;
        }
    }
}