using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MinecraftConnectTool.Services;

public enum LocalStorageMode
{
    AppDirectory,
    SystemTemp,
    Custom
}

public sealed record LocalStorageOption(LocalStorageMode Mode, string DisplayName, string Description);

public static class LocalStorageService
{
    private const string AppDataFolderName = "MCTConfig";
    private const string LegacyAppFolderName = "MCZLFAPP";
    private const string BootstrapFileName = "MCTStorage.json";

    private static readonly object SyncRoot = new();
    private static LocalStorageSettings? _settings;

    public static string BootstrapDirectory => Path.Combine(GetUserHomeDirectory(), "MCTStorageConfig");

    public static string BootstrapFilePath => Path.Combine(BootstrapDirectory, BootstrapFileName);

    public static string AppRootDirectory => GetRootDirectory();

    public static string TempDirectory => Path.Combine(AppRootDirectory, "Temp");

    public static string ETBaseDirectory => Path.Combine(AppRootDirectory, "ET");

    public static string ConfigFilePath => Path.Combine(TempDirectory, "APPconfig.json");

    public static string AppLogPath => Path.Combine(TempDirectory, "APPLog.ini");

    public static string LinkCoreDirectory => TempDirectory;

    public static string P2PCoreDirectory => TempDirectory;

    public static string ThemeFilePath => Path.Combine(AppRootDirectory, "theme.json");

    public static string LegacyRootDirectory => Path.Combine(GetSystemTempDirectory(), LegacyAppFolderName);

    public static string LegacyTempDirectory => Path.Combine(LegacyRootDirectory, "Temp");

    public static string LegacyConfigFilePath => Path.Combine(LegacyTempDirectory, "APPconfig.json");

    public static LocalStorageMode StorageMode => LoadSettings().Mode;

    public static string CustomDirectory => LoadSettings().CustomDirectory ?? string.Empty;

    public static IReadOnlyList<LocalStorageOption> PresetOptions { get; } = new[]
    {
        new LocalStorageOption(LocalStorageMode.AppDirectory, "当前目录", "主程序目录下的 MCTConfig 文件夹"),
        new LocalStorageOption(LocalStorageMode.SystemTemp, "系统 Temp", "兼容旧版的系统临时目录"),
        new LocalStorageOption(LocalStorageMode.Custom, "自定义地址", "由用户手动选择存放目录")
    };

    public static string GetRootDirectory()
    {
        var settings = LoadSettings();
        var directory = settings.Mode switch
        {
            LocalStorageMode.SystemTemp => LegacyRootDirectory,
            LocalStorageMode.Custom when !string.IsNullOrWhiteSpace(settings.CustomDirectory) => settings.CustomDirectory!,
            _ => Path.Combine(GetExecutableDirectory(), AppDataFolderName)
        };

        return directory;
    }

    public static string GetETDirectory(string version)
    {
        var directory = Path.Combine(ETBaseDirectory, version);
        Directory.CreateDirectory(directory);
        return directory;
    }

    public static string GetTempFilePath(string fileName) => Path.Combine(TempDirectory, fileName);

    public static string GetAppFilePath(string fileName) => Path.Combine(AppRootDirectory, fileName);

    public static string GetPresetImagePath(string fileName) => Path.Combine(TempDirectory, fileName);

    public static void Configure(LocalStorageMode mode, string? customDirectory = null, bool migrateExistingData = true)
    {
        if (mode == LocalStorageMode.Custom && string.IsNullOrWhiteSpace(customDirectory))
            mode = LocalStorageMode.AppDirectory;

        var previousRoot = AppRootDirectory;
        var nextSettings = new LocalStorageSettings
        {
            Mode = mode,
            CustomDirectory = string.IsNullOrWhiteSpace(customDirectory) ? null : customDirectory.Trim()
        };

        lock (SyncRoot)
        {
            SaveSettings(nextSettings);
            _settings = nextSettings;
        }

        EnsureDirectory(AppRootDirectory);
        EnsureDirectory(TempDirectory);

        if (migrateExistingData)
            MigrateData(previousRoot, AppRootDirectory);
    }

