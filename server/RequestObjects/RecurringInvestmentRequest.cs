using server.Domain;

namespace server.RequestObjects;

public class RecurringInvestmentRequest
{
    public string title { get; set; } = string.Empty;
    public InvestmentType? investment_type { get; set; }
    public Guid? wallet_id { get; set; }
    public int bank_code { get; set; }
    public decimal value { get; set; }
    public IdexesType index { get; set; }
    public decimal index_percent { get; set; }
    public decimal index_value { get; set; }
    public bool taxes { get; set; }
    public bool liquidity_daily { get; set; }
    public int? duration_days { get; set; }
    public RecurringInvestmentFrequency frequency { get; set; }
    public List<short>? weekdays { get; set; }
    public int? day_of_month { get; set; }
    public List<int>? months { get; set; }
    public bool active { get; set; } = true;
}
