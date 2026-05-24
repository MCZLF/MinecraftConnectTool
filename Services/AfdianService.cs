using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MinecraftConnectTool.Models;

namespace MinecraftConnectTool.Services;

public static class AfdianService
{
    private const string QuerySponsorUrl = "https://ifdian.net/api/open/query-sponsor";
    private const string UserId = "d106bdbc158111edbbcc52540025c377";
    private const string Token = "GBmeRgPbDA4YNJC5V8HUkFSthcXxwa39";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static string ComputeSign(string paramsJson, long ts)
    {
        var raw = $"{Token}params{paramsJson}ts{ts}user_id{UserId}";
        var hash = System.Security.Cryptography.MD5.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static async Task<List<AfdianSponsor>> FetchAllSponsorsAsync()
    {
        var allSponsors = new List<AfdianSponsor>();
        var page = 1;

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

            while (true)
            {
                var paramsObj = new Dictionary<string, object>
                {
                    ["page"] = page,
                    ["per_page"] = 100
                };
                var paramsJson = JsonSerializer.Serialize(paramsObj, JsonOptions);
                var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var sign = ComputeSign(paramsJson, ts);

                var body = new
                {
                    user_id = UserId,
                    ts,
                    sign,
                    @params = paramsJson
                };

                var json = JsonSerializer.Serialize(body, JsonOptions);
                System.Diagnostics.Debug.WriteLine($"[AfdianService] 请求第{page}页: {json}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(QuerySponsorUrl, content);
                var responseJson = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[AfdianService] 响应: {responseJson[..Math.Min(responseJson.Length, 500)]}");

                var apiResponse = JsonSerializer.Deserialize<AfdianApiResponse>(responseJson, JsonOptions);

                if (apiResponse?.Data == null || apiResponse.Ec != 200)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[AfdianService] API返回异常: ec={apiResponse?.Ec}, em={apiResponse?.Em}");
                    break;
                }

                allSponsors.AddRange(apiResponse.Data.List);

                if (page >= apiResponse.Data.TotalPage)
                    break;

                page++;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AfdianService] 获取赞助者数据失败: {ex.Message}");
        }

        return allSponsors;
    }
}