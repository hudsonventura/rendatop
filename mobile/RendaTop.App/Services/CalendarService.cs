using RendaTop.App.Models;

namespace RendaTop.App.Services;

public sealed class CalendarService
{
    private readonly InvestmentService _investments;

    public CalendarService(InvestmentService investments)
    {
        _investments = investments;
    }

    public async Task<IReadOnlyList<CalendarEventItem>> GetCachedEventsAsync(CancellationToken cancellationToken = default)
        => BuildEvents(await _investments.GetCachedInvestmentsAsync(cancellationToken));

    public async Task<IReadOnlyList<CalendarEventItem>> GetEventsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<InvestmentDto> source;

        if (forceRefresh)
        {
            source = await _investments.GetInvestmentsAsync(forceRefresh: true, cancellationToken);
        }
        else
        {
            source = await _investments.GetCachedInvestmentsAsync(cancellationToken);
            if (source.Count == 0)
                source = await _investments.GetInvestmentsAsync(forceRefresh: true, cancellationToken);
        }

        return BuildEvents(source);
    }

    public async Task<IReadOnlyList<CalendarEventItem>> GetEventsForDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        var items = await GetCachedEventsAsync(cancellationToken);
        if (items.Count == 0)
            items = await GetEventsAsync(forceRefresh: true, cancellationToken);

        return items
            .Where(item => item.Date.Date == date.Date)
            .OrderBy(item => item.Type)
            .ThenBy(item => item.Title)
            .ToList();
    }

    public async Task<CalendarEventItem> GetEventAsync(Guid investmentId, CalendarEventType type, DateTime date, CancellationToken cancellationToken = default)
    {
        var items = await GetEventsForDateAsync(date, cancellationToken);
        var match = items.FirstOrDefault(item => item.InvestmentId == investmentId && item.Type == type);
        if (match is not null)
            return match;

        var refreshed = await GetEventsAsync(forceRefresh: true, cancellationToken);
        return refreshed.FirstOrDefault(item => item.InvestmentId == investmentId && item.Type == type && item.Date.Date == date.Date)
            ?? throw new ApiException("Evento do calendario nao encontrado.", 404);
    }

    public static IReadOnlyList<CalendarEventItem> BuildEvents(IReadOnlyList<InvestmentDto> investments)
    {
        var events = new List<CalendarEventItem>();

        foreach (var investment in investments)
        {
            events.Add(new CalendarEventItem(
                $"{investment.Id}-start",
                investment.Id,
                investment,
                investment.Title,
                investment.DateBuy.ToLocalTime().Date,
                CalendarEventType.Start));

            if (investment.DueDate.HasValue)
            {
                events.Add(new CalendarEventItem(
                    $"{investment.Id}-due",
                    investment.Id,
                    investment,
                    investment.Title,
                    investment.DueDate.Value.ToLocalTime().Date,
                    CalendarEventType.Due));
            }
        }

        return events;
    }
}
