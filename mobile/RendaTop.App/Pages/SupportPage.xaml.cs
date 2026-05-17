using RendaTop.App.Controls;
using RendaTop.App.Models;
using RendaTop.App.Services;

namespace RendaTop.App.Pages;

public partial class SupportPage : ContentPage
{
    private const string AllStatusesValue = "__all__";

    private readonly SupportTicketService _service;
    private readonly ConnectivityService _connectivity;
    private readonly NotificationTitleView _titleView;
    private CancellationTokenSource? _loadCts;
    private bool _filtersReady;

    public SupportPage(SupportTicketService service, ConnectivityService connectivity, NotificationService notifications)
    {
        _service = service;
        _connectivity = connectivity;
        InitializeComponent();
        _titleView = NotificationChrome.Apply(this, "Atendimento", notifications);
        ConfigureFilters();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _titleView.RefreshAsync();
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        _ = LoadAsync(_loadCts.Token);
    }

    protected override void OnDisappearing()
    {
        _loadCts?.Cancel();
        base.OnDisappearing();
    }

    private void ConfigureFilters()
    {
        var scopeOptions = new List<FilterOption>
        {
            new(SupportScope.Open, "Abertos"),
            new(SupportScope.Archived, "Arquivados"),
            new(SupportScope.All, "Todos")
        };

        var statusOptions = new List<FilterOption>
        {
            new(AllStatusesValue, "Todos os status"),
            new(SupportStatus.AguardandoAtendimento, "Aguardando atendimento"),
            new(SupportStatus.EmAtendimento, "Em atendimento"),
            new(SupportStatus.AguardandoRespostaUsuario, "Aguardando resposta do usuario"),
            new(SupportStatus.Encerrado, "Encerrado"),
            new(SupportStatus.Cancelado, "Cancelado")
        };

        ScopePicker.ItemsSource = scopeOptions;
        ScopePicker.SelectedIndex = 0;

        StatusPicker.ItemsSource = statusOptions;
        StatusPicker.SelectedIndex = 0;

        _filtersReady = true;
    }

    private async void OnRefresh(object? sender, EventArgs e)
        => await LoadAsync(CancellationToken.None, showLoading: false);

    private async void OnRetryClicked(object? sender, EventArgs e)
        => await LoadAsync(CancellationToken.None, showLoading: true);

