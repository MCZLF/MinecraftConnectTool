using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MinecraftConnectTool.Services;

/// <summary>
/// 配置管理服务 - 全局单例
/// </summary>
public static class ConfigService
{
    private static string ConfigFilePath;
    private static readonly object LockObj = new();
    private static JsonObject? _configCache;
    private static readonly JsonSerializerOptions JsonOptions;

    static ConfigService()
    {
        ConfigFilePath = LocalStorageService.ConfigFilePath;

        // 初始化 JSON 序列化选项
        JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = null
        };
    }

    private static void InitializeDefaultConfig()
    {
        if (!File.Exists(ConfigFilePath))
        {
            var defaultConfig = new JsonObject
            {
                ["goupdatewhenstart"] = false,
                ["Bar"] = 1,
                ["AutoCheckP2PIFOpen"] = true,
                ["AutoDetectConflictProgram"] = true,
                ["ServerPostEnable"] = true,
                ["nonotifywhenstart"] = false,
                ["EnableVersionCheck"] = true,
                ["EnableATDDark"] = false,
                ["AnimationSpeed"] = "Medium",
                ["CustomAnimationDuration"] = 200,
                ["RenderingMode"] = "SystemDefault",
                ["DefaultStartupPage"] = "Home",
                ["SimulateFluentDesign"] = true,
                ["AutoReportCrashLog"] = true,  //错误日志自动上报 默认值True
                ["AllowProbe"] = true //是否允许上报Probe数据 默认值True
            };

            SaveConfig(defaultConfig);
        }
    }

    private static JsonObject LoadConfig()
    {
        if (_configCache != null) return _configCache;

        lock (LockObj)
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var json = File.ReadAllText(ConfigFilePath);
                    _configCache = JsonNode.Parse(json)?.AsObject() ?? [];
                }
                else
                {
                    _configCache = [];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载配置失败: {ex.Message}");
                _configCache = [];
            }
            return _configCache;
        }
    }

    private static void SaveConfig(JsonObject config)
    {
        lock (LockObj)
        {
            var json = SerializeConfig(config);
            if (TrySaveConfigFile(ConfigFilePath, json))
            {
                _configCache = config;
                return;
            }

            if (LocalStorageService.StorageMode != LocalStorageMode.SystemTemp && TrySwitchToFallbackStorage())
            {
                if (TrySaveConfigFile(ConfigFilePath, json))
                {
                    _configCache = config;
                    return;
                }
            }

            Console.WriteLine($"保存配置失败，已保留内存配置: {ConfigFilePath}");
            _configCache = config;
        }
    }

    private static string SerializeConfig(JsonObject config)
    {
        var dict = new System.Collections.Generic.Dictionary<string, object?>();
        foreach (var prop in config)
        {
            dict[prop.Key] = prop.Value?.GetValue<object?>();
        }

        return JsonSerializer.Serialize(dict, JsonOptions);
    }

    private static bool TrySaveConfigFile(string path, string json)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            if (File.Exists(path))
                File.SetAttributes(path, File.GetAttributes(path) & ~FileAttributes.ReadOnly);

            File.WriteAllText(path, json);
            return true;
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            Console.WriteLine($"保存配置失败: {ex.Message}");
            return false;
        }
    }

    private static bool TrySwitchToFallbackStorage()
    {
        try
        {
            Console.WriteLine($"当前配置目录不可写，切换到系统 Temp 存储: {ConfigFilePath}");
            LocalStorageService.Configure(LocalStorageMode.SystemTemp, migrateExistingData: false);
            ConfigFilePath = LocalStorageService.ConfigFilePath;
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"切换系统 Temp 存储失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 读取配置项
    /// </summary>
    public static T Read<T>(string key, T defaultValue = default!)
    {
        var config = LoadConfig();
        
        if (config.TryGetPropertyValue(key, out var value) && value != null)
        {
            try
            {
                return value.GetValue<T>()!;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取配置项 '{key}' 失败: {ex.Message}");
            }
        }
        
        return defaultValue;
    }

    /// <summary>
    /// 写入配置项
    /// </summary>
    public static void Write(string key, object value)
    {
        lock (LockObj)
        {
            var config = LoadConfig();
            // 使用 JsonSerializer 序列化值，然后解析为 JsonNode
            var jsonString = JsonSerializer.Serialize(value, JsonOptions);
            config[key] = JsonNode.Parse(jsonString);
            SaveConfig(config);
            Console.WriteLine($"配置已保存: {key} = {value}");
        }
    }

    /// <summary>
    /// 删除配置项
    /// </summary>
    public static void Delete(string key)
    {
        lock (LockObj)
        {
            var config = LoadConfig();
            config.Remove(key);
            SaveConfig(config);
        }
    }

    /// <summary>
    /// 检查配置项是否存在
    /// </summary>
    public static bool Exists(string key)
    {
        var config = LoadConfig();
        return config.ContainsKey(key);
    }

    /// <summary>
    /// 清除所有配置
    /// </summary>
    public static void Clear()
    {
        lock (LockObj)
        {
            SaveConfig([]);
        }
    }

    /// <summary>
    /// 验证配置文件是否存在并可访问
    /// </summary>
    public static bool ValidateConfigAccess()
    {
        try
        {
            if (!File.Exists(ConfigFilePath))
            {
                Console.WriteLine($"配置文件不存在: {ConfigFilePath}");
                return false;
            }

            var fileInfo = new FileInfo(ConfigFilePath);
            if (fileInfo.IsReadOnly)
            {
                Console.WriteLine($"配置文件只读: {ConfigFilePath}");
                return false;
            }

            var testContent = File.ReadAllText(ConfigFilePath);
            Console.WriteLine($"配置文件可读，大小: {testContent.Length} 字节");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"验证配置访问失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取配置文件路径（用于调试）
    /// </summary>
    public static string GetConfigFilePath() => ConfigFilePath;

    public static void ReloadFromStorage()
    {
        lock (LockObj)
        {
            _configCache = null;
            ConfigFilePath = LocalStorageService.ConfigFilePath;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath)!);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Console.WriteLine($"初始化配置目录失败: {ex.Message}");
                if (LocalStorageService.StorageMode != LocalStorageMode.SystemTemp)
                    TrySwitchToFallbackStorage();
            }

            InitializeDefaultConfig();
        }
    }
}
