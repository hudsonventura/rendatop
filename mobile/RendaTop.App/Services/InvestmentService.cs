using RendaTop.App.Models;

namespace RendaTop.App.Services;

public sealed class InvestmentService
{
    private static readonly TimeSpan BackgroundRefreshInterval = TimeSpan.FromMinutes(10);

    private readonly ApiClient _apiClient;
    private readonly InvestmentCacheService _cache;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private Task? _backgroundRefreshTask;

    public InvestmentService(ApiClient apiClient, InvestmentCacheService cache)
    {
        _apiClient = apiClient;
        _cache = cache;
    }

    public Task<IReadOnlyList<InvestmentDto>> GetInvestmentsAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
        => _cache.GetOrFetchAsync(FetchInvestmentsAsync, forceRefresh, cancellationToken);

    public Task<IReadOnlyList<InvestmentDto>> GetCachedInvestmentsAsync(CancellationToken cancellationToken = default)
        => _cache.GetCachedAsync(cancellationToken);

    public bool ShouldRefreshInBackground(IReadOnlyCollection<InvestmentDto> cachedInvestments)
    {
        if (cachedInvestments.Count == 0)
            return false;

        var lastSync = _cache.LastSyncUtc;
        if (!lastSync.HasValue)
            return true;

        return DateTimeOffset.UtcNow - lastSync.Value >= BackgroundRefreshInterval;
    }

    public async Task RefreshInvestmentsCacheAsync(CancellationToken cancellationToken = default)
        => await _cache.SetAsync(await FetchInvestmentsAsync(cancellationToken), cancellationToken);

    public Task RefreshInvestmentsCacheInBackgroundAsync(CancellationToken cancellationToken = default)
    {
        if (_backgroundRefreshTask is { IsCompleted: false })
            return _backgroundRefreshTask;

        _backgroundRefreshTask = Task.Run(async () =>
        {
            await _refreshGate.WaitAsync(cancellationToken);
            try
            {
                await RefreshInvestmentsCacheAsync(cancellationToken);
            }
            finally
            {
                _refreshGate.Release();
            }
        }, cancellationToken);

        return _backgroundRefreshTask;
    }

    private async Task<IReadOnlyList<InvestmentDto>> FetchInvestmentsAsync(CancellationToken cancellationToken)
    {
        var investments = await _apiClient.GetAsync<List<InvestmentDto>>("/Investments", cancellationToken);
        return investments ?? [];
    }

    public async Task<InvestmentDto> GetInvestmentAsync(Guid investmentId, CancellationToken cancellationToken = default)
        => await _apiClient.GetAsync<InvestmentDto>($"/Investments/{investmentId}", cancellationToken)
           ?? throw new ApiException("Investimento nao encontrado.", 404);

    public async Task<InvestmentDto> GetInvestmentWithCalculatedAsync(Guid investmentId, CancellationToken cancellationToken = default)
        => (await GetInvestmentsAsync(cancellationToken: cancellationToken)).FirstOrDefault(item => item.Id == investmentId)
           ?? throw new ApiException("Investimento nao encontrado.", 404);

    public async Task<IReadOnlyList<BankDto>> GetBanksAsync(CancellationToken cancellationToken = default)
    {
        var banks = await _apiClient.GetAsync<List<BankDto>>("/Banks", cancellationToken);
        return banks ?? [];
    }

    public async Task<Guid> CreateInvestmentAsync(InvestmentRequestDto request, CancellationToken cancellationToken = default)
    {
        var investmentId = await _apiClient.PostAsync<InvestmentRequestDto, Guid>("/Investments", request, cancellationToken);
        if (investmentId == Guid.Empty)
            throw new ApiException("Nao foi possivel obter o id do investimento criado.", 500);

        return investmentId;
    }

    public Task UpdateInvestmentAsync(Guid investmentId, InvestmentRequestDto request, CancellationToken cancellationToken = default)
        => _apiClient.PatchAsync($"/Investments/{investmentId}", request, cancellationToken);

    public Task ArchiveInvestmentAsync(Guid investmentId, bool archived = true, CancellationToken cancellationToken = default)
        => _apiClient.PatchAsync(
            $"/Investments/{investmentId}/archive",
            new ArchiveInvestmentRequestDto { Archived = archived },
            cancellationToken);

    public Task DeleteInvestmentAsync(Guid investmentId, CancellationToken cancellationToken = default)
        => _apiClient.DeleteAsync($"/Investments/{investmentId}", cancellationToken);

    public Task RedeemInvestmentAsync(Guid investmentId, RedemptionRequestDto request, CancellationToken cancellationToken = default)
        => _apiClient.PutAsync($"/Investments/{investmentId}", request, cancellationToken);

    public Task UpdateRedemptionAsync(Guid redemptionId, RedemptionRequestDto request, CancellationToken cancellationToken = default)
        => _apiClient.PatchAsync($"/Redemptions/{redemptionId}", request, cancellationToken);

    public Task DeleteRedemptionAsync(Guid redemptionId, CancellationToken cancellationToken = default)
        => _apiClient.DeleteAsync($"/Redemptions/{redemptionId}", cancellationToken);

    public async Task<DashboardSummary> GetDashboardSummaryAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        var investments = await GetInvestmentsAsync(forceRefresh, cancellationToken);
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

    public Task ArchiveInvestmentInCacheAsync(Guid investmentId, bool archived = true, CancellationToken cancellationToken = default)
        => _cache.ArchiveAsync(investmentId, archived, cancellationToken);

    public Task DeleteInvestmentInCacheAsync(Guid investmentId, CancellationToken cancellationToken = default)
        => _cache.RemoveAsync(investmentId, cancellationToken);

    public Task UpsertInvestmentInCacheAsync(InvestmentDto investment, CancellationToken cancellationToken = default)
        => _cache.UpsertAsync(investment, cancellationToken);
}
