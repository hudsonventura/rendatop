using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using RendaTop.App.Models;

namespace RendaTop.App.Services;

public sealed class ApiClient
{
    private const string JwtCookieName = "jwt";
    private const string JwtStorageKey = "rendatop.jwt";

    private readonly AppConfig _config;
    private readonly CookieContainer _cookies = new();
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public ApiClient(AppConfig config)
    {
        _config = config;
        var handler = new HttpClientHandler
        {
            CookieContainer = _cookies,
            UseCookies = true,
            AllowAutoRedirect = true
        };

        _http = new HttpClient(handler)
        {
            BaseAddress = _config.ApiBaseUri,
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    public Uri BuildUri(string path) => _config.BuildApiUri(path);

    public async Task RestoreSessionCookieAsync()
    {
        var token = await SafeSecureGetAsync(JwtStorageKey);
        if (string.IsNullOrWhiteSpace(token))
            return;

        _cookies.Add(_config.ApiBaseUri, new Cookie(JwtCookieName, token.Trim()));
    }

    public async Task PersistSessionCookieAsync()
    {
        var token = GetJwtCookieValue();
        if (string.IsNullOrWhiteSpace(token))
            return;

        await SafeSecureSetAsync(JwtStorageKey, token);
    }

    public void ClearSessionCookie()
    {
        _cookies.Add(_config.ApiBaseUri, new Cookie(JwtCookieName, string.Empty)
        {
            Expires = DateTime.UtcNow.AddDays(-1)
        });
    }

    public async Task ClearSessionCookieAsync()
    {
        ClearSessionCookie();
        SecureStorage.Default.Remove(JwtStorageKey);
        await Task.CompletedTask;
    }

    public async Task<bool> HasPersistedSessionCookieAsync()
        => !string.IsNullOrWhiteSpace(await SafeSecureGetAsync(JwtStorageKey));

    public async Task<TResponse?> GetAsync<TResponse>(string path, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync(NormalizeRequestPath(path), cancellationToken);
        return await ReadResponseAsync<TResponse>(response, cancellationToken);
    }

    public async Task GetAsync(string path, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync(NormalizeRequestPath(path), cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string path,
        TRequest body,
        CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync(NormalizeRequestPath(path), body, _jsonOptions, cancellationToken);
        var result = await ReadResponseAsync<TResponse>(response, cancellationToken);
        await PersistSessionCookieAsync();
        return result;
    }

    public async Task PostAsync<TRequest>(string path, TRequest body, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync(NormalizeRequestPath(path), body, _jsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        await PersistSessionCookieAsync();
    }

    public async Task<TResponse?> PostMultipartAsync<TResponse>(
        string path,
        MultipartFormDataContent content,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, NormalizeRequestPath(path))
        {
            Content = content
        };

        using var response = await _http.SendAsync(request, cancellationToken);
        var result = await ReadResponseAsync<TResponse>(response, cancellationToken);
        await PersistSessionCookieAsync();
        return result;
    }

    public async Task PutAsync<TRequest>(string path, TRequest body, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PutAsJsonAsync(NormalizeRequestPath(path), body, _jsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        await PersistSessionCookieAsync();
    }

    public async Task PatchAsync<TRequest>(string path, TRequest body, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, NormalizeRequestPath(path))
        {
            Content = JsonContent.Create(body, options: _jsonOptions)
        };

        using var response = await _http.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        await PersistSessionCookieAsync();
    }

    public async Task<TResponse?> PatchAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, NormalizeRequestPath(path))
        {
            Content = JsonContent.Create(body, options: _jsonOptions)
        };

        using var response = await _http.SendAsync(request, cancellationToken);
        var result = await ReadResponseAsync<TResponse>(response, cancellationToken);
        await PersistSessionCookieAsync();
        return result;
    }

    public async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        using var response = await _http.DeleteAsync(NormalizeRequestPath(path), cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        await PersistSessionCookieAsync();
    }

    private static string NormalizeRequestPath(string path) => path.TrimStart('/');

    private string? GetJwtCookieValue()
    {
        var cookie = _cookies.GetCookies(_config.ApiBaseUri)[JwtCookieName];
        return string.IsNullOrWhiteSpace(cookie?.Value) ? null : cookie.Value;
    }

    private async Task<TResponse?> ReadResponseAsync<TResponse>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        await EnsureSuccessAsync(response, cancellationToken);

        if (response.Content.Headers.ContentLength == 0)
            return default;

        return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions, cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var message = ExtractErrorMessage(content, response.StatusCode);
        throw new ApiException(message, (int)response.StatusCode);
    }

    private static string ExtractErrorMessage(string content, HttpStatusCode statusCode)
    {
        if (string.IsNullOrWhiteSpace(content))
            return $"Nao foi possivel concluir a requisicao. HTTP {(int)statusCode}.";

        try
        {
            var error = JsonSerializer.Deserialize<ErrorResponse>(content, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (!string.IsNullOrWhiteSpace(error?.Message))
                return error.Message;
        }
        catch
        {
            // The backend may return a plain text message for expected errors.
        }

        return content.Trim('"', ' ', '\n', '\r', '\t');
    }

    private static async Task<string?> SafeSecureGetAsync(string key)
    {
        try
        {
            return await SecureStorage.Default.GetAsync(key);
        }
        catch
        {
            var fallback = Preferences.Default.Get(key, string.Empty);
            if (string.IsNullOrWhiteSpace(fallback))
                return null;

            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(fallback));
            }
            catch
            {
                return null;
            }
        }
    }

    private static async Task SafeSecureSetAsync(string key, string value)
    {
        try
        {
            await SecureStorage.Default.SetAsync(key, value);
        }
        catch
        {
            Preferences.Default.Set(key, Convert.ToBase64String(Encoding.UTF8.GetBytes(value)));
        }
    }
}
