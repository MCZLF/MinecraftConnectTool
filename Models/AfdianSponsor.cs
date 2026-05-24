using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MinecraftConnectTool.Models;

public class AfdianApiResponse
{
    [JsonPropertyName("ec")]
    public int Ec { get; set; }

    [JsonPropertyName("em")]
    public string Em { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public AfdianSponsorData? Data { get; set; }
}

public class AfdianSponsorData
{
    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }

    [JsonPropertyName("total_page")]
    public int TotalPage { get; set; }

    [JsonPropertyName("list")]
    public List<AfdianSponsor> List { get; set; } = new();
}

public class AfdianSponsor
{
    [JsonPropertyName("sponsor_plans")]
    public List<SponsorPlan> SponsorPlans { get; set; } = new();

    [JsonPropertyName("current_plan")]
    public CurrentPlan CurrentPlan { get; set; } = new();

    [JsonPropertyName("all_sum_amount")]
    public string AllSumAmount { get; set; } = "0.00";

    [JsonPropertyName("create_time")]
    public long CreateTime { get; set; }

    [JsonPropertyName("last_pay_time")]
    public long LastPayTime { get; set; }

    [JsonPropertyName("user")]
    public AfdianUser User { get; set; } = new();
}

public class SponsorPlan
{
    [JsonPropertyName("plan_id")]
    public string PlanId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public string Price { get; set; } = "0.00";
}

public class CurrentPlan
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class AfdianUser
{
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("avatar")]
    public string Avatar { get; set; } = string.Empty;
}