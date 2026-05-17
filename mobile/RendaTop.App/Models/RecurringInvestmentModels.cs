using System.Text.Json.Serialization;

namespace RendaTop.App.Models;

public sealed record RecurringInvestmentsOverviewDto
{
    [JsonPropertyName("recurring_investments_enabled")]
    public bool RecurringInvestmentsEnabled { get; init; }

    [JsonPropertyName("items")]
    public List<RecurringInvestmentDto>? Items { get; init; }
}

public sealed record RecurringInvestmentDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("investment_type")]
    public string? InvestmentType { get; init; }

    [JsonPropertyName("bank_code")]
    public int BankCode { get; init; }

    [JsonPropertyName("bank_name")]
    public string BankName { get; init; } = string.Empty;

    [JsonPropertyName("value")]
    public decimal Value { get; init; }

    [JsonPropertyName("index")]
    public string Index { get; init; } = "CDI";

    [JsonPropertyName("index_percent")]
    public decimal IndexPercent { get; init; }

    [JsonPropertyName("index_value")]
    public decimal IndexValue { get; init; }

    [JsonPropertyName("taxes")]
    public bool Taxes { get; init; }

    [JsonPropertyName("liquidity_daily")]
    public bool LiquidityDaily { get; init; }

    [JsonPropertyName("duration_days")]
    public int? DurationDays { get; init; }

    [JsonPropertyName("frequency")]
    public string Frequency { get; init; } = "Monthly";

    [JsonPropertyName("weekdays")]
    public List<short>? Weekdays { get; init; }

    [JsonPropertyName("day_of_month")]
    public int? DayOfMonth { get; init; }

    [JsonPropertyName("months")]
    public List<int>? Months { get; init; }

    [JsonPropertyName("active")]
    public bool Active { get; init; }

    [JsonPropertyName("last_generated_at")]
    public DateTime? LastGeneratedAt { get; init; }

    [JsonPropertyName("next_occurrence_at")]
    public DateTime? NextOccurrenceAt { get; init; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; init; }

    public int? InvestmentTypeCode => InvestmentType switch
    {
        null => null,
        "CDB" => 0,
        "LCI" => 1,
        "LCA" => 2,
        "RCI" => 3,
        "RCA" => 4,
        "Tesouro" => 5,
        "Debentures" => 6,
        "TitulosPublicos" => 7,
        "CRI" => 8,
        "CRA" => 9,
        "RDB" => 10,
        _ => null
    };

    public int IndexCode => Index switch
    {
        "CDI" => 0,
        "IPCA_MAIS" => 1,
        "PERCENT_YEAR" => 2,
        "CDI_MAIS" => 3,
        _ => 0
    };

    public int FrequencyCode => Frequency switch
    {
        "Weekly" => 0,
        "Monthly" => 1,
        _ => 1
    };
}

public sealed record RecurringInvestmentRequestDto
{
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("investment_type")]
    public int? InvestmentType { get; init; }

    [JsonPropertyName("bank_code")]
    public int BankCode { get; init; }

    [JsonPropertyName("value")]
    public decimal Value { get; init; }

    [JsonPropertyName("index")]
    public int Index { get; init; }

    [JsonPropertyName("index_percent")]
    public decimal IndexPercent { get; init; }

    [JsonPropertyName("index_value")]
    public decimal IndexValue { get; init; }

    [JsonPropertyName("taxes")]
    public bool Taxes { get; init; }

    [JsonPropertyName("liquidity_daily")]
    public bool LiquidityDaily { get; init; }

    [JsonPropertyName("duration_days")]
    public int? DurationDays { get; init; }

    [JsonPropertyName("frequency")]
    public int Frequency { get; init; }

    [JsonPropertyName("weekdays")]
    public List<short> Weekdays { get; init; } = [];

    [JsonPropertyName("day_of_month")]
    public int? DayOfMonth { get; init; }

    [JsonPropertyName("months")]
    public List<int> Months { get; init; } = [];

    [JsonPropertyName("active")]
    public bool Active { get; init; } = true;
}

public sealed record RecurringInvestmentActiveRequestDto(
    [property: JsonPropertyName("active")] bool Active);

public sealed record RecurringOption(string Label, int Value);
