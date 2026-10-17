using System;
using System.Diagnostics;
using System.IO;

namespace MinecraftConnectTool.Services;

internal static class CoreProcessOwnership
{
    internal static bool IsManaged(Process process)
    {
        try
        {
            var path = process.MainModule?.FileName;
            if (string.IsNullOrEmpty(path))
                return false;
            var root = Path.GetFullPath(LocalStorageService.AppRootDirectory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return Path.GetFullPath(path).StartsWith(root,
                OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }
}
//被误杀了真难绷吧