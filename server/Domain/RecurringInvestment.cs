using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using server.RequestObjects;

namespace server.Domain;

public enum RecurringInvestmentFrequency
{
    Weekly = 0,
    Monthly = 1
}

public class RecurringInvestment
{
    [Key]
    public Guid id { get; set; } = SnowflakeGuid.NewGuid();

    [ForeignKey("owner_id")]
    [JsonIgnore]
    public User owner { get; set; } = null!;
    public Guid owner_id { get; set; }

    public Guid bank_id { get; set; }
    [ForeignKey(nameof(bank_id))]
    public Bank bank { get; set; } = null!;

    public string title { get; set; } = string.Empty;
    public decimal value { get; set; }
    public IdexesType index { get; set; }
    public decimal index_percent { get; set; }
    public decimal index_value { get; set; } = 0m;
    public bool taxes { get; set; } = true;

    public bool liquidity_daily { get; set; } = false;
    public int? duration_days { get; set; }

    public RecurringInvestmentFrequency frequency { get; set; } = RecurringInvestmentFrequency.Monthly;
    [Column(TypeName = "smallint[]")]
    public List<short> weekdays { get; set; } = [];
    public int? day_of_month { get; set; }
    public string months_csv { get; set; } = string.Empty;

    public bool active { get; set; } = true;
    public DateTime? last_generated_at { get; set; }
    public DateTime created_at { get; set; } = DateTime.UtcNow;
    public DateTime updated_at { get; set; } = DateTime.UtcNow;

    public RecurringInvestment() { }

    public RecurringInvestment(RecurringInvestmentRequest request, User owner, Bank bank)
    {
        this.owner = owner;
        this.owner_id = owner.id;
        this.bank = bank;
        this.bank_id = bank.Id;
        Apply(request, bank);
    }

    public void Apply(RecurringInvestmentRequest request, Bank bank)
    {
        title = request.title.Trim();
        this.bank = bank;
        bank_id = bank.Id;
        value = request.value;
        index = request.index;
        index_percent = request.index_percent;
        index_value = request.index_value;
        taxes = request.taxes;
        liquidity_daily = request.liquidity_daily;
        duration_days = request.liquidity_daily ? null : request.duration_days;
        frequency = request.frequency;
        weekdays = request.frequency == RecurringInvestmentFrequency.Weekly
            ? NormalizeWeekdays(request.weekdays)
            : [];
        day_of_month = request.frequency == RecurringInvestmentFrequency.Monthly ? request.day_of_month : null;
        months_csv = request.frequency == RecurringInvestmentFrequency.Monthly
            ? SerializeMonths(request.months)
            : string.Empty;
        active = request.active;
        updated_at = DateTime.UtcNow;
    }

    public List<int> GetMonths()
    {
        if (string.IsNullOrWhiteSpace(months_csv))
            return [];

        return months_csv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var month) ? month : 0)
            .Where(month => month >= 1 && month <= 12)
            .Distinct()
            .OrderBy(month => month)
            .ToList();
    }

    public bool MatchesDate(DateOnly date)
    {
        if (!active)
            return false;

        if (frequency == RecurringInvestmentFrequency.Weekly)
            return weekdays.Contains((short)date.DayOfWeek);

        var months = GetMonths();
        if (!day_of_month.HasValue || months.Count == 0 || !months.Contains(date.Month))
            return false;

        return date.Day == Math.Min(day_of_month.Value, DateTime.DaysInMonth(date.Year, date.Month));
    }

    public DateOnly? GetNextOccurrence(DateOnly fromDate)
    {
        if (!active)
            return null;

        if (frequency == RecurringInvestmentFrequency.Weekly)
        {
            if (weekdays.Count == 0)
                return null;

            return Enumerable.Range(0, 7)
                .Select(offset => (DateOnly?)fromDate.AddDays(offset))
                .FirstOrDefault(date => date.HasValue && weekdays.Contains((short)date.Value.DayOfWeek));
        }

        var months = GetMonths();
        if (!day_of_month.HasValue || months.Count == 0)
            return null;

        for (var monthOffset = 0; monthOffset < 24; monthOffset++)
        {
            var probe = fromDate.AddMonths(monthOffset);
            if (!months.Contains(probe.Month))
                continue;

            var occurrenceDay = Math.Min(day_of_month.Value, DateTime.DaysInMonth(probe.Year, probe.Month));
            var occurrence = new DateOnly(probe.Year, probe.Month, occurrenceDay);
            if (occurrence >= fromDate)
                return occurrence;
        }

        return null;
    }

    public InvestmentRequest ToInvestmentRequest(DateOnly occurrenceDate)
    {
        var buyDate = new DateTime(occurrenceDate.Year, occurrenceDate.Month, occurrenceDate.Day, 0, 0, 0, DateTimeKind.Utc);
        DateTime? dueDate = liquidity_daily || !duration_days.HasValue
            ? null
            : buyDate.AddDays(duration_days.Value);

        return new InvestmentRequest
        {
            title = title,
            bank_code = bank.Code,
            date_buy = buyDate,
            date_expected_sell = dueDate,
            value = value,
            index = index,
            index_percent = index_percent,
            index_value = index_value,
            taxes = taxes,
            archived = false
        };
    }

    public static string SerializeMonths(IEnumerable<int>? months)
    {
        if (months is null)
            return string.Empty;

        return string.Join(",",
            months
                .Where(month => month >= 1 && month <= 12)
                .Distinct()
                .OrderBy(month => month));
    }

    public static List<short> NormalizeWeekdays(IEnumerable<short>? weekdays)
    {
        if (weekdays is null)
            return [];

        return weekdays
            .Where(day => day >= 0 && day <= 6)
            .Distinct()
            .OrderBy(day => day)
            .ToList();
    }
}