    public static void Reload()
    {
        lock (SyncRoot)
        {
            _settings = null;
        }
    }

    public static void EnsureInitialized()
    {
        EnsureDirectory(AppRootDirectory);
        EnsureDirectory(TempDirectory);
        TryMigrateLegacyConfigIfNeeded();
    }

    public static bool TryAutoConfigureLegacySystemTempStorageIfNeeded()
    {
        try
        {
            if (Directory.Exists(BootstrapDirectory))
                return false;

            if (!HasLegacySystemTempStorage())
                return false;

            var settings = new LocalStorageSettings
            {
                Mode = LocalStorageMode.SystemTemp,
                CustomDirectory = null
            };

            lock (SyncRoot)
            {
                SaveSettings(settings);
                _settings = settings;
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"自动迁移旧版存储配置失败: {ex.Message}");
            return false;
        }
    }

    public static bool TryReadBootstrapConfigValue<T>(string key, T defaultValue, out T value)
    {
        value = defaultValue;
        try
        {
            var configPath = ConfigFilePath;
            if (!File.Exists(configPath) && File.Exists(LegacyConfigFilePath))
                configPath = LegacyConfigFilePath;

            if (!File.Exists(configPath))
                return false;

            using var document = JsonDocument.Parse(File.ReadAllText(configPath));
            if (!document.RootElement.TryGetProperty(key, out var element))
                return false;

            value = element.Deserialize<T>() ?? defaultValue;
            return true;
        }
        catch
        {
            value = defaultValue;
            return false;
        }
    }

    public static void ClearAppRoot()
    {
        var root = AppRootDirectory;
        if (!Directory.Exists(root))
            return;

        try
        {
            foreach (var file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            {
                try
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"删除文件失败 {file}: {ex.Message}");
                }
            }

            foreach (var folder in Directory.GetDirectories(root, "*", SearchOption.AllDirectories).OrderByDescending(d => d.Length))
            {
                try
                {
                    if (Directory.Exists(folder) && !Directory.EnumerateFileSystemEntries(folder).Any())
                        Directory.Delete(folder);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"删除目录失败 {folder}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"清空存储目录失败 {root}: {ex.Message}");
            throw;
        }
    }

    public static void DeleteConfigFiles()
    {
        DeleteFileIfExists(ConfigFilePath);
        DeleteFileIfExists(ThemeFilePath);
    }

    public static void DeleteBootstrapDirectory()
    {
        try
        {
            var directory = BootstrapDirectory;
            if (!Directory.Exists(directory))
                return;

            foreach (var file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
            {
                try
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }
                catch { }
            }

            Directory.Delete(directory, true);
            Reload();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"删除存储配置目录失败: {ex.Message}");
        }
    }

