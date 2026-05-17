using RendaTop.App.Models;

namespace RendaTop.App.Services;

public sealed class SessionService
{
    private const string NameKey = "rendatop.user.name";
    private const string EmailKey = "rendatop.user.email";
    private const string UserTypeKey = "rendatop.user.type";

    private readonly ApiClient _apiClient;
    private readonly ConnectivityService _connectivity;

    public SessionService(ApiClient apiClient, ConnectivityService connectivity)
    {
        _apiClient = apiClient;
        _connectivity = connectivity;
    }

    public string Name => Preferences.Default.Get(NameKey, string.Empty);
    public string Email => Preferences.Default.Get(EmailKey, string.Empty);
    public string UserType => Preferences.Default.Get(UserTypeKey, string.Empty);

    public async Task InitializeAsync() => await _apiClient.RestoreSessionCookieAsync();

    public async Task<bool> IsAuthenticatedAsync()
    {
        if (_connectivity.IsOffline)
            return await HasOfflineSessionAsync();

        try
        {
            await _apiClient.GetAsync("/Authenticated");
            return true;
        }
        catch (ApiException ex) when (ex.StatusCode is 401 or 403)
        {
            await ClearAsync();
            return false;
        }
        catch
        {
            return await HasOfflineSessionAsync();
        }
    }

    public async Task<bool> HasOfflineSessionAsync()
        => !string.IsNullOrWhiteSpace(Name)
           && !string.IsNullOrWhiteSpace(Email)
           && !string.IsNullOrWhiteSpace(UserType)
           && await _apiClient.HasPersistedSessionCookieAsync();

    public async Task SaveLoginAsync(LoginStartResponse login)
    {
        SaveUser(login.Name, login.Email, login.UserType);
        await _apiClient.PersistSessionCookieAsync();
    }

    public async Task SaveLoginAsync(LoginResponse login)
    {
        SaveUser(login.Name, login.Email, login.UserType);
        await _apiClient.PersistSessionCookieAsync();
    }

    public Task UpdateProfileAsync(string? name, string? email, string? userType)
    {
        SaveUser(name, email, userType);
        return Task.CompletedTask;
    }

    public async Task ClearAsync()
    {
        Preferences.Default.Remove(NameKey);
        Preferences.Default.Remove(EmailKey);
        Preferences.Default.Remove(UserTypeKey);
        await _apiClient.ClearSessionCookieAsync();
    }

    private static void SaveUser(string? name, string? email, string? userType)
    {
        Preferences.Default.Set(NameKey, name ?? string.Empty);
        Preferences.Default.Set(EmailKey, email ?? string.Empty);
        Preferences.Default.Set(UserTypeKey, userType ?? string.Empty);
    }
}
