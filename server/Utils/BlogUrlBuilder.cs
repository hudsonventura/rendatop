namespace server.Utils;

public static class BlogUrlBuilder
{
    public static string GetApiBaseUrl(HttpRequest? request = null)
    {
        var configured = (Environment.GetEnvironmentVariable("BASE_URL_SERVER") ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(configured))
            return configured.TrimEnd('/');

        if (request is null)
            return string.Empty;

        return $"{request.Scheme}://{request.Host.Value}".TrimEnd('/');
    }

    public static string GetLandingBaseUrl()
    {
        var configured = (Environment.GetEnvironmentVariable("BASE_URL_LANDING") ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(configured))
            return configured.TrimEnd('/');

        var fallback = (Environment.GetEnvironmentVariable("BASE_URL") ?? string.Empty).Trim();
        return fallback.TrimEnd('/');
    }

    public static string BuildPublicAssetUrl(Guid assetId, HttpRequest? request = null)
    {
        var apiBaseUrl = GetApiBaseUrl(request);
        return $"{apiBaseUrl}/public/blog/assets/{assetId}";
    }

    public static string BuildTemporarySocialAssetUrl(string token, HttpRequest? request = null)
    {
        var apiBaseUrl = GetApiBaseUrl(request);
        return $"{apiBaseUrl}/public/blog/social-assets/{token}";
    }

    public static string BuildPublicPostUrl(string slug)
    {
        var landingBaseUrl = GetLandingBaseUrl();
        return $"{landingBaseUrl}/blog/{slug}";
    }
}
