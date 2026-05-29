using RendaTop.App.Controls;
using RendaTop.App.Models;
using RendaTop.App.Services;

namespace RendaTop.App.Pages;

public partial class RecurringInvestmentsPage : ContentPage
{
    private readonly RecurringInvestmentService _service;
    private readonly ConnectivityService _connectivity;
    private readonly WalletService _wallets;
    private readonly NotificationTitleView _titleView;
    private bool _enabled;
    private bool _walletSubscribed;

    public RecurringInvestmentsPage(RecurringInvestmentService service, ConnectivityService connectivity, NotificationService notifications, WalletService wallets)
    {
        _service = service;
        _connectivity = connectivity;
        _wallets = wallets;
        InitializeComponent();
        _titleView = NotificationChrome.Apply(this, "Investimentos Recorrentes", notifications);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _ = _titleView.RefreshAsync();
        SubscribeWalletChanges();
        await LoadAsync();
    }

    protected override void OnDisappearing()
    {
        UnsubscribeWalletChanges();
        base.OnDisappearing();
    }

    private async void OnRefresh(object? sender, EventArgs e)
        => await LoadAsync();

    private async void OnRetryClicked(object? sender, EventArgs e)
        => await LoadAsync();

    private void SubscribeWalletChanges()
    {
        if (_walletSubscribed)
            return;

        _wallets.ActiveWalletChanged += OnActiveWalletChanged;
        _walletSubscribed = true;
    }

    private void UnsubscribeWalletChanges()
    {
        if (!_walletSubscribed)
            return;

        _wallets.ActiveWalletChanged -= OnActiveWalletChanged;
        _walletSubscribed = false;
    }

    private void OnActiveWalletChanged(object? sender, Guid walletId)
        => MainThread.BeginInvokeOnMainThread(async () => await LoadAsync());

    private async void OnCreateClicked(object? sender, EventArgs e)
        => await OpenCreateAsync();

