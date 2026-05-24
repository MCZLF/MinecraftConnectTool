using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using MinecraftConnectTool.Models;

namespace MinecraftConnectTool.Services;

public static class ThanksService
{
    private const string ThanksJsonUrl = "https://api.mct.mczlf.loft.games/007/thanks.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<ThanksData?> FetchThanksDataAsync()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var json = await client.GetStringAsync(ThanksJsonUrl);
            var data = JsonSerializer.Deserialize<ThanksData>(json, JsonOptions);
            return data;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ThanksService] 获取鸣谢数据失败: {ex.Message}");
            return null;
        }
    }
}