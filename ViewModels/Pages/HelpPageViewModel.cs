using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MinecraftConnectTool.ViewModels.Pages;

public partial class HelpPageViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _userName = GetCurrentUserName();

    public HelpPageViewModel()
    {
    }

    /// <summary>
    /// 获取当前系统用户名（跨平台）
    /// </summary>
    private static string GetCurrentUserName()
    {
        try
        {
            // 优先使用 Environment.UserName（适用于 Windows/Linux/macOS）
            var userName = Environment.UserName;

            // 如果 Environment.UserName 为空，尝试其他方式
            if (string.IsNullOrWhiteSpace(userName))
            {
                // 尝试从 USER 环境变量获取（Unix/Linux/macOS 常用）
                userName = Environment.GetEnvironmentVariable("USER");
            }

            if (string.IsNullOrWhiteSpace(userName))
            {
                // 尝试从 USERNAME 环境变量获取（Windows 常用）
                userName = Environment.GetEnvironmentVariable("USERNAME");
            }

            if (string.IsNullOrWhiteSpace(userName))
            {
                // 尝试从 LOGNAME 环境变量获取（某些 Unix 系统）
                userName = Environment.GetEnvironmentVariable("LOGNAME");
            }

            // 如果仍然为空，返回默认值
            return string.IsNullOrWhiteSpace(userName) ? "User" : userName;
        }
        catch
        {
            return "User";
        }
    }
}
