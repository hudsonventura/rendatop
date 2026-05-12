using RendaTop.App.Models;
using RendaTop.App.Services;

namespace RendaTop.App.Pages;

public partial class DashboardPage : ContentPage
{
    private readonly InvestmentService _investments;
    private readonly SessionService _session;
    private bool _loaded;

    public DashboardPage(InvestmentService investments, SessionService session)
    {
        _investments = investments;
        _session = session;
        InitializeComponent();
        PageFab.AddCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(AddInvestmentPage)));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        WelcomeLabel.Text = $"Ola, {GetFirstName()}";

        if (!_loaded)
            await LoadDashboardAsync();
    }

    private async void OnRefresh(object? sender, EventArgs e)
        => await LoadDashboardAsync();

    private async void OnRetryClicked(object? sender, EventArgs e)
        => await LoadDashboardAsync();

    private async Task LoadDashboardAsync()
    {
        SetLoading(true);
        HideError();

        try
        {
            var summary = await _investments.GetDashboardSummaryAsync();
            ApplySummary(summary);
            _loaded = true;
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel carregar o dashboard. Verifique sua conexao e tente novamente.");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private void ApplySummary(DashboardSummary summary)
    {
        InvestedLabel.Text = summary.Invested;
        CurrentLabel.Text = summary.Current;
        ProfitLabel.Text = summary.Profit;
        DueCountLabel.Text = summary.DueSoonCount;

        BankCollection.ItemsSource = summary.BankAllocation;
        DueCollection.ItemsSource = summary.DueSoon;
        BankEmptyLabel.IsVisible = summary.BankAllocation.Count == 0;
        DueEmptyLabel.IsVisible = summary.DueSoon.Count == 0;
    }

    private string GetFirstName()
    {
        var name = _session.Name;
        if (string.IsNullOrWhiteSpace(name))
            return "Usuario";

        return name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? name;
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
