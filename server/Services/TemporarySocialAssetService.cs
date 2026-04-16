using Microsoft.Extensions.Caching.Memory;
using server.Utils;

namespace server.Services;

public interface ITemporarySocialAssetService
{
    string Store(string fileName, string contentType, byte[] content, HttpRequest? request = null, TimeSpan? ttl = null);
    bool TryGet(string token, out TemporarySocialAssetValue? asset);
}

public sealed record TemporarySocialAssetValue(
    string FileName,
    string ContentType,
    byte[] Content,
    DateTime ExpiresAtUtc);

public sealed class TemporarySocialAssetService : ITemporarySocialAssetService
{
    private readonly IMemoryCache _memoryCache;

    public TemporarySocialAssetService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public string Store(string fileName, string contentType, byte[] content, HttpRequest? request = null, TimeSpan? ttl = null)
    {
        var token = Guid.NewGuid().ToString("N");
        var effectiveTtl = ttl ?? TimeSpan.FromHours(2);
        var expiresAtUtc = DateTime.UtcNow.Add(effectiveTtl);

        _memoryCache.Set(
            BuildCacheKey(token),
            new TemporarySocialAssetValue(fileName, contentType, content, expiresAtUtc),
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = effectiveTtl,
                Size = Math.Max(1, content.Length)
            });

        return BlogUrlBuilder.BuildTemporarySocialAssetUrl(token, request);
    }

    public bool TryGet(string token, out TemporarySocialAssetValue? asset)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            asset = null;
            return false;
        }

        return _memoryCache.TryGetValue(BuildCacheKey(token), out asset);
    }

    private static string BuildCacheKey(string token) => $"blog-social-temp-asset:{token}";
}
