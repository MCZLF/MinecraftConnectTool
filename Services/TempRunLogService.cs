using System;
using System.IO;
using System.Text;

namespace MinecraftConnectTool.Services;

public static class TempRunLogService
{
    private static readonly object SyncRoot = new();
    private static string BaseDirectory => Path.Combine(LocalStorageService.TempDirectory, "TempRunLog");

    public static void Initialize()
    {
        try
        {
            if (Directory.Exists(BaseDirectory))
                Directory.Delete(BaseDirectory, true);
            Directory.CreateDirectory(BaseDirectory);
        }
        catch
        {
            try { Directory.CreateDirectory(BaseDirectory); } catch { }
        }
    }

    public static void Append(string pageName, string message)
    {
        if (string.IsNullOrWhiteSpace(pageName) || string.IsNullOrEmpty(message))
            return;

        try
        {
            lock (SyncRoot)
            {
                Directory.CreateDirectory(BaseDirectory);
                File.AppendAllText(GetPageLogPath(pageName), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}", Encoding.UTF8);
            }
        }
        catch { }
    }

    public static void AppendAppLog(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        try
        {
            var logPath = LocalStorageService.AppLogPath;
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}", Encoding.UTF8);
        }
        catch { }
    }

    public static void AppendPageAndApp(string pageName, string message)
    {
        Append(pageName, message);
        AppendAppLog(message);
    }

    public static string Read(string pageName)
    {
        try
        {
            var path = GetPageLogPath(pageName);
            if (!File.Exists(path))
                return string.Empty;

            lock (SyncRoot)
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                return reader.ReadToEnd();
            }
        }
        catch
        {
            return string.Empty;
        }
    }

    public static void Cleanup()
    {
        try
        {
            if (Directory.Exists(BaseDirectory))
                Directory.Delete(BaseDirectory, true);
        }
        catch { }
    }

    private static string GetPageLogPath(string pageName)
    {
        var safeName = pageName.Replace("模式", "", StringComparison.OrdinalIgnoreCase).Trim();
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
            safeName = safeName.Replace(invalidChar, '_');

        if (string.IsNullOrWhiteSpace(safeName))
            safeName = "Global";

        return Path.Combine(BaseDirectory, $"{safeName}.log");
    }
}
