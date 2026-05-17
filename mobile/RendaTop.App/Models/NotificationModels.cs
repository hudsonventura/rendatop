using System.Text.Json.Serialization;

namespace RendaTop.App.Models;

public sealed record NotificationItemDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("is_read")]
    public bool IsRead { get; init; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("read_at")]
    public DateTime? ReadAt { get; init; }
}

public sealed record NotificationUnreadCountDto
{
    [JsonPropertyName("unread_count")]
    public int UnreadCount { get; init; }
}
