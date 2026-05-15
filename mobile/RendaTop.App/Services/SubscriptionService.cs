using RendaTop.App.Models;

namespace RendaTop.App.Services;

public sealed class SubscriptionService
{
    private readonly ApiClient _apiClient;

    public SubscriptionService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IReadOnlyList<PlanDto>> GetPlansAsync(CancellationToken cancellationToken = default)
        => await _apiClient.GetAsync<List<PlanDto>>("/plans", cancellationToken) ?? [];

    public async Task<SubscriptionOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
        => await _apiClient.GetAsync<SubscriptionOverviewDto>("/subscription/overview", cancellationToken)
           ?? new SubscriptionOverviewDto();

    public async Task<PaymentResultDto> SubscribeWithPixAsync(PixSubscriptionRequestDto request, CancellationToken cancellationToken = default)
        => await _apiClient.PostAsync<PixSubscriptionRequestDto, PaymentResultDto>("/subscription/pix", request, cancellationToken)
           ?? throw new ApiException("Resposta invalida ao gerar PIX.", 500);

    public async Task<PaymentResultDto> SubscribeWithBoletoAsync(BoletoSubscriptionRequestDto request, CancellationToken cancellationToken = default)
        => await _apiClient.PostAsync<BoletoSubscriptionRequestDto, PaymentResultDto>("/subscription/boleto", request, cancellationToken)
           ?? throw new ApiException("Resposta invalida ao gerar boleto.", 500);

    public async Task<PaymentResultDto> SubscribeWithCardAsync(CardSubscriptionRequestDto request, CancellationToken cancellationToken = default)
        => await _apiClient.PostAsync<CardSubscriptionRequestDto, PaymentResultDto>("/subscription/card", request, cancellationToken)
           ?? throw new ApiException("Resposta invalida ao processar cartao.", 500);

    public async Task<PaymentResultDto> GetPaymentStatusAsync(string paymentId, CancellationToken cancellationToken = default)
        => await _apiClient.GetAsync<PaymentResultDto>($"/subscription/payment-status/{paymentId}", cancellationToken)
           ?? throw new ApiException("Nao foi possivel consultar o status do pagamento.", 500);

    public async Task<string> CancelActiveSubscriptionAsync(string mode, CancellationToken cancellationToken = default)
    {
        var response = await _apiClient.PostAsync<CancelSubscriptionRequestDto, CancelSubscriptionResultDto>(
            "/subscription/cancel-active",
            new CancelSubscriptionRequestDto(true, mode),
            cancellationToken);

        return response?.Message ?? string.Empty;
    }

    public Task CancelPendingSubscriptionAsync(CancellationToken cancellationToken = default)
        => _apiClient.PostAsync("/subscription/cancel-pending", new { }, cancellationToken);

    public async Task<string> RevertScheduledCancellationAsync(CancellationToken cancellationToken = default)
    {
        var response = await _apiClient.PostAsync<RevertScheduledCancellationRequestDto, CancelSubscriptionResultDto>(
            "/subscription/cancel-scheduled/revert",
            new RevertScheduledCancellationRequestDto(true),
            cancellationToken);

        return response?.Message ?? string.Empty;
    }
}
