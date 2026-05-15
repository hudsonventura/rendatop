using RendaTop.App.Controls;
using RendaTop.App.Models;
using RendaTop.App.Services;

namespace RendaTop.App.Pages;

public partial class MyInvestmentsPage : ContentPage
{
    private readonly InvestmentService _investmentService;
    private readonly ConnectivityService _connectivity;
    private readonly NotificationTitleView _titleView;
    private CancellationTokenSource? _loadCts;

    public MyInvestmentsPage(InvestmentService investmentService, ConnectivityService connectivity, NotificationService notifications)
    {
        _investmentService = investmentService;
        _connectivity = connectivity;
        InitializeComponent();
        _titleView = NotificationChrome.Apply(this, "Meus Investimentos", notifications);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _titleView.RefreshAsync();
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        _ = LoadInitialStateAsync(_loadCts.Token);
    }

    protected override void OnDisappearing()
    {
        _loadCts?.Cancel();
        base.OnDisappearing();
    }

    private async void OnRefresh(object? sender, EventArgs e)
        => await RefreshFromBackendAsync(showLoading: false);

    private async void OnRetryClicked(object? sender, EventArgs e)
        => await RefreshFromBackendAsync(showLoading: true);

    private async void OnCreateClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(AddInvestmentPage));

    private async Task LoadInitialStateAsync(CancellationToken cancellationToken)
    {
        HideError();

        try
        {
            OfflineBorder.IsVisible = _connectivity.IsOffline;
            CreateButton.IsEnabled = !_connectivity.IsOffline;
            var cached = await _investmentService.GetCachedInvestmentsAsync(cancellationToken);
            ApplyInvestments(cached);

            if (cached.Count == 0 && !_connectivity.IsOffline)
            {
                await RefreshFromBackendAsync(showLoading: true, cancellationToken);
                return;
            }

            if (!_connectivity.IsOffline && _investmentService.ShouldRefreshInBackground(cached))
                _ = RefreshInBackgroundAsync(cancellationToken);
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel carregar seus investimentos.");
        }
    }

    private async Task RefreshFromBackendAsync(bool showLoading, CancellationToken cancellationToken = default)
    {
        if (showLoading)
            SetLoading(true);

        HideError();

        try
        {
            if (_connectivity.IsOffline)
            {
                ApplyInvestments(await _investmentService.GetCachedInvestmentsAsync(cancellationToken));
                return;
            }

            await _investmentService.RefreshInvestmentsCacheAsync(cancellationToken);
            var fresh = await _investmentService.GetCachedInvestmentsAsync(cancellationToken);
            ApplyInvestments(fresh);
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel carregar seus investimentos.");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async Task RefreshInBackgroundAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _investmentService.RefreshInvestmentsCacheInBackgroundAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                return;

            var fresh = await _investmentService.GetCachedInvestmentsAsync(cancellationToken);
            await MainThread.InvokeOnMainThreadAsync(() => ApplyInvestments(fresh));
        }
        catch
        {
            // Keep the cached list visible if background refresh fails.
        }
    }

    private async void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not InvestmentRow row)
            return;

        InvestmentsCollection.SelectedItem = null;
        await Shell.Current.GoToAsync($"{nameof(InvestmentDetailsPage)}?investmentId={row.Id}");
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

    private void ApplyInvestments(IReadOnlyList<InvestmentDto> source)
    {
        var investments = source
            .Where(item => !item.Archived)
            .OrderBy(item => item.DueDate ?? DateTime.MaxValue)
            .ToList();

        InvestmentsCollection.ItemsSource = investments.Select(InvestmentRow.FromDto).ToList();

        InvestmentsCollection.SelectedItem = null;
        EmptyLabel.IsVisible = investments.Count == 0;
        CountLabel.Text = investments.Count.ToString();
        TotalLabel.Text = MoneyFormatter.Currency(investments.Sum(item => item.CurrentValueForDisplay));
        SubtitleLabel.Text = investments.Count == 1 ? "1 investimento ativo" : $"{investments.Count} investimentos ativos";
    }

    private sealed record InvestmentRow(
        Guid Id,
        string Title,
        string BankName,
        string CurrentValue,
        string IndexLabel,
        string DueDateLabel)
    {
        public static InvestmentRow FromDto(InvestmentDto investment)
        {
            var dueDate = investment.DueDate?.ToString("dd/MM/yyyy") ?? "Liquidez diaria";
            var index = investment.Calculated?.FirstOrDefault()?.ProfitLiq is not null
                ? investment.CurrentValueForDisplay >= investment.PrincipalForDisplay
                    ? "Rendimento atualizado"
                    : "Valor atualizado"
                : "Investimento";

            return new InvestmentRow(
                investment.Id,
                investment.Title,
                investment.Bank?.Name ?? "Banco",
                MoneyFormatter.Currency(investment.CurrentValueForDisplay),
                index,
                dueDate);
        }
    }
}
