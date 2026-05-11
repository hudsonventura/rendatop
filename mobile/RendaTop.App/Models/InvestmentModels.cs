using System.Globalization;
using System.Text.Json.Serialization;

namespace RendaTop.App.Models;

public sealed record InvestmentDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("date_buy")]
    public DateTime DateBuy { get; init; }

    [JsonPropertyName("due_date")]
    public DateTime? DueDate { get; init; }

    [JsonPropertyName("value")]
    public decimal Value { get; init; }

    [JsonPropertyName("table_value")]
    public decimal? TableValue { get; init; }

    [JsonPropertyName("archived")]
    public bool Archived { get; init; }

    [JsonPropertyName("bank")]
    public BankDto? Bank { get; init; }

    [JsonPropertyName("calculated")]
    public List<CalculatedDto>? Calculated { get; init; }

    [JsonPropertyName("table_calculated")]
    public List<CalculatedDto>? TableCalculated { get; init; }

    public decimal PrincipalForDisplay => Math.Max(0m, TableValue ?? Value);

    public decimal CurrentValueForDisplay =>
        Math.Max(0m, TableCalculated?.FirstOrDefault()?.ValueLiq
                     ?? Calculated?.FirstOrDefault()?.ValueLiq
                     ?? PrincipalForDisplay);
}

public sealed record BankDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = "Banco";

    [JsonPropertyName("color")]
    public string Color { get; init; } = "#94A3B8";

    [JsonPropertyName("code")]
    public int Code { get; init; }

    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? $"Banco {Code}" : Name;
}

public sealed record CalculatedDto
{
    [JsonPropertyName("value_liq")]
    public decimal ValueLiq { get; init; }

    [JsonPropertyName("profit_liq")]
    public decimal ProfitLiq { get; init; }
}

public sealed record InvestmentRequestDto
{
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("investment_type")]
    public string? InvestmentType { get; init; }

    [JsonPropertyName("money_box_id")]
    public Guid? MoneyBoxId { get; init; }

    [JsonPropertyName("bank_code")]
    public int BankCode { get; init; }

    [JsonPropertyName("date_buy")]
    public DateTime DateBuy { get; init; }

    [JsonPropertyName("date_expected_sell")]
    public DateTime? DateExpectedSell { get; init; }

    [JsonPropertyName("value")]
    public decimal Value { get; init; }

    [JsonPropertyName("index")]
    public string Index { get; init; } = "CDI";

    [JsonPropertyName("index_percent")]
    public decimal IndexPercent { get; init; }

    [JsonPropertyName("index_value")]
    public decimal IndexValue { get; init; }

    [JsonPropertyName("taxes")]
    public bool Taxes { get; init; } = true;

    [JsonPropertyName("archived")]
    public bool Archived { get; init; }

    [JsonPropertyName("ai_extracted")]
    public bool AiExtracted { get; init; }
}

public sealed record InvestmentOption(string Label, string Value);

public sealed record InvestmentIndexOption(string Label, string Value);

public sealed record BankAllocationItem(
    string BankName,
    string Color,
    string Amount,
    string PercentText,
    double Percent);

public sealed record DueSoonItem(
    string Title,
    string BankName,
    string DueDate,
    string Amount,
    string DaysText);

public sealed record DashboardSummary(
    string Invested,
    string Current,
    string Profit,
    string DueSoonCount,
    IReadOnlyList<BankAllocationItem> BankAllocation,
    IReadOnlyList<DueSoonItem> DueSoon);

public static class MoneyFormatter
{
    private static readonly CultureInfo Brazil = new("pt-BR");

    public static string Currency(decimal value) => value.ToString("C", Brazil);
}
