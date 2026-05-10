using RendaTop.App.Models;

namespace RendaTop.App.Services;

public sealed class SessionService
{
    private const string NameKey = "rendatop.user.name";
    private const string EmailKey = "rendatop.user.email";
    private const string UserTypeKey = "rendatop.user.type";

    private readonly ApiClient _apiClient;

    public SessionService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public string Name => Preferences.Default.Get(NameKey, string.Empty);
    public string Email => Preferences.Default.Get(EmailKey, string.Empty);
    public string UserType => Preferences.Default.Get(UserTypeKey, string.Empty);

    public async Task InitializeAsync() => await _apiClient.RestoreSessionCookieAsync();

    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            await _apiClient.GetAsync("/Authenticated");
            return true;
        }
        catch
        {
            await ClearAsync();
            return false;
        }
    }

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
