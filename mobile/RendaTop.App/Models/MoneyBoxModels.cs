using System.Text.Json.Serialization;

namespace RendaTop.App.Models;

public sealed record MoneyBoxesOverviewDto
{
    [JsonPropertyName("items")]
    public List<MoneyBoxDto>? Items { get; init; }

    [JsonPropertyName("count")]
    public int Count { get; init; }

    [JsonPropertyName("limit")]
    public int? Limit { get; init; }

    [JsonPropertyName("can_create")]
    public bool CanCreate { get; init; }

    [JsonPropertyName("selection_enabled")]
    public bool SelectionEnabled { get; init; }

    [JsonPropertyName("active_plan_id")]
    public string ActivePlanId { get; init; } = string.Empty;

    [JsonPropertyName("restriction_message")]
    public string? RestrictionMessage { get; init; }
}

public sealed record MoneyBoxDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("total_liquid_value")]
    public decimal TotalLiquidValue { get; init; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; init; }
}

public sealed record MoneyBoxRequestDto(
    [property: JsonPropertyName("name")] string Name);
