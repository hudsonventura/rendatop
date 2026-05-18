using RendaTop.App.Models;

namespace RendaTop.App.Services;

public sealed class StoreBillingService
{
    private readonly AppPlatformService _platform;

    public StoreBillingService(AppPlatformService platform)
    {
        _platform = platform;
    }

    public StoreCheckoutInfo GetCheckoutInfo(PlanDto plan)
    {
        if (!_platform.UsesNativeStoreBilling)
        {
            return new StoreCheckoutInfo(
                PaymentCheckoutKind.BackendCheckout,
                NativeBillingProvider.None,
                _platform.PlatformDisplayName,
                "Checkout externo",
                true,
                "Pagamento direto",
                "Neste ambiente, o app usa o checkout direto ja existente no backend.",
                "Continuar");
        }

        var provider = _platform.NativeBillingProvider;
        var providerDisplay = _platform.NativeBillingProviderDisplayName;

        return new StoreCheckoutInfo(
            PaymentCheckoutKind.NativeStore,
            provider,
            _platform.PlatformDisplayName,
            providerDisplay,
            false,
            $"Assinar {plan.Name} via {providerDisplay}",
            provider == NativeBillingProvider.GooglePlay
                ? "No Android, assinaturas pagas devem ser concluídas pelo Google Play Billing."
                : "No iOS, assinaturas pagas devem ser concluídas pelo App Store / StoreKit.",
            provider == NativeBillingProvider.GooglePlay ? "Comprar na Google Play" : "Comprar na App Store",
            "A estrutura do app ja esta pronta, mas a compra nativa ainda depende da configuracao do billing da loja e da validacao de recibo no backend.");
    }

    public Task<StorePurchaseResult> StartPurchaseAsync(PlanDto plan, CancellationToken cancellationToken = default)
    {
        var info = GetCheckoutInfo(plan);
        if (info.CheckoutKind != PaymentCheckoutKind.NativeStore)
        {
            return Task.FromResult(new StorePurchaseResult(true, "Use o checkout direto deste ambiente."));
        }

        return Task.FromResult(new StorePurchaseResult(false, info.BlockingReason ?? "Pagamento nativo indisponivel."));
    }
}
