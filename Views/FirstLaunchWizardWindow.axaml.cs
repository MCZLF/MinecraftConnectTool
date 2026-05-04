using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using MinecraftConnectTool.ViewModels;

namespace MinecraftConnectTool.Views;

public partial class FirstLaunchWizardWindow : Window
{
    public FirstLaunchWizardWindow()
    {
        InitializeComponent();
        var viewModel = new FirstLaunchWizardViewModel();
        DataContext = viewModel;
        
        // 初始化性能模式检测
        viewModel.InitializePerformanceMode();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// 标题栏鼠标按下时开始拖拽窗口
    /// </summary>
    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Pointer.Type == PointerType.Mouse)
        {
            var properties = e.GetCurrentPoint(this).Properties;
            if (properties.IsLeftButtonPressed)
            {
                BeginMoveDrag(e);
            }
        }
    }

    /// <summary>
    /// 点击跳过主题设置文字
    /// </summary>
    private void OnSkipThemeTextPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Pointer.Type == PointerType.Mouse)
        {
            var properties = e.GetCurrentPoint(this).Properties;
            if (properties.IsLeftButtonPressed)
            {
                if (DataContext is FirstLaunchWizardViewModel viewModel)
                {
                    viewModel.SkipThemeSetupCommand.Execute(null);
                }
            }
        }
    }
}
