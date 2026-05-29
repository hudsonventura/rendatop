using RendaTop.App.Models;

namespace RendaTop.App.Services;

public sealed class RecurringInvestmentService
{
    private const string CacheKey = "recurring-investments-overview";

    private readonly ApiClient _apiClient;
    private readonly LocalSnapshotStore _snapshots;
    private readonly WalletService _wallets;

    public RecurringInvestmentService(ApiClient apiClient, LocalSnapshotStore snapshots, WalletService wallets)
    {
        _apiClient = apiClient;
        _snapshots = snapshots;
        _wallets = wallets;
    }

    public async Task<RecurringInvestmentsOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var overview = await _apiClient.GetAsync<RecurringInvestmentsOverviewDto>(_wallets.WithActiveWallet("/Investments/Recurring"), cancellationToken)
           ?? new RecurringInvestmentsOverviewDto { Items = [] };

        await _snapshots.SetAsync(CacheKey, overview, cancellationToken);
        return overview;
    }

    public async Task<RecurringInvestmentsOverviewDto> GetCachedOverviewAsync(CancellationToken cancellationToken = default)
        => await _snapshots.GetAsync<RecurringInvestmentsOverviewDto>(CacheKey, cancellationToken)
           ?? new RecurringInvestmentsOverviewDto { Items = [] };

    public async Task<RecurringInvestmentDto> CreateAsync(RecurringInvestmentRequestDto request, CancellationToken cancellationToken = default)
        => await _apiClient.PostAsync<RecurringInvestmentRequestDto, RecurringInvestmentDto>("/Investments/Recurring", request with { WalletId = _wallets.ActiveWalletId }, cancellationToken)
           ?? throw new ApiException("Resposta de recorrencia invalida.", 500);

    public async Task<RecurringInvestmentDto> UpdateAsync(Guid id, RecurringInvestmentRequestDto request, CancellationToken cancellationToken = default)
        => await _apiClient.PatchAsync<RecurringInvestmentRequestDto, RecurringInvestmentDto>($"/Investments/Recurring/{id}", request with { WalletId = _wallets.ActiveWalletId }, cancellationToken)
           ?? throw new ApiException("Resposta de recorrencia invalida.", 500);

    public async Task<RecurringInvestmentDto> UpdateActiveAsync(Guid id, bool active, CancellationToken cancellationToken = default)
        => await _apiClient.PatchAsync<RecurringInvestmentActiveRequestDto, RecurringInvestmentDto>(
            $"/Investments/Recurring/{id}/active",
            new RecurringInvestmentActiveRequestDto(active),
            cancellationToken)
           ?? throw new ApiException("Resposta de recorrencia invalida.", 500);

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => _apiClient.DeleteAsync($"/Investments/Recurring/{id}", cancellationToken);
}
