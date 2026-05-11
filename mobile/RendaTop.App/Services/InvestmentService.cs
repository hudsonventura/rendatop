using RendaTop.App.Models;

namespace RendaTop.App.Services;

public sealed class InvestmentService
{
    private readonly ApiClient _apiClient;

    public InvestmentService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IReadOnlyList<InvestmentDto>> GetInvestmentsAsync(CancellationToken cancellationToken = default)
    {
        var investments = await _apiClient.GetAsync<List<InvestmentDto>>("/Investments", cancellationToken);
        return investments ?? [];
    }

    public async Task<IReadOnlyList<BankDto>> GetBanksAsync(CancellationToken cancellationToken = default)
    {
        var banks = await _apiClient.GetAsync<List<BankDto>>("/Banks", cancellationToken);
        return banks ?? [];
    }

    public async Task CreateInvestmentAsync(InvestmentRequestDto request, CancellationToken cancellationToken = default)
    {
        await _apiClient.PostAsync<InvestmentRequestDto, Guid>("/Investments", request, cancellationToken);
    }

    public async Task<DashboardSummary> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        var investments = await GetInvestmentsAsync(cancellationToken);
        var active = investments.Where(item => !item.Archived).ToList();
        var invested = active.Sum(item => item.PrincipalForDisplay);
        var current = active.Sum(item => item.CurrentValueForDisplay);
        var profit = current - invested;
        var today = DateTime.Today;
        var dueLimit = today.AddDays(30);

        var dueSoon = active
            .Where(item => item.DueDate.HasValue)
            .Select(item => new { Investment = item, Due = item.DueDate!.Value.Date })
            .Where(item => item.Due <= dueLimit)
            .OrderBy(item => item.Due)
            .Select(item =>
            {
                var days = (item.Due - today).Days;
                var daysText = days switch
                {
                    < 0 => "Vencido",
                    0 => "Hoje",
                    1 => "Amanha",
                    _ => $"{days} dias"
                };

                return new DueSoonItem(
                    item.Investment.Title,
                    item.Investment.Bank?.Name ?? "Banco",
                    item.Due.ToString("dd/MM/yyyy"),
                    MoneyFormatter.Currency(item.Investment.CurrentValueForDisplay),
                    daysText);
            })
            .ToList();

        var bankAllocation = active
            .GroupBy(item => item.Bank?.Name ?? "Banco")
            .Select(group =>
            {
                var amount = group.Sum(item => item.CurrentValueForDisplay);
                var first = group.FirstOrDefault();
                var percent = current <= 0m ? 0d : decimal.ToDouble(amount / current);

                return new BankAllocationItem(
                    group.Key,
                    first?.Bank?.Color ?? "#94A3B8",
                    MoneyFormatter.Currency(amount),
                    percent.ToString("P1"),
                    Math.Clamp(percent, 0d, 1d));
            })
            .OrderByDescending(item => item.Percent)
            .ToList();

        return new DashboardSummary(
            MoneyFormatter.Currency(invested),
            MoneyFormatter.Currency(current),
            MoneyFormatter.Currency(profit),
            dueSoon.Count.ToString(),
            bankAllocation,
            dueSoon);
    }
}
