using RendaTop.App.Models;

namespace RendaTop.App.Services;

public sealed class UserSettingsService
{
    private const string CacheKey = "user-settings";

    private readonly ApiClient _apiClient;
    private readonly LocalSnapshotStore _snapshots;

    public UserSettingsService(ApiClient apiClient, LocalSnapshotStore snapshots)
    {
        _apiClient = apiClient;
        _snapshots = snapshots;
    }

    public async Task<UserSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _apiClient.GetAsync<UserSettingsDto>("/User/Settings", cancellationToken)
           ?? throw new ApiException("Nao foi possivel carregar suas configuracoes.", 500);

        await _snapshots.SetAsync(CacheKey, settings, cancellationToken);
        return settings;
    }

    public async Task<UserSettingsDto?> GetCachedAsync(CancellationToken cancellationToken = default)
        => await _snapshots.GetAsync<UserSettingsDto>(CacheKey, cancellationToken);

    public async Task<UserSettingsDto> UpdateAsync(UserSettingsUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var settings = await _apiClient.PatchAsync<UserSettingsUpdateRequest, UserSettingsDto>("/User/Settings", request, cancellationToken)
           ?? throw new ApiException("Resposta invalida ao salvar configuracoes.", 500);

        await _snapshots.SetAsync(CacheKey, settings, cancellationToken);
        return settings;
    }

    public async Task<string> TestTelegramAsync(string? telegramChatId, CancellationToken cancellationToken = default)
    {
        var response = await _apiClient.PostAsync<UserSettingsNotificationTestRequest, MessageResponse>(
            "/User/Settings/TestTelegram",
            new UserSettingsNotificationTestRequest(null, telegramChatId),
            cancellationToken);

        return response?.Message ?? "Mensagem de teste enviada no Telegram.";
    }

    public async Task<string> TestWhatsAppAsync(string? phone, CancellationToken cancellationToken = default)
    {
        var response = await _apiClient.PostAsync<UserSettingsNotificationTestRequest, MessageResponse>(
            "/User/Settings/TestWhatsApp",
            new UserSettingsNotificationTestRequest(phone, null),
            cancellationToken);

        return response?.Message ?? "Mensagem de teste enviada no WhatsApp.";
    }

    public async Task<string> TestEmailAsync(CancellationToken cancellationToken = default)
    {
        var response = await _apiClient.PostAsync<object, MessageResponse>(
            "/User/Settings/TestEmail",
            new { },
            cancellationToken);

        return response?.Message ?? "Mensagem de teste enviada por Email.";
    }

    public async Task<UserSettingsDto> VerifyPendingEmailAsync(string code, CancellationToken cancellationToken = default)
    {
        var settings = await _apiClient.PostAsync<PendingEmailCodeRequest, UserSettingsDto>(
            "/User/Settings/Email/Verify",
            new PendingEmailCodeRequest(code),
            cancellationToken)
           ?? throw new ApiException("Nao foi possivel verificar o novo email.", 500);

        await _snapshots.SetAsync(CacheKey, settings, cancellationToken);
        return settings;
    }

    public async Task<string> ResendPendingEmailAsync(CancellationToken cancellationToken = default)
    {
        var response = await _apiClient.PostAsync<object, MessageResponse>(
            "/User/Settings/Email/Resend",
            new { },
            cancellationToken);

        return response?.Message ?? "Novo codigo de verificacao enviado para seu novo email.";
    }

    public async Task<UserSettingsDto> CancelPendingEmailAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _apiClient.PostAsync<object, UserSettingsDto>(
            "/User/Settings/Email/Cancel",
            new { },
            cancellationToken)
           ?? throw new ApiException("Nao foi possivel cancelar a alteracao de email.", 500);

        await _snapshots.SetAsync(CacheKey, settings, cancellationToken);
        return settings;
    }

    public async Task<TotpSetupDto> GenerateTotpAsync(CancellationToken cancellationToken = default)
        => await _apiClient.PostAsync<object, TotpSetupDto>(
            "/User/Settings/Totp/Generate",
            new { },
            cancellationToken)
           ?? throw new ApiException("Nao foi possivel gerar o QR Code TOTP.", 500);

    public async Task<UserSettingsDto> EnableTotpAsync(string secret, string code, CancellationToken cancellationToken = default)
    {
        var settings = await _apiClient.PostAsync<TotpEnableRequestDto, UserSettingsDto>(
            "/User/Settings/Totp/Enable",
            new TotpEnableRequestDto(secret, code),
            cancellationToken)
           ?? throw new ApiException("Nao foi possivel habilitar TOTP.", 500);

        await _snapshots.SetAsync(CacheKey, settings, cancellationToken);
        return settings;
    }

    public async Task<UserSettingsDto> DisableTotpAsync(string code, CancellationToken cancellationToken = default)
    {
        var settings = await _apiClient.PostAsync<TotpDisableRequestDto, UserSettingsDto>(
            "/User/Settings/Totp/Disable",
            new TotpDisableRequestDto(code),
            cancellationToken)
           ?? throw new ApiException("Nao foi possivel desabilitar TOTP.", 500);

        await _snapshots.SetAsync(CacheKey, settings, cancellationToken);
        return settings;
    }
}
