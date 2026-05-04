using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace MinecraftConnectTool.Services;

/// <summary>
/// 管理员权限检测服务
/// </summary>
public static class AdminService
{
    /// <summary>
    /// 管理员状态
    /// 1 = 管理员模式
    /// 2 = 非管理员模式
    /// </summary>
    public static int AdminState { get; private set; } = 0;

    /// <summary>
    /// 是否为管理员模式
    /// </summary>
    public static bool IsAdmin => AdminState == 1;

    /// <summary>
    /// 初始化管理员状态
    /// </summary>
    public static void Initialize()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                if (principal.IsInRole(WindowsBuiltInRole.Administrator))
                {
                    AdminState = 1;
                }
                else
                {
                    AdminState = 2;
                }
            }
            catch
            {
                AdminState = 2;
            }
        }
        else
        {
            // 非Windows平台默认非管理员
            AdminState = 2;
        }
    }

    /// <summary>
    /// 尝试以管理员权限重启应用程序
    /// </summary>
    public static void RestartAsAdmin()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                var exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = exePath,
                        UseShellExecute = true,
                        Verb = "runas"
                    };
                    Process.Start(psi);
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"以管理员权限重启失败: {ex.Message}");
            }
        }
    }
}
