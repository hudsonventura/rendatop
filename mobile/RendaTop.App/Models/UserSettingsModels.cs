using System.Text.Json.Serialization;

namespace RendaTop.App.Models;

public sealed record UserSettingsDto
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("phone")]
    public string Phone { get; init; } = string.Empty;

    [JsonPropertyName("cpf")]
    public string Cpf { get; init; } = string.Empty;

    [JsonPropertyName("notify_whatsapp")]
    public bool NotifyWhatsapp { get; init; }

    [JsonPropertyName("notify_telegram")]
    public bool NotifyTelegram { get; init; }

    [JsonPropertyName("telegram_chat_id")]
    public string? TelegramChatId { get; init; }

    [JsonPropertyName("notify_email")]
    public bool NotifyEmail { get; init; }

    [JsonPropertyName("notify_browser")]
    public bool NotifyBrowser { get; init; }

    [JsonPropertyName("calendar_public_enabled")]
    public bool CalendarPublicEnabled { get; init; }

    [JsonPropertyName("calendar_public_url")]
    public string? CalendarPublicUrl { get; init; }

    [JsonPropertyName("totp_enabled")]
    public bool TotpEnabled { get; init; }

    [JsonPropertyName("whatsapp_notifications_enabled")]
    public bool WhatsappNotificationsEnabled { get; init; }

    [JsonPropertyName("calendar_ics_enabled")]
    public bool CalendarIcsEnabled { get; init; }

    [JsonPropertyName("ai_document_extraction_enabled")]
    public bool AiDocumentExtractionEnabled { get; init; }

    [JsonPropertyName("ai_document_extraction_current_usage")]
    public int AiDocumentExtractionCurrentUsage { get; init; }

    [JsonPropertyName("ai_document_extraction_monthly_limit")]
    public int AiDocumentExtractionMonthlyLimit { get; init; }

    [JsonPropertyName("ai_document_extraction_restriction_message")]
    public string? AiDocumentExtractionRestrictionMessage { get; init; }

    [JsonPropertyName("user_type")]
    public string UserType { get; init; } = string.Empty;

    [JsonPropertyName("pending_email")]
    public string? PendingEmail { get; init; }

    [JsonPropertyName("pending_email_verification_sent")]
    public bool? PendingEmailVerificationSent { get; init; }
}

public sealed record UserSettingsUpdateRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("password")] string? Password,
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("notify_whatsapp")] bool NotifyWhatsapp,
    [property: JsonPropertyName("notify_telegram")] bool NotifyTelegram,
    [property: JsonPropertyName("telegram_chat_id")] string? TelegramChatId,
    [property: JsonPropertyName("notify_email")] bool NotifyEmail,
    [property: JsonPropertyName("calendar_public_enabled")] bool CalendarPublicEnabled);

public sealed record UserSettingsNotificationTestRequest(
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("telegram_chat_id")] string? TelegramChatId);

public sealed record PendingEmailCodeRequest(
    [property: JsonPropertyName("code")] string Code);

public sealed record TotpSetupDto(
    [property: JsonPropertyName("secret")] string Secret,
    [property: JsonPropertyName("otpauth_uri")] string OtpAuthUri,
    [property: JsonPropertyName("account")] string Account);

public sealed record TotpEnableRequestDto(
    [property: JsonPropertyName("secret")] string Secret,
    [property: JsonPropertyName("code")] string Code);

public sealed record TotpDisableRequestDto(
    [property: JsonPropertyName("code")] string Code);
