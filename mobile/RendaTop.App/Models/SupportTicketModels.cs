using System.Text.Json.Serialization;

namespace RendaTop.App.Models;

public static class SupportScope
{
    public const string Open = "open";
    public const string Archived = "archived";
    public const string All = "all";
}

public static class SupportStatus
{
    public const string AguardandoAtendimento = "AguardandoAtendimento";
    public const string EmAtendimento = "EmAtendimento";
    public const string AguardandoRespostaUsuario = "AguardandoRespostaUsuario";
    public const string Encerrado = "Encerrado";
    public const string Cancelado = "Cancelado";
}

public sealed record SupportTicketListResponseDto
{
    [JsonPropertyName("items")]
    public List<SupportTicketListItemDto>? Items { get; init; }

    [JsonPropertyName("counts")]
    public SupportTicketListCountsDto? Counts { get; init; }
}

public sealed record SupportTicketListCountsDto
{
    [JsonPropertyName("open_count")]
    public int OpenCount { get; init; }

    [JsonPropertyName("archived_count")]
    public int ArchivedCount { get; init; }

    [JsonPropertyName("waiting_admin_count")]
    public int WaitingAdminCount { get; init; }

    [JsonPropertyName("waiting_user_count")]
    public int WaitingUserCount { get; init; }
}

public sealed record SupportTicketListItemDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("subject")]
    public string Subject { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("is_archived")]
    public bool IsArchived { get; init; }

    [JsonPropertyName("pending_for")]
    public string? PendingFor { get; init; }

    [JsonPropertyName("requester_user_name")]
    public string RequesterUserName { get; init; } = string.Empty;

    [JsonPropertyName("requester_user_email")]
    public string RequesterUserEmail { get; init; } = string.Empty;

    [JsonPropertyName("latest_sender_user_name")]
    public string? LatestSenderUserName { get; init; }

    [JsonPropertyName("latest_message_preview")]
    public string? LatestMessagePreview { get; init; }

    [JsonPropertyName("message_count")]
    public int MessageCount { get; init; }

    [JsonPropertyName("last_message_at")]
    public DateTime LastMessageAt { get; init; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; }
}

public sealed record SupportTicketDetailDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("subject")]
    public string Subject { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("is_archived")]
    public bool IsArchived { get; init; }

    [JsonPropertyName("pending_for")]
    public string? PendingFor { get; init; }

    [JsonPropertyName("requester_user_name")]
    public string RequesterUserName { get; init; } = string.Empty;

    [JsonPropertyName("requester_user_email")]
    public string RequesterUserEmail { get; init; } = string.Empty;

    [JsonPropertyName("can_current_user_reply")]
    public bool CanCurrentUserReply { get; init; }

    [JsonPropertyName("messages")]
    public List<SupportTicketMessageDto>? Messages { get; init; }

    [JsonPropertyName("last_message_at")]
    public DateTime LastMessageAt { get; init; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; init; }

    [JsonPropertyName("archived_at")]
    public DateTime? ArchivedAt { get; init; }
}

public sealed record SupportTicketMessageDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("sender_user_id")]
    public Guid SenderUserId { get; init; }

    [JsonPropertyName("sender_user_type")]
    public string SenderUserType { get; init; } = string.Empty;

    [JsonPropertyName("sender_user_name")]
    public string SenderUserName { get; init; } = string.Empty;

    [JsonPropertyName("body_html")]
    public string BodyHtml { get; init; } = string.Empty;

    [JsonPropertyName("body_text")]
    public string BodyText { get; init; } = string.Empty;

    [JsonPropertyName("attachments")]
    public List<SupportTicketAttachmentDto>? Attachments { get; init; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; }
}

public sealed record SupportTicketAttachmentDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("file_name")]
    public string FileName { get; init; } = string.Empty;

    [JsonPropertyName("content_type")]
    public string ContentType { get; init; } = string.Empty;

    [JsonPropertyName("size_bytes")]
    public long SizeBytes { get; init; }

    [JsonPropertyName("is_image")]
    public bool IsImage { get; init; }
}
