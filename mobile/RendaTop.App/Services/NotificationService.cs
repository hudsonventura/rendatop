using RendaTop.App.Models;

namespace RendaTop.App.Services;

public sealed class NotificationService
{
    private const int MaxNotificationLimit = 100;

    private readonly ApiClient _apiClient;

    public NotificationService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public event EventHandler<int>? UnreadCountChanged;

    public int UnreadCount { get; private set; }

    public async Task<IReadOnlyList<NotificationItemDto>> GetRecentNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var items = await _apiClient.GetAsync<List<NotificationItemDto>>(
            $"/Notifications?limit={MaxNotificationLimit}",
            cancellationToken) ?? [];

        var threshold = DateTime.UtcNow.AddMonths(-3);

        return items
            .Where(item => item.CreatedAt >= threshold)
            .OrderByDescending(item => item.CreatedAt)
            .ToList();
    }

    public async Task<int> RefreshUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        var response = await _apiClient.GetAsync<NotificationUnreadCountDto>("/Notifications/UnreadCount", cancellationToken);
        ApplyUnreadCount(response?.UnreadCount ?? 0);
        return UnreadCount;
    }

    public async Task<int> MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _apiClient.PostAsync<object, NotificationUnreadCountDto>(
            $"/Notifications/{id}/Read",
            new { },
            cancellationToken);

        ApplyUnreadCount(response?.UnreadCount ?? 0);
        return UnreadCount;
    }

    public async Task<int> MarkAllAsReadAsync(CancellationToken cancellationToken = default)
    {
        var response = await _apiClient.PostAsync<object, NotificationUnreadCountDto>(
            "/Notifications/ReadAll",
            new { },
            cancellationToken);

        ApplyUnreadCount(response?.UnreadCount ?? 0);
        return UnreadCount;
    }

    public void Clear() => ApplyUnreadCount(0);

    private void ApplyUnreadCount(int count)
    {
        if (UnreadCount == count)
            return;

        UnreadCount = count;
        UnreadCountChanged?.Invoke(this, count);
    }
}
