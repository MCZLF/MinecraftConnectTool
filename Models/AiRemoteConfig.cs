namespace MinecraftConnectTool.Models;

public class AiRemoteConfig
{
    public bool Enable { get; set; }
    public string ServerIP { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 120;
}
