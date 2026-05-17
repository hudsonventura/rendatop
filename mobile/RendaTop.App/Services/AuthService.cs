using RendaTop.App.Models;

namespace RendaTop.App.Services;

public sealed class AuthService
{
    private readonly AppConfig _config;
    private readonly ApiClient _apiClient;
    private readonly SessionService _session;
    private readonly InvestmentCacheService _investmentCache;
    private readonly NotificationService _notifications;
    private readonly LocalSnapshotStore _snapshots;

    public AuthService(AppConfig config, ApiClient apiClient, SessionService session, InvestmentCacheService investmentCache, NotificationService notifications, LocalSnapshotStore snapshots)
    {
        _config = config;
        _apiClient = apiClient;
        _session = session;
        _investmentCache = investmentCache;
        _notifications = notifications;
        _snapshots = snapshots;
    }

    public Uri GoogleLoginUri => _apiClient.BuildUri("/auth/google/login");
    public Uri MicrosoftLoginUri => _apiClient.BuildUri("/auth/microsoft/login");

    public async Task LoginWithGoogleAsync()
    {
        var startUri = _apiClient.BuildUri("/auth/google/login?client=mobile");
        var callbackUri = _config.MobileAuthCallbackUri;

        WebAuthenticatorResult authResult;
        try
        {
            authResult = await WebAuthenticator.Default.AuthenticateAsync(startUri, callbackUri);
        }
        catch (TaskCanceledException)
        {
            throw new ApiException("Login com Google cancelado.", 400);
        }

        if (!authResult.Properties.TryGetValue("status", out var status) || string.IsNullOrWhiteSpace(status))
            throw new ApiException("Nao foi possivel concluir o login com Google.", 400);

        if (!string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
        {
            var errorMessage = authResult.Properties.TryGetValue("message", out var message) && !string.IsNullOrWhiteSpace(message)
                ? message
                : "Nao foi possivel concluir o login com Google.";

            throw new ApiException(errorMessage, 400);
        }

        if (!authResult.Properties.TryGetValue("handoff_token", out var handoffToken) || string.IsNullOrWhiteSpace(handoffToken))
            throw new ApiException("Token de login social ausente. Tente novamente.", 400);

        var response = await _apiClient.PostAsync<MobileSessionExchangeRequest, LoginResponse>(
            "/auth/mobile/session",
            new MobileSessionExchangeRequest(handoffToken));

        if (response is null)
            throw new ApiException("Resposta de login social invalida.", 500);

        await _investmentCache.ClearAsync();
        await _snapshots.ClearAsync();
        _notifications.Clear();
        await _session.SaveLoginAsync(response);
    }

    public async Task<LoginStartResponse> LoginAsync(string email, string password)
    {
        var response = await _apiClient.PostAsync<LoginRequest, LoginStartResponse>(
            "/login",
            new LoginRequest(email.Trim(), password));

        if (response is null)
            throw new ApiException("Resposta de login invalida.", 500);

        if (!response.RequiresTotp)
        {
            await _investmentCache.ClearAsync();
            await _snapshots.ClearAsync();
            _notifications.Clear();
            await _session.SaveLoginAsync(response);
        }

        return response;
    }

    public async Task LoginTotpAsync(string challengeId, string code)
    {
        var response = await _apiClient.PostAsync<TotpLoginRequest, LoginResponse>(
            "/login/totp",
            new TotpLoginRequest(challengeId.Trim(), code.Trim()));

        if (response is null)
            throw new ApiException("Resposta de TOTP invalida.", 500);

        await _investmentCache.ClearAsync();
        await _snapshots.ClearAsync();
        _notifications.Clear();
        await _session.SaveLoginAsync(response);
    }

    public async Task<SignupPendingResponse> SignupAsync(string name, string email, string password)
    {
        var response = await _apiClient.PostAsync<SignupRequest, SignupPendingResponse>(
            "/signup",
            new SignupRequest(name.Trim(), email.Trim(), password));

        return response ?? throw new ApiException("Resposta de cadastro invalida.", 500);
    }

    public async Task VerifySignupAsync(string email, string code)
    {
        var response = await _apiClient.PostAsync<SignupVerificationRequest, LoginResponse>(
            "/signup/verify",
            new SignupVerificationRequest(email.Trim(), code.Trim()));

        if (response is null)
            throw new ApiException("Resposta de verificacao invalida.", 500);

        await _investmentCache.ClearAsync();
        await _snapshots.ClearAsync();
        _notifications.Clear();
        await _session.SaveLoginAsync(response);
    }

    public async Task<string> ResendSignupVerificationAsync(string email)
    {
        var response = await _apiClient.PostAsync<SignupVerificationResendRequest, MessageResponse>(
            "/signup/verification/resend",
            new SignupVerificationResendRequest(email.Trim()));

        return response?.Message ?? "Novo codigo de verificacao enviado para seu email.";
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _apiClient.PostAsync("/logout", new { });
        }
        finally
        {
            await _investmentCache.ClearAsync();
            await _snapshots.ClearAsync();
            _notifications.Clear();
            await _session.ClearAsync();
        }
    }
}