    private async void OnCreateClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(CreateSupportTicketPage));

    private async void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not SupportTicketRow row)
            return;

        TicketsCollection.SelectedItem = null;
        await Shell.Current.GoToAsync($"{nameof(SupportTicketDetailsPage)}?ticketId={row.Id}");
    }

    private async void OnFiltersChanged(object? sender, EventArgs e)
    {
        if (!_filtersReady)
            return;

        await LoadAsync(CancellationToken.None, showLoading: true);
    }

    private async void OnSearchClicked(object? sender, EventArgs e)
        => await LoadAsync(CancellationToken.None, showLoading: true);

    private async void OnSearchCompleted(object? sender, EventArgs e)
        => await LoadAsync(CancellationToken.None, showLoading: true);

    private async void OnClearSearchClicked(object? sender, EventArgs e)
    {
        SearchEntry.Text = string.Empty;
        await LoadAsync(CancellationToken.None, showLoading: true);
    }

    private async Task LoadAsync(CancellationToken cancellationToken, bool showLoading = true)
    {
        if (showLoading)
            SetLoading(true);

        HideError();

        try
        {
            SupportTicketListResponseDto response;
            if (_connectivity.IsOffline)
            {
                response = await _service.GetCachedTicketsAsync(cancellationToken);
            }
            else
            {
                response = await _service.RefreshAllTicketsCacheAsync(cancellationToken);
            }

            ApplyCounts(response.Counts);
            ApplyTickets(FilterTickets(response.Items ?? []));
            OfflineBorder.IsVisible = _connectivity.IsOffline;
            CreateButton.IsEnabled = !_connectivity.IsOffline;
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel carregar seus chamados.");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private string GetSelectedScope()
        => (ScopePicker.SelectedItem as FilterOption)?.Value ?? SupportScope.Open;

    private string? GetSelectedStatus()
    {
        var value = (StatusPicker.SelectedItem as FilterOption)?.Value ?? AllStatusesValue;
        return value == AllStatusesValue ? null : value;
    }

    private void ApplyCounts(SupportTicketListCountsDto? counts)
    {
        OpenCountLabel.Text = (counts?.OpenCount ?? 0).ToString();
        ArchivedCountLabel.Text = (counts?.ArchivedCount ?? 0).ToString();
        WaitingAdminCountLabel.Text = (counts?.WaitingAdminCount ?? 0).ToString();
        WaitingUserCountLabel.Text = (counts?.WaitingUserCount ?? 0).ToString();
    }

    private void ApplyTickets(IReadOnlyList<SupportTicketListItemDto> source)
    {
        var rows = source
            .OrderBy(item => item.IsArchived ? 1 : 0)
            .ThenByDescending(item => item.LastMessageAt)
            .Select(SupportTicketRow.FromDto)
            .ToList();

        TicketsCollection.ItemsSource = rows;
        EmptyLabel.IsVisible = rows.Count == 0;
    }

    private IReadOnlyList<SupportTicketListItemDto> FilterTickets(IReadOnlyList<SupportTicketListItemDto> source)
    {
        IEnumerable<SupportTicketListItemDto> query = source;
        var scope = GetSelectedScope();
        var status = GetSelectedStatus();
        var search = (SearchEntry.Text ?? string.Empty).Trim();

        query = scope switch
        {
            SupportScope.Open => query.Where(item => !item.IsArchived),
            SupportScope.Archived => query.Where(item => item.IsArchived),
            _ => query
        };

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(item => string.Equals(item.Status, status, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(item =>
                item.Subject.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (item.LatestMessagePreview?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        return query.ToList();
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

    private sealed record FilterOption(string Value, string Label);

    private sealed record SupportTicketRow(
        Guid Id,
        string Subject,
        string Preview,
        string StatusLabel,
        Color StatusBackground,
        Color StatusStroke,
        Color StatusText,
        string PendingLabel,
        Color PendingBackground,
        Color PendingStroke,
        Color PendingText,
        string LastMessageAtLabel,
        string CreatedAtLabel,
        string MessageCountLabel)
    {
        public static SupportTicketRow FromDto(SupportTicketListItemDto item)
        {
            var (statusBg, statusStroke, statusText) = item.Status switch
            {
                SupportStatus.AguardandoAtendimento => Palette("#FEF3C7", "#FCD34D", "#92400E"),
                SupportStatus.EmAtendimento => Palette("#DBEAFE", "#93C5FD", "#1D4ED8"),
                SupportStatus.AguardandoRespostaUsuario => Palette("#EDE9FE", "#C4B5FD", "#6D28D9"),
                SupportStatus.Encerrado => Palette("#DCFCE7", "#86EFAC", "#166534"),
                SupportStatus.Cancelado => Palette("#E5E7EB", "#D1D5DB", "#374151"),
                _ => Palette("#F1F5F9", "#E2E8F0", "#334155")
            };

            var (pendingLabel, pendingBg, pendingStroke, pendingText) = item.PendingFor switch
            {
                "admin" => ("Aguardando time", Color.FromArgb("#FFFBEB"), Color.FromArgb("#FCD34D"), Color.FromArgb("#92400E")),
                "user" => ("Aguardando voce", Color.FromArgb("#F5F3FF"), Color.FromArgb("#C4B5FD"), Color.FromArgb("#6D28D9")),
                _ => ("Sem pendencia", Color.FromArgb("#F8FAFC"), Color.FromArgb("#CBD5E1"), Color.FromArgb("#475569"))
            };

            var preview = string.IsNullOrWhiteSpace(item.LatestMessagePreview)
                ? "Sem previa disponivel."
                : item.LatestMessagePreview!.Trim();

            return new SupportTicketRow(
                item.Id,
                item.Subject,
                preview,
                GetStatusLabel(item.Status),
                statusBg,
                statusStroke,
                statusText,
                pendingLabel,
                pendingBg,
                pendingStroke,
                pendingText,
                item.LastMessageAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                $"Criado em {item.CreatedAt.ToLocalTime():dd/MM/yyyy HH:mm}",
                item.MessageCount == 1 ? "1 mensagem" : $"{item.MessageCount} mensagens");
        }

        private static string GetStatusLabel(string status)
            => status switch
            {
                SupportStatus.AguardandoAtendimento => "Aguardando atendimento",
                SupportStatus.EmAtendimento => "Em atendimento",
                SupportStatus.AguardandoRespostaUsuario => "Aguardando resposta do usuario",
                SupportStatus.Encerrado => "Encerrado",
                SupportStatus.Cancelado => "Cancelado",
                _ => status
            };

        private static (Color, Color, Color) Palette(string background, string stroke, string text)
            => (Color.FromArgb(background), Color.FromArgb(stroke), Color.FromArgb(text));
    }
}
