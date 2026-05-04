using System;
using System.Runtime.InteropServices;

namespace MinecraftConnectTool.Services;

public interface IPlatformService
{
    bool IsWindows { get; }
    bool IsLinux { get; }
    bool IsMacOS { get; }
    bool SupportsMica { get; }
    bool SupportsCustomTitleBar { get; }
}

public class PlatformService : IPlatformService
{
    public bool IsWindows => OperatingSystem.IsWindows();
    public bool IsLinux => OperatingSystem.IsLinux();
    public bool IsMacOS => OperatingSystem.IsMacOS();
    
    // Mica效果只在Windows 11上支持
    public bool SupportsMica => IsWindows && IsWindows11();
    
    // 自定义标题栏在Windows和Linux上支持较好，macOS有特殊处理
    public bool SupportsCustomTitleBar => IsWindows || IsLinux;
    
    private static bool IsWindows11()
    {
        if (!OperatingSystem.IsWindows())
            return false;
        
        // Windows 11的版本号是10.0.22000或更高
        var version = Environment.OSVersion.Version;
        return version.Major >= 10 && version.Build >= 22000;
    }
}
