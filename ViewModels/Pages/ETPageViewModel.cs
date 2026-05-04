using CommunityToolkit.Mvvm.ComponentModel;

namespace MinecraftConnectTool.ViewModels.Pages;

public partial class ETPageViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _pageText = "这是 ET 模式页面 - 基于EasyTier的联机模式";

    [ObservableProperty]
    private string _statusText = "状态: 正在开发...";
}