    private async void OnEditClicked(object? sender, EventArgs e)
    {
        if (sender is BindableObject { BindingContext: RecurringInvestmentRow row })
            await Shell.Current.GoToAsync($"{nameof(EditRecurringInvestmentPage)}?recurringInvestmentId={row.Id}");
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (sender is not BindableObject { BindingContext: RecurringInvestmentRow row })
            return;

        var confirmed = await DisplayAlertAsync(
            "Excluir recorrencia",
            $"Deseja excluir a recorrencia \"{row.Title}\"?",
            "Excluir",
            "Cancelar");

        if (!confirmed)
            return;

        SetLoading(true);
        HideError();

        try
        {
            await _service.DeleteAsync(row.Id);
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel excluir a recorrencia.");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async void OnActiveToggled(object? sender, ToggledEventArgs e)
    {
        if (sender is not Switch toggle || toggle.BindingContext is not RecurringInvestmentRow row)
            return;

        toggle.IsEnabled = false;
        HideError();

        try
        {
            await _service.UpdateActiveAsync(row.Id, e.Value);

            if (IsRecurringScheduledForToday(row with { Active = e.Value }))
                ShowNotice("Se a recorrencia era para hoje, aguarde alguns segundos e confira o investimento criado na sua carteira.");

            await LoadAsync();
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
            toggle.IsToggled = row.Active;
        }
        catch
        {
            ShowError("Nao foi possivel atualizar a recorrencia.");
            toggle.IsToggled = row.Active;
        }
        finally
        {
            toggle.IsEnabled = true;
        }
    }

    private async Task OpenCreateAsync()
    {
        if (!_enabled)
        {
            await DisplayAlertAsync(
                "Recurso Premium",
                "Investimentos recorrentes exigem um plano pago ativo. Se voce ja tinha recorrencias cadastradas, elas continuam visiveis aqui, mas a geracao automatica fica pausada sem um plano elegivel.",
                "OK");
            return;
        }

        await Shell.Current.GoToAsync(nameof(EditRecurringInvestmentPage));
    }

    private async Task LoadAsync()
    {
        SetLoading(true);
        HideError();

        try
        {
            var offline = _connectivity.IsOffline;
            var overview = offline
                ? await _service.GetCachedOverviewAsync()
                : await _service.GetOverviewAsync();

            _enabled = overview.RecurringInvestmentsEnabled;
            OfflineBorder.IsVisible = offline;
            PremiumBorder.IsVisible = !_enabled;
            CreateButton.IsEnabled = _enabled && !offline;

            var rows = (overview.Items ?? [])
                .OrderByDescending(item => item.Active)
                .ThenBy(item => item.Title)
                .Select(item => RecurringInvestmentRow.FromDto(item, !offline))
                .ToList();

            ItemsCollection.ItemsSource = rows;
            EmptyLabel.IsVisible = rows.Count == 0;
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel carregar os investimentos recorrentes.");
        }
        finally
        {
            SetLoading(false);
        }
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

    private void ShowNotice(string message)
    {
        NoticeLabel.Text = message;
        NoticeBorder.IsVisible = true;
    }

    private static bool IsRecurringScheduledForToday(RecurringInvestmentRow item)
    {
        if (!item.Active)
            return false;

        var today = DateTime.Today;
        if (item.Frequency == 0)
            return item.Weekdays.Contains((short)today.DayOfWeek);

        if (!item.Months.Contains(today.Month))
            return false;

        var lastDay = DateTime.DaysInMonth(today.Year, today.Month);
        return today.Day == Math.Min(item.DayOfMonth ?? 1, lastDay);
    }

    private sealed record RecurringInvestmentRow(
        Guid Id,
        string Title,
        string BankName,
        string InvestmentTypeLabel,
        string ValueLabel,
        string IndexLabel,
        string DurationLabel,
        string FrequencyLabel,
        string LastGeneratedLabel,
        string NextOccurrenceLabel,
        bool Active,
        int Frequency,
        List<short> Weekdays,
        List<int> Months,
        int? DayOfMonth,
        bool CanWrite,
        string StatusLabel,
        Color StatusBackground,
        Color StatusStroke,
        Color StatusText)
    {
        public static RecurringInvestmentRow FromDto(RecurringInvestmentDto item, bool canWrite)
        {
            var active = item.Active;
            return new RecurringInvestmentRow(
                item.Id,
                item.Title,
                item.BankName,
                GetInvestmentTypeLabel(item.InvestmentTypeCode),
                MoneyFormatter.Currency(item.Value),
                GetIndexLabel(item.IndexCode, item.IndexPercent),
                item.LiquidityDaily ? "Liquidez diaria" : $"{item.DurationDays ?? 0} dia(s)",
                GetFrequencyLabel(item),
                FormatDate(item.LastGeneratedAt),
                FormatDate(item.NextOccurrenceAt),
                active,
                item.FrequencyCode,
                item.Weekdays ?? [],
                item.Months ?? [],
                item.DayOfMonth,
                canWrite,
                active ? "Ativa" : "Pausada",
                active ? Color.FromArgb("#ECFDF5") : Color.FromArgb("#F8FAFC"),
                active ? Color.FromArgb("#86EFAC") : Color.FromArgb("#CBD5E1"),
                active ? Color.FromArgb("#166534") : Color.FromArgb("#475569"));
        }

        private static string GetInvestmentTypeLabel(int? investmentType)
            => investmentType switch
            {
                null => "Nao informado",
                0 => "CDB",
                1 => "LCI",
                2 => "LCA",
                3 => "RCI",
                4 => "RCA",
                5 => "Tesouro",
                6 => "Debentures",
                7 => "Titulos publicos",
                8 => "CRI",
                9 => "CRA",
                10 => "RDB",
                _ => "Nao informado"
            };

        private static string GetIndexLabel(int index, decimal percent)
        {
            var formatted = percent.ToString("N2", new System.Globalization.CultureInfo("pt-BR"));
            return index switch
            {
                0 => $"{formatted}% CDI",
                1 => $"IPCA + {formatted}%",
                3 => $"CDI + {formatted}% a.a.",
                _ => $"{formatted}% a.a."
            };
        }

        private static string GetFrequencyLabel(RecurringInvestmentDto item)
        {
            if (item.FrequencyCode == 0)
            {
                var labels = (item.Weekdays ?? [])
                    .Select(GetWeekdayLabel)
                    .Where(label => !string.IsNullOrWhiteSpace(label));
                return $"Semanal · {string.Join(", ", labels)}";
            }

            var months = (item.Months ?? [])
                .Select(GetMonthLabel)
                .Where(label => !string.IsNullOrWhiteSpace(label));
            return $"Mensal · dia {item.DayOfMonth ?? 1} · {string.Join(", ", months)}";
        }

        private static string FormatDate(DateTime? value)
            => value.HasValue ? value.Value.ToLocalTime().ToString("dd/MM/yyyy") : "Ainda nao gerado";

        private static string GetWeekdayLabel(short day)
            => day switch
            {
                0 => "Domingo",
                1 => "Segunda",
                2 => "Terca",
                3 => "Quarta",
                4 => "Quinta",
                5 => "Sexta",
                6 => "Sabado",
                _ => string.Empty
            };

        private static string GetMonthLabel(int month)
            => month switch
            {
                1 => "Jan",
                2 => "Fev",
                3 => "Mar",
                4 => "Abr",
                5 => "Mai",
                6 => "Jun",
                7 => "Jul",
                8 => "Ago",
                9 => "Set",
                10 => "Out",
                11 => "Nov",
                12 => "Dez",
                _ => string.Empty
            };
    }
}
