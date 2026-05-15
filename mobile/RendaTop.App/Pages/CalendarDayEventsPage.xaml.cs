using RendaTop.App.Controls;
using RendaTop.App.Models;
using RendaTop.App.Services;

namespace RendaTop.App.Pages;

public partial class CalendarDayEventsPage : ContentPage, IQueryAttributable
{
    private readonly CalendarService _calendar;
    private readonly NotificationTitleView _titleView;
    private DateTime? _date;

    public CalendarDayEventsPage(CalendarService calendar, NotificationService notifications)
    {
        _calendar = calendar;
        InitializeComponent();
        _titleView = NotificationChrome.Apply(this, "Eventos do dia", notifications);
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _date = null;
        if (query.TryGetValue("date", out var rawDate)
            && DateTime.TryParse(rawDate?.ToString(), out var parsed))
        {
            _date = parsed.Date;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _ = _titleView.RefreshAsync();
        await LoadAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        MainThread.BeginInvokeOnMainThread(async () => await NavigateBackAsync());
        return true;
    }

    private async void OnRetryClicked(object? sender, EventArgs e)
        => await LoadAsync();

    private async void OnBackClicked(object? sender, EventArgs e)
        => await NavigateBackAsync();

    private async void OnViewDetailsClicked(object? sender, EventArgs e)
    {
        if (_date is null || sender is not BindableObject { BindingContext: CalendarEventItem item })
            return;

        await Shell.Current.GoToAsync(
            $"{nameof(CalendarEventDetailsPage)}?investmentId={item.InvestmentId}&eventType={item.Type}&date={_date:yyyy-MM-dd}");
    }

    private async Task LoadAsync()
    {
        if (_date is null)
        {
            ShowError("Data nao informada.");
            return;
        }

        SetLoading(true);
        HideError();

        try
        {
            HeadingLabel.Text = _date.Value.ToString("dd/MM/yyyy");
            DescriptionLabel.Text = "Eventos encontrados para este dia.";
            var items = await _calendar.GetEventsForDateAsync(_date.Value);
            EventsCollection.ItemsSource = items;
            EmptyLabel.IsVisible = items.Count == 0;
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel carregar os eventos deste dia.");
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

    private static async Task NavigateBackAsync()
    {
        var shell = Shell.Current;
        if (shell is null)
            return;

        var navigation = shell.Navigation;
        if (navigation?.NavigationStack?.Count > 1)
        {
            await shell.GoToAsync("..");
            return;
        }

        await shell.GoToAsync("//calendar");
    }
}
