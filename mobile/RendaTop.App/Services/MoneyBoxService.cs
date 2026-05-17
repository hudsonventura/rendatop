using RendaTop.App.Models;

namespace RendaTop.App.Services;

public sealed class MoneyBoxService
{
    private const string CacheKey = "moneyboxes-overview";

    private readonly ApiClient _apiClient;
    private readonly LocalSnapshotStore _snapshots;

    public MoneyBoxService(ApiClient apiClient, LocalSnapshotStore snapshots)
    {
        _apiClient = apiClient;
        _snapshots = snapshots;
    }

    public async Task<MoneyBoxesOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var overview = await _apiClient.GetAsync<MoneyBoxesOverviewDto>("/MoneyBoxes", cancellationToken)
           ?? new MoneyBoxesOverviewDto { Items = [] };

        await _snapshots.SetAsync(CacheKey, overview, cancellationToken);
        return overview;
    }

    public async Task<MoneyBoxesOverviewDto> GetCachedOverviewAsync(CancellationToken cancellationToken = default)
        => await _snapshots.GetAsync<MoneyBoxesOverviewDto>(CacheKey, cancellationToken)
           ?? new MoneyBoxesOverviewDto { Items = [] };

    public async Task<MoneyBoxDto> CreateAsync(string name, CancellationToken cancellationToken = default)
        => await _apiClient.PostAsync<MoneyBoxRequestDto, MoneyBoxDto>("/MoneyBoxes", new MoneyBoxRequestDto(name.Trim()), cancellationToken)
           ?? throw new ApiException("Resposta de cofrinho invalida.", 500);

    public async Task<MoneyBoxDto> UpdateAsync(Guid id, string name, CancellationToken cancellationToken = default)
        => await _apiClient.PatchAsync<MoneyBoxRequestDto, MoneyBoxDto>($"/MoneyBoxes/{id}", new MoneyBoxRequestDto(name.Trim()), cancellationToken)
           ?? throw new ApiException("Resposta de cofrinho invalida.", 500);

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => _apiClient.DeleteAsync($"/MoneyBoxes/{id}", cancellationToken);
}
