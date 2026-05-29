using System.Text.Json.Serialization;

namespace RendaTop.App.Models;

public sealed record WalletsOverviewDto
{
    [JsonPropertyName("items")]
    public List<WalletDto>? Items { get; init; }

    [JsonPropertyName("active_wallet_id")]
    public Guid ActiveWalletId { get; init; }

    [JsonPropertyName("count")]
    public int Count { get; init; }

    [JsonPropertyName("limit")]
    public int? Limit { get; init; }

    [JsonPropertyName("can_create")]
    public bool CanCreate { get; init; }

    [JsonPropertyName("restriction_message")]
    public string? RestrictionMessage { get; init; }
}

public sealed record WalletDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; }
}

public sealed record WalletRequestDto(
    [property: JsonPropertyName("name")] string Name);
