namespace RendaTop.App.Models;

public enum AppPlatform
{
    Unknown = 0,
    Android = 1,
    iOS = 2,
    MacCatalyst = 3,
    Windows = 4
}

public enum NativeBillingProvider
{
    None = 0,
    GooglePlay = 1,
    AppleAppStore = 2
}

public enum PaymentCheckoutKind
{
    NativeStore = 0,
    BackendCheckout = 1
}
