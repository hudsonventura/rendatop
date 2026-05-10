namespace RendaTop.App.Services;

public sealed class AppConfig
{
#if DEBUG
    public const string DefaultApiBaseUrl = "http://10.10.1.100:5000";
#else
    public const string DefaultApiBaseUrl = "https://rendatop.com.br";
#endif

    public Uri ApiBaseUri { get; } = new(DefaultApiBaseUrl.TrimEnd('/') + "/");

    public Uri BuildApiUri(string path)
    {
        var normalized = path.TrimStart('/');
        return new Uri(ApiBaseUri, normalized);
    }
}
