using CommunityToolkit.Mvvm.ComponentModel;

namespace MinecraftConnectTool.ViewModels.Pages;

public partial class HomePageViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _welcomeText = "欢迎来到 Minecraft Connect Tool 首页";
}
