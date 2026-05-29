using Microcharts;
using RendaTop.App.Controls;
using RendaTop.App.Models;
using RendaTop.App.Services;
using SkiaSharp;

namespace RendaTop.App.Pages;

public partial class DashboardPage : ContentPage
{
    private readonly InvestmentService _investments;
    private readonly NotificationService _notifications;
    private readonly SessionService _session;
    private readonly WalletService _wallets;
    private readonly NotificationTitleView _titleView;
    private bool _loaded;
    private bool _walletSubscribed;

    public DashboardPage(InvestmentService investments, SessionService session, NotificationService notifications, WalletService wallets)
    {
        _investments = investments;
        _session = session;
        _notifications = notifications;
        _wallets = wallets;
        InitializeComponent();
        _titleView = NotificationChrome.Apply(this, "Dashboard", notifications);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        WelcomeLabel.Text = $"Ola, {GetFirstName()}";
        _ = _titleView.RefreshAsync();
        SubscribeWalletChanges();

        if (!_loaded)
            await LoadDashboardAsync();
    }

    protected override void OnDisappearing()
    {
        UnsubscribeWalletChanges();
        base.OnDisappearing();
    }

    private async void OnRefresh(object? sender, EventArgs e)
        => await LoadDashboardAsync(forceRefresh: true);

    private async void OnRetryClicked(object? sender, EventArgs e)
        => await LoadDashboardAsync(forceRefresh: true);

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
        => MainThread.BeginInvokeOnMainThread(async () => await LoadDashboardAsync(forceRefresh: true));

    private async Task LoadDashboardAsync(bool forceRefresh = false)
    {
        SetLoading(true);
        HideError();

        try
        {
            if (forceRefresh)
            {
                ApplySummary(await _investments.GetDashboardSummaryAsync(forceRefresh: true));
            }
            else
            {
                var cachedInvestments = await _investments.GetCachedInvestmentsAsync();
                var cachedSummary = await _investments.GetCachedDashboardSummaryAsync();
                ApplySummary(cachedSummary);

                if (cachedInvestments.Count == 0)
                {
                    ApplySummary(await _investments.GetDashboardSummaryAsync(forceRefresh: true));
                }
                else if (_investments.ShouldRefreshInBackground(cachedInvestments))
                {
                    _ = RefreshDashboardInBackgroundAsync();
                }
            }

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

    private async Task RefreshDashboardInBackgroundAsync()
    {
        try
        {
            await _investments.RefreshInvestmentsCacheInBackgroundAsync();
            var summary = await _investments.GetCachedDashboardSummaryAsync();
            await MainThread.InvokeOnMainThreadAsync(() => ApplySummary(summary));
        }
        catch
        {
            // Keep the cached dashboard visible if background refresh fails.
        }
    }

    private void ApplySummary(DashboardSummary summary)
    {
        InvestedLabel.Text = summary.Invested;
        CurrentLabel.Text = summary.Current;
        ProfitLabel.Text = summary.Profit;
        DueCountLabel.Text = summary.DueSoonCount;

        BankChart.Chart = BuildBankPieChart(summary.BankAllocation);
        BankCollection.ItemsSource = summary.BankAllocation;
        DueCollection.ItemsSource = summary.DueSoon;
        BankEmptyLabel.IsVisible = summary.BankAllocation.Count == 0;
        DueEmptyLabel.IsVisible = summary.DueSoon.Count == 0;
        BankChart.IsVisible = summary.BankAllocation.Count > 0;
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

    private static Chart? BuildBankPieChart(IReadOnlyList<BankAllocationItem> allocation)
    {
        if (allocation.Count == 0)
            return null;

        var entries = allocation.Select(item => new ChartEntry((float)Math.Max(item.Percent, 0d))
        {
            Label = item.BankName,
            ValueLabel = item.PercentText,
            Color = ToSkColor(item.DisplayColor),
            ValueLabelColor = SKColor.Parse("#111827"),
            TextColor = SKColor.Parse("#475569")
        }).ToList();

        return new PieChart
        {
            Entries = entries,
            LabelTextSize = 0,
            BackgroundColor = SKColors.Transparent,
            IsAnimated = false,
            LabelMode = LabelMode.None,
            GraphPosition = GraphPosition.Center
        };
    }

    private static SKColor ToSkColor(Color color)
    {
        var red = (byte)Math.Round(color.Red * 255);
        var green = (byte)Math.Round(color.Green * 255);
        var blue = (byte)Math.Round(color.Blue * 255);
        var alpha = (byte)Math.Round(color.Alpha * 255);
        return new SKColor(red, green, blue, alpha);
    }
}
