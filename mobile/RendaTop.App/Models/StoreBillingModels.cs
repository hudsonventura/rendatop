namespace RendaTop.App.Models;

public sealed record StoreCheckoutInfo(
    PaymentCheckoutKind CheckoutKind,
    NativeBillingProvider BillingProvider,
    string PlatformDisplayName,
    string ProviderDisplayName,
    bool IsNativePurchaseEnabled,
    string Headline,
    string Description,
    string ActionLabel,
    string? BlockingReason = null);

public sealed record StorePurchaseResult(
    bool Started,
    string Message);
