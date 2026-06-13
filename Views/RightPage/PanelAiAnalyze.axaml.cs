using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MinecraftConnectTool.ViewModels.RightPage;

namespace MinecraftConnectTool.Views.RightPage;

public partial class PanelAiAnalyze : UserControl
{
    public PanelAiAnalyze()
    {
        InitializeComponent();
    }

    public PanelAiAnalyze(string logContent, string serverUrl, string pageName, int timeoutSeconds = 120) : this()
    {
        DataContext = new PanelAiAnalyzeViewModel(logContent, serverUrl, pageName, timeoutSeconds);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
