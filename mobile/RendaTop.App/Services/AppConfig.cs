using System.Diagnostics;

namespace RendaTop.App.Services;

public sealed class AppConfig
{
public static string DefaultApiBaseUrl
    {
        get
        {
            #if DEBUG
                return Debugger.IsAttached
                    ? "http://10.10.1.100:5000"
                    : "https://testing-api.rendatop.com.br";
            #else
                return "https://api.rendatop.com.br";
            #endif
        }  
    }

    public const string MobileAuthCallbackUrl = "br.com.rendatop.app://auth/callback";

    public Uri ApiBaseUri { get; } = new(DefaultApiBaseUrl.TrimEnd('/') + "/");
    public Uri MobileAuthCallbackUri { get; } = new(MobileAuthCallbackUrl);

    public Uri BuildApiUri(string path)
    {
        var normalized = path.TrimStart('/');
        return new Uri(ApiBaseUri, normalized);
    }
}
