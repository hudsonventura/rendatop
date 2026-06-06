using System.Globalization;
using System.Windows.Input;
using Plugin.Maui.Calendar.Models;
using RendaTop.App.Controls;
using RendaTop.App.Models;
using RendaTop.App.Services;

namespace RendaTop.App.Pages;

public partial class CalendarPage : ContentPage
{
    private static readonly CultureInfo Brazil = new("pt-BR");

    private readonly InvestmentService _investments;
    private readonly CalendarService _calendar;
    private readonly ConnectivityService _connectivity;
    private readonly WalletService _wallets;
    private readonly NotificationTitleView _titleView;
    private readonly List<CalendarEventItem> _events = [];

    private bool _loaded;
    private bool _walletSubscribed;
    private DateTime _shownDate = DateTime.Today;
    private DateTime _selectedDate = DateTime.Today;
    private EventCollection _calendarEvents = new();

    public CalendarPage(InvestmentService investments, CalendarService calendar, ConnectivityService connectivity, NotificationService notifications, WalletService wallets)
    {
        _investments = investments;
        _calendar = calendar;
        _connectivity = connectivity;
        _wallets = wallets;
        InitializeComponent();
        BindingContext = this;
        _titleView = NotificationChrome.Apply(this, "Calendario", notifications);
        DayTappedCommand = new Command<object?>(async parameter => await OnDayTappedAsync(parameter));
    }

    public CultureInfo CalendarCulture => Brazil;

    public ICommand DayTappedCommand { get; }

    public EventCollection CalendarEvents
    {
        get => _calendarEvents;
        private set
        {
            _calendarEvents = value;
            OnPropertyChanged();
        }
    }

    public DateTime ShownDate
    {
        get => _shownDate;
        set
        {
            if (_shownDate == value)
                return;

            _shownDate = value.Date;
            OnPropertyChanged();
        }
    }

    public DateTime SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (_selectedDate == value)
                return;

            _selectedDate = value.Date;
            OnPropertyChanged();
            UpdateSelectedDateItems();
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _ = _titleView.RefreshAsync();
        SubscribeWalletChanges();

        if (!_loaded)
        {
            await LoadCalendarAsync();
        }
    }

    protected override void OnDisappearing()
    {
        UnsubscribeWalletChanges();
        base.OnDisappearing();
    }

    private async void OnRefresh(object? sender, EventArgs e)
        => await LoadCalendarAsync(forceRefresh: true);

    private async void OnRetryClicked(object? sender, EventArgs e)
        => await LoadCalendarAsync(forceRefresh: true);

    private async void OnSelectedEventClicked(object? sender, EventArgs e)
    {
        if (sender is not BindableObject { BindingContext: CalendarEventItem item })
            return;

        await Shell.Current.GoToAsync($"{nameof(InvestmentDetailsPage)}?investmentId={item.InvestmentId}");
    }

    private void OnTodayClicked(object? sender, EventArgs e)
    {
        var today = DateTime.Today;
        ShownDate = today;
        SelectedDate = today;
    }

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
        => MainThread.BeginInvokeOnMainThread(async () => await LoadCalendarAsync(forceRefresh: true));

    private async Task LoadCalendarAsync(bool forceRefresh = false)
    {
        SetLoading(true);
        HideError();

        try
        {
            IReadOnlyList<InvestmentDto> source;

            if (forceRefresh)
            {
                source = await _investments.GetInvestmentsAsync(forceRefresh: true);
            }
            else
            {
                source = await _investments.GetCachedInvestmentsAsync();

                if (source.Count == 0 && !_connectivity.IsOffline)
                {
                    source = await _investments.GetInvestmentsAsync(forceRefresh: true);
                }
                else if (!_connectivity.IsOffline && _investments.ShouldRefreshInBackground(source))
                {
                    _ = RefreshInBackgroundAsync();
                }
            }

            BuildEvents(source);
            CalendarEvents = BuildPluginEvents(_events);
            EmptyLabel.IsVisible = _events.Count == 0;
            UpdateSelectedDateItems();
            _loaded = true;
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel carregar o calendario.");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async Task RefreshInBackgroundAsync()
    {
        try
        {
            await _investments.RefreshInvestmentsCacheInBackgroundAsync();
            var cached = await _investments.GetCachedInvestmentsAsync();
            BuildEvents(cached);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                CalendarEvents = BuildPluginEvents(_events);
                EmptyLabel.IsVisible = _events.Count == 0;
                UpdateSelectedDateItems();
            });
        }
        catch
        {
            // Keep cached calendar visible if refresh fails.
        }
    }

    private void BuildEvents(IReadOnlyList<InvestmentDto> investments)
    {
        _events.Clear();
        _events.AddRange(CalendarService.BuildEvents(investments));
    }

    private static EventCollection BuildPluginEvents(IEnumerable<CalendarEventItem> events)
    {
        var collection = new EventCollection();

        foreach (var group in events
                     .GroupBy(item => item.Date.Date)
                     .OrderBy(group => group.Key))
        {
            collection.Add(group.Key, group.OrderBy(item => item.Type).ThenBy(item => item.Title).ToList());
        }

        return collection;
    }

    private Task OnDayTappedAsync(object? parameter)
    {
        var date = TryGetTappedDate(parameter);
        if (!date.HasValue)
            return Task.CompletedTask;

        SelectedDate = date.Value.Date;
        ShownDate = date.Value.Date;
        return Task.CompletedTask;
    }

    private static DateTime? TryGetTappedDate(object? parameter)
    {
        var type = parameter?.GetType();
        if (type is null)
            return null;

        if (type.FullName == "Plugin.Maui.Calendar.Models.DayModel")
        {
            var dateProperty = type.GetProperty("Date");
            if (dateProperty?.GetValue(parameter) is DateTime date)
                return date.Date;
        }

        return parameter switch
        {
            DateTime date => date.Date,
            DateTimeOffset offset => offset.Date,
            _ => null
        };
    }

    private void SetLoading(bool loading)
    {
        LoadingIndicator.IsRunning = loading;
        LoadingIndicator.IsVisible = loading;
        Refresh.IsRefreshing = loading;
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

    private void UpdateSelectedDateItems()
    {
        var selectedDate = SelectedDate.Date;
        var items = _events
            .Where(item => item.Date.Date == selectedDate)
            .OrderBy(item => item.Type)
            .ThenBy(item => item.Title)
            .ToList();

        SelectedDateHeadingLabel.Text = selectedDate.ToString("dd/MM/yyyy", Brazil);
        SelectedDateDescriptionLabel.Text = items.Count == 1
            ? "1 investimento relacionado nesta data."
            : $"{items.Count} itens relacionados nesta data.";
        SelectedDateCollection.ItemsSource = items;
        SelectedDateEmptyLabel.IsVisible = items.Count == 0;
        SelectedDateSection.IsVisible = true;
    }
}