    public static void AppendAppLog(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(AppLogPath)!);
            File.AppendAllText(AppLogPath, message);
        }
        catch { }
    }

    private static LocalStorageSettings LoadSettings()
    {
        if (_settings != null)
            return _settings;

        lock (SyncRoot)
        {
            if (_settings != null)
                return _settings;

            try
            {
                if (File.Exists(BootstrapFilePath))
                {
                    var settings = JsonSerializer.Deserialize<LocalStorageSettings>(File.ReadAllText(BootstrapFilePath));
                    if (settings != null)
                    {
                        _settings = settings;
                        return _settings;
                    }
                }
            }
            catch { }

            _settings = new LocalStorageSettings { Mode = LocalStorageMode.AppDirectory };
            return _settings;
        }
    }

    private static void SaveSettings(LocalStorageSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(BootstrapFilePath)!);
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(BootstrapFilePath, json);
    }

    private static string EnsureDirectory(string directory)
    {
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string GetSystemTempDirectory() => Path.GetTempPath();

    private static string GetExecutableDirectory()
    {
        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(processPath))
        {
            var directory = Path.GetDirectoryName(processPath);
            if (!string.IsNullOrWhiteSpace(directory))
                return directory;
        }

        return Directory.GetCurrentDirectory();
    }

    private static string GetUserHomeDirectory()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(home))
            return home;

        return AppContext.BaseDirectory;
    }

    private static bool HasLegacySystemTempStorage()
    {
        return File.Exists(Path.Combine(LegacyTempDirectory, "link.exe")) ||
               File.Exists(Path.Combine(LegacyTempDirectory, "main.exe")) ||
               Directory.Exists(Path.Combine(LegacyRootDirectory, "ET")) &&
               Directory.GetFiles(Path.Combine(LegacyRootDirectory, "ET"), "easytier-core.exe", SearchOption.AllDirectories).Any();
    }

    private static void TryMigrateLegacyConfigIfNeeded()
    {
        try
        {
            if (StorageMode == LocalStorageMode.SystemTemp)
                return;

            if (!File.Exists(ConfigFilePath) && File.Exists(LegacyConfigFilePath))
                CopyDirectory(LegacyRootDirectory, AppRootDirectory, false);
        }
        catch { }
    }

    private static void MigrateData(string sourceRoot, string destinationRoot)
    {
        try
        {
            if (string.Equals(sourceRoot, destinationRoot, StringComparison.OrdinalIgnoreCase))
                return;

            if (Directory.Exists(sourceRoot))
                MoveDirectoryContents(sourceRoot, destinationRoot, true);

            if (StorageMode != LocalStorageMode.SystemTemp && Directory.Exists(LegacyRootDirectory))
                MoveDirectoryContents(LegacyRootDirectory, destinationRoot, false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"迁移本地数据失败: {ex.Message}");
        }
    }

    private static void MoveDirectoryContents(string sourceDirectory, string destinationDirectory, bool deleteSourceDirectory)
    {
        if (!Directory.Exists(sourceDirectory))
            return;

        Directory.CreateDirectory(destinationDirectory);

        foreach (var file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, file);
            var destinationPath = Path.Combine(destinationDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

            try
            {
                if (File.Exists(destinationPath))
                {
                    File.SetAttributes(destinationPath, FileAttributes.Normal);
                    File.Delete(destinationPath);
                }

                File.SetAttributes(file, FileAttributes.Normal);
                File.Move(file, destinationPath);
            }
            catch
            {
                File.Copy(file, destinationPath, true);
                DeleteFileIfExists(file);
            }
        }

        foreach (var folder in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories).OrderByDescending(d => d.Length))
        {
            try
            {
                if (Directory.Exists(folder) && !Directory.EnumerateFileSystemEntries(folder).Any())
                    Directory.Delete(folder);
            }
            catch { }
        }

        if (deleteSourceDirectory)
        {
            try
            {
                if (Directory.Exists(sourceDirectory) && !Directory.EnumerateFileSystemEntries(sourceDirectory).Any())
                    Directory.Delete(sourceDirectory);
            }
            catch { }
        }
    }

    private static void CopyDirectory(string sourceDirectory, string destinationDirectory, bool overwrite)
    {
        Directory.CreateDirectory(destinationDirectory);

        foreach (var file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, file);
            var destinationPath = Path.Combine(destinationDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

            if (!overwrite && File.Exists(destinationPath))
                continue;

            File.Copy(file, destinationPath, overwrite);
        }
    }

    private static bool IsSameOrChildPath(string path, string possibleParentPath)
    {
        var fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var fullParentPath = Path.GetFullPath(possibleParentPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return string.Equals(fullPath, fullParentPath, StringComparison.OrdinalIgnoreCase) ||
               fullPath.StartsWith(fullParentPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
               fullPath.StartsWith(fullParentPath + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static void DeleteFileIfExists(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return;

        try
        {
            File.SetAttributes(path, FileAttributes.Normal);
            File.Delete(path);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"删除文件失败 {path}: {ex.Message}");
        }
    }

    private sealed class LocalStorageSettings
    {
        public LocalStorageMode Mode { get; set; } = LocalStorageMode.AppDirectory;
        public string? CustomDirectory { get; set; }
    }
}
