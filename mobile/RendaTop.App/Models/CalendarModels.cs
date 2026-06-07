using Plugin.Maui.Calendar.Interfaces;

namespace RendaTop.App.Models;

public enum CalendarEventType
{
    Start = 0,
    Due = 1,
    Redemption = 2
}

public sealed record CalendarEventItem : IPersonalizableDayEvent
{
    public CalendarEventItem(
        string id,
        Guid investmentId,
        InvestmentDto investment,
        string title,
        DateTime date,
        CalendarEventType type)
    {
        Id = id;
        InvestmentId = investmentId;
        Investment = investment;
        Title = title;
        Date = date;
        Type = type;

        var indicatorColor = type == CalendarEventType.Start
            ? Color.FromArgb("#16A34A")
            : Color.FromArgb("#2563EB");

        EventIndicatorColor = indicatorColor;
        EventIndicatorSelectedColor = indicatorColor;
    }

    public string Id { get; }

    public Guid InvestmentId { get; }

    public InvestmentDto Investment { get; }

    public string Title { get; }

    public DateTime Date { get; }

    public CalendarEventType Type { get; }

    public string TypeLabel => Type switch
    {
        CalendarEventType.Start => "Aplicacao",
        CalendarEventType.Due => "Vencimento",
        CalendarEventType.Redemption => "Resgate",
        _ => "Evento"
    };

    public Color TypeColor => Type switch
    {
        CalendarEventType.Start => Color.FromArgb("#16A34A"),
        CalendarEventType.Due => Color.FromArgb("#2563EB"),
        CalendarEventType.Redemption => Color.FromArgb("#EA580C"),
        _ => Color.FromArgb("#64748B")
    };

    public Color? EventIndicatorColor { get; set; }

    public Color? EventIndicatorSelectedColor { get; set; }

    public Color? EventIndicatorTextColor { get; set; }

    public Color? EventIndicatorSelectedTextColor { get; set; }

    public string DateLabel => Date.ToString("dd/MM/yyyy");

    public string BankName => Investment.Bank?.Name ?? "Banco desconhecido";

    public string InvestedValueLabel => MoneyFormatter.Currency(Investment.Value);

    public string RelatedAmountLabel => Type == CalendarEventType.Redemption
        ? MoneyFormatter.Currency((Investment.Redemptions ?? [])
            .Where(item => item.Date.ToLocalTime().Date == Date)
            .Sum(item => item.Value))
        : InvestedValueLabel;

    public string CurrentNetValueLabel => MoneyFormatter.Currency(
        Investment.Calculated?.FirstOrDefault()?.ValueLiq ?? Investment.CurrentValueForDisplay);

    public string CurrentProfitLabel => MoneyFormatter.Currency(
        Investment.Calculated?.FirstOrDefault()?.ProfitLiq ?? 0m);

    public string IndexLabel => Investment.Index switch
    {
        "PERCENT_YEAR" => $"{Investment.IndexPercent:N2}% a.a.",
        "CDI" => $"{Investment.IndexPercent:N2}% CDI",
        "CDI_MAIS" => $"CDI + {Investment.IndexPercent:N2}% a.a.",
        "IPCA_MAIS" => $"IPCA+{Investment.IndexPercent:N2}%",
        _ => $"{Investment.IndexPercent:N2}%"
    };
}
