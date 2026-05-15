using RendaTop.App.Models;
using RendaTop.App.Services;

namespace RendaTop.App.Pages;

public partial class CalendarEventDetailsPage : ContentPage, IQueryAttributable
{
    private readonly CalendarService _calendar;
    private Guid? _investmentId;
    private DateTime? _date;
    private CalendarEventType? _eventType;

    public CalendarEventDetailsPage(CalendarService calendar)
    {
        _calendar = calendar;
        InitializeComponent();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _investmentId = null;
        _date = null;
        _eventType = null;

        if (query.TryGetValue("investmentId", out var rawInvestmentId)
            && Guid.TryParse(rawInvestmentId?.ToString(), out var investmentId))
        {
            _investmentId = investmentId;
        }

        if (query.TryGetValue("date", out var rawDate)
            && DateTime.TryParse(rawDate?.ToString(), out var date))
        {
            _date = date.Date;
        }

        if (query.TryGetValue("eventType", out var rawEventType)
            && Enum.TryParse<CalendarEventType>(rawEventType?.ToString(), out var eventType))
        {
            _eventType = eventType;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
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

    private async Task LoadAsync()
    {
        if (!_investmentId.HasValue || !_date.HasValue || !_eventType.HasValue)
        {
            ShowError("Evento nao informado.");
            return;
        }

        SetLoading(true);
        HideError();

        try
        {
            var item = await _calendar.GetEventAsync(_investmentId.Value, _eventType.Value, _date.Value);
            BindEvent(item);
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel carregar os detalhes do evento.");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private void BindEvent(CalendarEventItem item)
    {
        TitleLabel.Text = item.Title;
        TypeLabel.Text = item.TypeLabel;
        TypeBadge.BackgroundColor = item.TypeColor;
        DateLabel.Text = item.DateLabel;
        BankLabel.Text = $"Banco: {item.BankName}";
        InvestedValueLabel.Text = item.InvestedValueLabel;
        IndexLabel.Text = item.IndexLabel;
        CurrentValueLabel.Text = item.CurrentNetValueLabel;
        ProfitLabel.Text = item.CurrentProfitLabel;
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
