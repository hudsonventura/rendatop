using RendaTop.App.Models;

namespace RendaTop.App.Services;

public sealed class AppPlatformService
{
    public AppPlatform CurrentPlatform
    {
        get
        {
            var platform = DeviceInfo.Current.Platform;
            if (platform == DevicePlatform.Android)
                return AppPlatform.Android;

            if (platform == DevicePlatform.iOS)
                return AppPlatform.iOS;

            if (platform == DevicePlatform.MacCatalyst)
                return AppPlatform.MacCatalyst;

            if (platform == DevicePlatform.WinUI)
                return AppPlatform.Windows;

            return AppPlatform.Unknown;
        }
    }

    public bool IsAndroid => CurrentPlatform == AppPlatform.Android;
    public bool IsiOS => CurrentPlatform == AppPlatform.iOS;
    public bool UsesNativeStoreBilling => IsAndroid || IsiOS;

    public string PlatformDisplayName => CurrentPlatform switch
    {
        AppPlatform.Android => "Android",
        AppPlatform.iOS => "iOS",
        AppPlatform.MacCatalyst => "Mac",
        AppPlatform.Windows => "Windows",
        _ => "Desconhecido"
    };

    public NativeBillingProvider NativeBillingProvider => CurrentPlatform switch
    {
        AppPlatform.Android => NativeBillingProvider.GooglePlay,
        AppPlatform.iOS => NativeBillingProvider.AppleAppStore,
        _ => NativeBillingProvider.None
    };

    public string NativeBillingProviderDisplayName => NativeBillingProvider switch
    {
        NativeBillingProvider.GooglePlay => "Google Play",
        NativeBillingProvider.AppleAppStore => "App Store",
        _ => "Checkout externo"
    };
}
