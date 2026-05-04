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
    private static readonly string ConfigFilePath;
    private static readonly object LockObj = new();
    private static JsonObject? _configCache;
    private static readonly JsonSerializerOptions JsonOptions;

    static ConfigService()
    {
        var tempPath = Environment.GetEnvironmentVariable("TEMP") ?? Path.GetTempPath();
        var configDir = Path.Combine(tempPath, "MCZLFAPP", "Temp");
        ConfigFilePath = Path.Combine(configDir, "APPconfig.json");

        // 初始化 JSON 序列化选项
        JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = null
        };

        // 确保目录存在
        if (!Directory.Exists(configDir))
        {
            Directory.CreateDirectory(configDir);
        }

        // 初始化默认配置
        InitializeDefaultConfig();
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
                ["ServerPostEnable"] = true,
                ["nonotifywhenstart"] = false,
                ["EnableVersionCheck"] = true,
                ["EnableATDDark"] = false,
                ["AnimationSpeed"] = "Medium",
                ["CustomAnimationDuration"] = 200,
                ["RenderingMode"] = "SystemDefault"
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
            try
            {
                // 使用 JsonSerializer 序列化整个对象
                var dict = new System.Collections.Generic.Dictionary<string, object?>();
                foreach (var prop in config)
                {
                    dict[prop.Key] = prop.Value?.GetValue<object?>();
                }
                var json = JsonSerializer.Serialize(dict, JsonOptions);
                File.WriteAllText(ConfigFilePath, json);
                _configCache = config;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存配置失败: {ex.Message}");
                throw;
            }
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
}
