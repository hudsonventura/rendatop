namespace RendaTop.App.Services;

public sealed class AppConfig
{
#if DEBUG
    public const string DefaultApiBaseUrl = "http://10.10.1.100:5000";
#else
    public const string DefaultApiBaseUrl = "https://rendatop.com.br";
#endif
    public const string MercadoPagoPublicKey = "TEST-ce3a9e6-2390-4060-8d13-6ff3e48be91c3";
    public const string MobileAuthCallbackUrl = "br.com.rendatop.app://auth/callback";

    public Uri ApiBaseUri { get; } = new(DefaultApiBaseUrl.TrimEnd('/') + "/");
    public Uri MobileAuthCallbackUri { get; } = new(MobileAuthCallbackUrl);

    public Uri BuildApiUri(string path)
    {
        var normalized = path.TrimStart('/');
        return new Uri(ApiBaseUri, normalized);
    }
}
