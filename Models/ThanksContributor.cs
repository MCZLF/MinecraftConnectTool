using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MinecraftConnectTool.Models;

public class ThanksContributor
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("github")]
    public string? Github { get; set; }

    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }

    [JsonPropertyName("introduce")]
    public string Introduce { get; set; } = string.Empty;
}

public class ThanksData
{
    [JsonPropertyName("contributors")]
    public List<ThanksContributor> Contributors { get; set; } = new();
}