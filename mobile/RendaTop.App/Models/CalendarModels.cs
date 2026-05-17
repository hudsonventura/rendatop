namespace RendaTop.App.Models;

public enum CalendarEventType
{
    Start = 0,
    Due = 1
}

public sealed record CalendarEventItem(
    string Id,
    Guid InvestmentId,
    InvestmentDto Investment,
    string Title,
    DateTime Date,
    CalendarEventType Type)
{
    public string TypeLabel => Type == CalendarEventType.Start ? "Aplicacao" : "Vencimento";

    public Color TypeColor => Type == CalendarEventType.Start
        ? Color.FromArgb("#16A34A")
        : Color.FromArgb("#2563EB");

    public string DateLabel => Date.ToString("dd/MM/yyyy");

    public string BankName => Investment.Bank?.Name ?? "Banco desconhecido";

    public string InvestedValueLabel => MoneyFormatter.Currency(Investment.Value);

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
