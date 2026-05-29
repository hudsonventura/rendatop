using System.Globalization;
using Microsoft.Maui.Controls.Shapes;
using RendaTop.App.Controls;
using RendaTop.App.Models;
using RendaTop.App.Services;

namespace RendaTop.App.Pages;

public partial class CalendarPage : ContentPage
{
    private static readonly CultureInfo Brazil = new("pt-BR");
    private static readonly string[] WeekDays = ["Dom", "Seg", "Ter", "Qua", "Qui", "Sex", "Sab"];

    private readonly InvestmentService _investments;
    private readonly CalendarService _calendar;
    private readonly ConnectivityService _connectivity;
    private readonly WalletService _wallets;
    private readonly NotificationTitleView _titleView;
    private readonly List<CalendarEventItem> _events = [];

    private DateTime _currentMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime? _selectedDate;
    private bool _loaded;
    private bool _walletSubscribed;

    public CalendarPage(InvestmentService investments, CalendarService calendar, ConnectivityService connectivity, NotificationService notifications, WalletService wallets)
    {
        _investments = investments;
        _calendar = calendar;
        _connectivity = connectivity;
        _wallets = wallets;
        InitializeComponent();
        _titleView = NotificationChrome.Apply(this, "Calendario", notifications);
        BuildWeekDaysHeader();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _ = _titleView.RefreshAsync();
        SubscribeWalletChanges();

        if (!_loaded)
        {
            await LoadCalendarAsync();
            return;
        }

        RenderCalendar();
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

    private void OnPreviousMonthClicked(object? sender, EventArgs e)
    {
        _currentMonth = _currentMonth.AddMonths(-1);
        RenderCalendar();
    }

    private void OnNextMonthClicked(object? sender, EventArgs e)
    {
        _currentMonth = _currentMonth.AddMonths(1);
        RenderCalendar();
    }

    private void OnPreviousYearClicked(object? sender, EventArgs e)
    {
        _currentMonth = _currentMonth.AddYears(-1);
        RenderCalendar();
    }

    private void OnNextYearClicked(object? sender, EventArgs e)
    {
        _currentMonth = _currentMonth.AddYears(1);
        RenderCalendar();
    }

    private void OnTodayClicked(object? sender, EventArgs e)
    {
        _currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        _selectedDate = DateTime.Today;
        RenderCalendar();
    }

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
            _loaded = true;
            EmptyLabel.IsVisible = _events.Count == 0;
            RenderCalendar();
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
            await MainThread.InvokeOnMainThreadAsync(RenderCalendar);
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

    private void RenderCalendar()
    {
        MonthLabel.Text = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(_currentMonth.ToString("MMMM yyyy", Brazil));
        CalendarGrid.Children.Clear();
        CalendarGrid.RowDefinitions.Clear();
        CalendarGrid.ColumnDefinitions.Clear();

        for (var column = 0; column < 7; column++)
            CalendarGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        var monthStart = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var calendarStart = monthStart.AddDays(-(int)monthStart.DayOfWeek);
        var calendarEnd = monthEnd.AddDays(6 - (int)monthEnd.DayOfWeek);
        var days = Enumerable.Range(0, (calendarEnd - calendarStart).Days + 1)
            .Select(offset => calendarStart.AddDays(offset))
            .ToList();

        var rows = (int)Math.Ceiling(days.Count / 7d);
        for (var row = 0; row < rows; row++)
            CalendarGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        for (var index = 0; index < days.Count; index++)
        {
            var day = days[index];
            var row = index / 7;
            var column = index % 7;
            var cell = BuildDayCell(day);
            cell.SetValue(Grid.RowProperty, row);
            cell.SetValue(Grid.ColumnProperty, column);
            CalendarGrid.Children.Add(cell);
        }
    }

    private View BuildDayCell(DateTime day)
    {
        var dayEvents = _events
            .Where(item => item.Date == day.Date)
            .OrderBy(item => item.Type)
            .ThenBy(item => item.Title)
            .ToList();

        var isCurrentMonth = day.Month == _currentMonth.Month && day.Year == _currentMonth.Year;
        var isToday = day.Date == DateTime.Today;
        var isSelected = _selectedDate.HasValue && _selectedDate.Value.Date == day.Date;

        var header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        header.Children.Add(new Label
        {
            Text = day.Day.ToString(),
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = isToday ? Colors.White : isCurrentMonth ? Color.FromArgb("#111827") : Color.FromArgb("#94A3B8"),
            BackgroundColor = isToday ? Color.FromArgb("#111827") : Colors.Transparent,
            Padding = isToday ? new Thickness(6, 2) : new Thickness(0),
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            WidthRequest = isToday ? 24 : -1,
            HeightRequest = isToday ? 24 : -1
        });

        if (dayEvents.Count > 2)
        {
            var extra = new Label
            {
                Text = $"+{dayEvents.Count - 2}",
                FontSize = 11,
                TextColor = Color.FromArgb("#64748B"),
                HorizontalTextAlignment = TextAlignment.End,
                VerticalTextAlignment = TextAlignment.Center
            };
            extra.SetValue(Grid.ColumnProperty, 1);
            header.Children.Add(extra);
        }

        var stack = new VerticalStackLayout
        {
            Spacing = 6,
            Children = { header }
        };

        foreach (var item in dayEvents.Take(2))
            stack.Children.Add(BuildEventChip(item));

        var border = new Border
        {
            Stroke = isSelected ? Color.FromArgb("#0F172A") : Color.FromArgb("#E2E8F0"),
            StrokeThickness = isSelected ? 2 : 1,
            BackgroundColor = isCurrentMonth
                ? isToday ? Color.FromArgb("#F1F5F9") : Colors.White
                : Color.FromArgb("#F8FAFC"),
            Padding = new Thickness(8),
            MinimumHeightRequest = 110,
            Content = stack,
            StrokeShape = new RoundRectangle
            {
                CornerRadius = 12
            }
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) =>
        {
            _selectedDate = day.Date;
            RenderCalendar();

            if (dayEvents.Count > 0)
                await Shell.Current.GoToAsync($"{nameof(CalendarDayEventsPage)}?date={day:yyyy-MM-dd}");
        };
        border.GestureRecognizers.Add(tap);

        return border;
    }

    private View BuildEventChip(CalendarEventItem item)
    {
        var label = new Label
        {
            Text = item.Title,
            FontSize = 11,
            TextColor = Colors.White,
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 1
        };

        var border = new Border
        {
            BackgroundColor = item.Type == CalendarEventType.Start
                ? Color.FromArgb("#16A34A")
                : Color.FromArgb("#2563EB"),
            StrokeThickness = 0,
            Padding = new Thickness(8, 6),
            Content = label,
            StrokeShape = new RoundRectangle
            {
                CornerRadius = 8
            }
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) => await Shell.Current.GoToAsync(
            $"{nameof(CalendarEventDetailsPage)}?investmentId={item.InvestmentId}&eventType={item.Type}&date={item.Date:yyyy-MM-dd}");
        border.GestureRecognizers.Add(tap);

        return border;
    }

    private void BuildWeekDaysHeader()
    {
        WeekDaysGrid.Children.Clear();

        for (var index = 0; index < WeekDays.Length; index++)
        {
            var label = new Label
            {
                Text = WeekDays[index],
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#64748B"),
                HorizontalTextAlignment = TextAlignment.Center
            };

            label.SetValue(Grid.ColumnProperty, index);
            WeekDaysGrid.Children.Add(label);
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

}
