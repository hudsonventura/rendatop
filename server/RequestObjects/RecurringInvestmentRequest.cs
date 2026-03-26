using server.Domain;

namespace server.RequestObjects;

public record RecurringInvestmentRequest(
    string title,
    int bank_code,
    decimal value,
    IdexesType index,
    decimal index_percent,
    decimal index_value,
    bool taxes,
    bool liquidity_daily,
    int? duration_days,
    RecurringInvestmentFrequency frequency,
    List<short>? weekdays,
    int? day_of_month,
    List<int>? months,
    bool active = true
);
