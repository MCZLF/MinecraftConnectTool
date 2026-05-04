using System;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MinecraftConnectTool.Services;

/// <summary>
/// 版本检查服务
/// </summary>
public interface IVersionCheckService
{
    event EventHandler<VersionCheckResult>? VersionChecked;
    Task CheckAsync();
}

public class VersionCheckResult
{
    public bool HasUpdate { get; set; }
    public string? LatestVersion { get; set; }
    public string? DownloadUrl { get; set; }
    public string? ReleaseNotes { get; set; }
}

public partial class VersionCheckService : ObservableObject, IVersionCheckService
{
    private readonly HttpClient _httpClient;
    private readonly string _currentVersion;

    public event EventHandler<VersionCheckResult>? VersionChecked;

    public VersionCheckService(string currentVersion)
    {
        _currentVersion = currentVersion;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    public async Task CheckAsync()
    {
        // 检查用户是否启用了版本检查
        bool enableCheck = ConfigService.Read("EnableVersionCheck", true);
        if (!enableCheck) return;

        try
        {
            // 获取最新版本号
            string versionUrl = $"https://api.mct.mczlf.loft.games/007/{GetPlatformName()}_{GetArchitectureName()}/version";
            var latestVersion = await _httpClient.GetStringAsync(versionUrl);
            latestVersion = latestVersion.Trim();
            
            // 比较版本号
            bool hasUpdate = IsNewerVersion(latestVersion, _currentVersion);
            
            var result = new VersionCheckResult
            {
                HasUpdate = hasUpdate,
                LatestVersion = latestVersion,
                ReleaseNotes = hasUpdate ? $"发现新版本 {latestVersion}，当前版本 {_currentVersion}" : "当前已是最新版本"
            };
            
            VersionChecked?.Invoke(this, result);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"版本检查失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 比较版本号，判断新版本是否比当前版本新
    /// </summary>
    private static bool IsNewerVersion(string newVersion, string currentVersion)
    {
        try
        {
            // 移除版本号前缀"v"或"V"
            newVersion = newVersion.TrimStart('v', 'V');
            currentVersion = currentVersion.TrimStart('v', 'V');
            
            var newParts = newVersion.Split('.');
            var currentParts = currentVersion.Split('.');
            
            int maxLength = Math.Max(newParts.Length, currentParts.Length);
            
            for (int i = 0; i < maxLength; i++)
            {
                int newPart = i < newParts.Length && int.TryParse(newParts[i], out int np) ? np : 0;
                int currentPart = i < currentParts.Length && int.TryParse(currentParts[i], out int cp) ? cp : 0;
                
                if (newPart > currentPart) return true;
                if (newPart < currentPart) return false;
            }
            
            return false; // 版本相同
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取平台名称
    /// </summary>
    private static string GetPlatformName()
    {
        if (OperatingSystem.IsWindows()) return "Win";
        if (OperatingSystem.IsLinux()) return "Linux";
        if (OperatingSystem.IsMacOS()) return "MacOS";
        return "Win";
    }

    /// <summary>
    /// 获取架构名称
    /// </summary>
    private static string GetArchitectureName()
    {
        return RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X86 => "X86",
            Architecture.X64 => "X64",
            Architecture.Arm => "Arm",
            Architecture.Arm64 => "Arm64",
            _ => "X64"
        };
    }
}
