using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftConnectTool.Services;

/// <summary>
/// Probe 服务 - 启动时上报版本号等基础信息
/// </summary>
public static class ProbeService
{
    //================ 可改常量 ================
    private const string HOST = "mctservice.mczlf.loft.games";
    private const int PORT = 17600;
    private const int TIMEOUT = 5000;

    //================ 开关 ================
    private static bool _enablePopup = false;   // false=完全静默
    public static bool EnablePopup
    {
        get => _enablePopup;
        set => _enablePopup = value;
    }

    //================ 版本号 ================
    public static string Version { get; set; } = "Unknown";

    //================ Probe 标记文件路径 ================
    private static string ProbeMarkerPath => Path.Combine(Path.GetTempPath(), "MCZLFAPP", "Temp", "Probe");

    /// <summary>
    /// 检查是否需要发送 Probe（通过标记文件）
    /// </summary>
    public static bool ShouldSend()
    {
        return !File.Exists(ProbeMarkerPath);
    }

    /// <summary>
    /// 标记 Probe 已发送（创建标记文件）
    /// </summary>
    public static void MarkAsSent()
    {
        try
        {
            string? directory = Path.GetDirectoryName(ProbeMarkerPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(ProbeMarkerPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        }
        catch { /* 静默处理 */ }
    }

    /// <summary>
    /// 发送 Probe 数据
    /// </summary>
    public static async Task SendAsync()
    {
        // 检查是否允许 Probe
        bool allowProbe = ConfigService.Read<bool>("AllowProbe", true);
        if (!allowProbe) return;

        // 如果已经发送过，则跳过
        if (!ShouldSend()) return;

        await Task.Run(async () =>
        {
            try
            {
                string body =
$@"====ProbeContext====
Version = {Version}
Time = {DateTime.Now:yyyy-MM-dd HH:mm:ss}
";
                byte[] data = Encoding.UTF8.GetBytes(body);
                using (TcpClient client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(HOST, PORT);
                    bool ok = await Task.WhenAny(connectTask, Task.Delay(TIMEOUT)) == connectTask && connectTask.IsCompleted;
                    if (!ok) return;

                    var stream = client.GetStream();
                    stream.WriteTimeout = TIMEOUT;
                    stream.ReadTimeout = TIMEOUT;

                    await stream.WriteAsync(data, 0, data.Length);
                    byte[] buffer = new byte[256];
                    int n = await stream.ReadAsync(buffer, 0, buffer.Length);
                    string response = Encoding.UTF8.GetString(buffer, 0, n).Trim();

                    // 无论返回什么结果，都正常处理
                    _ = response;
                }
            }
            catch (Exception ex)
            {
                // 静默处理，仅输出到控制台
                Console.WriteLine($"[Probe] 发送失败: {ex.Message}");
            }
            finally
            {
                // 无论成功与否，都标记为已发送（避免重复尝试）
                MarkAsSent();
            }
        });
    }
}
