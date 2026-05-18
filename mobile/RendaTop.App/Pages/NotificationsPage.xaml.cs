using RendaTop.App.Models;
using RendaTop.App.Services;

namespace RendaTop.App.Pages;

public partial class NotificationsPage : ContentPage
{
    private readonly NotificationService _notifications;
    private List<NotificationRow> _rows = [];

    public NotificationsPage(NotificationService notifications)
    {
        _notifications = notifications;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async void OnRefresh(object? sender, EventArgs e)
        => await LoadAsync();

    private async void OnReloadClicked(object? sender, EventArgs e)
        => await LoadAsync();

    private async void OnRetryClicked(object? sender, EventArgs e)
        => await LoadAsync();

    private async void OnMarkAllClicked(object? sender, EventArgs e)
    {
        if (_rows.Count == 0 || _rows.All(item => item.IsRead))
            return;

        SetLoading(true);
        HideError();

        try
        {
            await _notifications.MarkAllAsReadAsync();
            var now = DateTime.Now;
            _rows = _rows
                .Select(item => item.IsRead ? item : item.MarkAsRead(now))
                .ToList();
            ApplyRows();
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel marcar as notificacoes como lidas.");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async void OnMarkReadClicked(object? sender, EventArgs e)
    {
        if (sender is not BindableObject { BindingContext: NotificationRow row } || row.IsRead)
            return;

        SetLoading(true);
        HideError();

        try
        {
            await _notifications.MarkAsReadAsync(row.Id);
            var now = DateTime.Now;
            _rows = _rows
                .Select(item => item.Id == row.Id ? item.MarkAsRead(now) : item)
                .ToList();
            ApplyRows();
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel marcar a notificacao como lida.");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async Task LoadAsync()
    {
        SetLoading(true);
        HideError();

        try
        {
            var items = await _notifications.GetRecentNotificationsAsync();
            await _notifications.RefreshUnreadCountAsync();

            _rows = items
                .Select(NotificationRow.FromDto)
                .OrderByDescending(item => item.CreatedAt)
                .ToList();

            ApplyRows();
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel carregar suas notificacoes.");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private void ApplyRows()
    {
        NotificationsCollection.ItemsSource = _rows;

        var unread = _rows.Count(item => !item.IsRead);
        UnreadLabel.Text = unread.ToString();
        TotalLabel.Text = _rows.Count.ToString();
        MarkAllButton.IsEnabled = unread > 0;
        EmptyLabel.IsVisible = _rows.Count == 0;
    }

    private void SetLoading(bool loading)
    {
        LoadingIndicator.IsRunning = loading;
        LoadingIndicator.IsVisible = loading;
        Refresh.IsRefreshing = false;
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorBorder.IsVisible = true;
    }

    private void HideError()
    {
        ErrorLabel.Text = string.Empty;
        ErrorBorder.IsVisible = false;
    }

    private sealed record NotificationRow(
        Guid Id,
        string Title,
        string Message,
        bool IsRead,
        DateTime CreatedAt,
        string CreatedAtLabel,
        bool CanMarkAsRead,
        string StatusText,
        Color StatusBackground,
        Color StatusStroke,
        Color StatusColor)
    {
        public static NotificationRow FromDto(NotificationItemDto item)
            => Create(item.Id, item.Title, item.Message, item.IsRead, item.CreatedAt);

        public NotificationRow MarkAsRead(DateTime readAt)
            => Create(Id, Title, Message, true, CreatedAt);

        private static NotificationRow Create(Guid id, string title, string message, bool isRead, DateTime createdAt)
            => new(
                id,
                title,
                message,
                isRead,
                createdAt,
                createdAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                !isRead,
                isRead ? "Lida" : "Nao lida",
                isRead ? Color.FromArgb("#F8FAFC") : Color.FromArgb("#EFF6FF"),
                isRead ? Color.FromArgb("#CBD5E1") : Color.FromArgb("#93C5FD"),
                isRead ? Color.FromArgb("#475569") : Color.FromArgb("#1D4ED8"));
    }
}
