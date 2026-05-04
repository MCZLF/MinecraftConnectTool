using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MinecraftConnectTool.ViewModels.RightPage;

namespace MinecraftConnectTool.Views.RightPage;

public partial class PanelPlayerManager : UserControl
{
    public event EventHandler? CloseRequested;

    public PanelPlayerManager()
    {
        InitializeComponent();
        DataContext = new PanelPlayerManagerViewModel();
        
        // 绑定ViewModel的关闭请求事件
        if (DataContext is PanelPlayerManagerViewModel vm)
        {
            vm.CloseRequested += (s, e) => CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
