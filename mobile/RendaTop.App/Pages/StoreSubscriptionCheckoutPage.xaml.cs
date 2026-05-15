using RendaTop.App.Models;
using RendaTop.App.Services;

namespace RendaTop.App.Pages;

[QueryProperty(nameof(PlanId), "planId")]
public partial class StoreSubscriptionCheckoutPage : ContentPage
{
    private readonly SubscriptionService _subscriptions;
    private readonly StoreBillingService _storeBilling;
    private PlanDto? _plan;
    private string? _planId;

    public StoreSubscriptionCheckoutPage(SubscriptionService subscriptions, StoreBillingService storeBilling)
    {
        _subscriptions = subscriptions;
        _storeBilling = storeBilling;
        InitializeComponent();
    }

    public string PlanId
    {
        set => _planId = value;
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

    private async Task LoadAsync()
    {
        if (string.IsNullOrWhiteSpace(_planId))
        {
            ShowError("Plano nao informado.");
            return;
        }

        SetLoading(true);
        HideMessages();

        try
        {
            var plans = await _subscriptions.GetPlansAsync();
            _plan = plans.FirstOrDefault(item => string.Equals(item.Id, _planId, StringComparison.OrdinalIgnoreCase));
            if (_plan is null)
            {
                ShowError("Plano nao encontrado.");
                return;
            }

            var checkout = _storeBilling.GetCheckoutInfo(_plan);
            HeadingLabel.Text = checkout.Headline;
            DescriptionLabel.Text = checkout.Description;
            PlatformLabel.Text = checkout.PlatformDisplayName;
            ProviderLabel.Text = checkout.ProviderDisplayName;
            PlanLabel.Text = _plan.Name;
            PriceLabel.Text = _plan.Price > 0 ? $"R${_plan.Price.ToString("N2").Replace('.', ',')} /mes" : "Gratis";
            FlowDescriptionLabel.Text = checkout.IsNativePurchaseEnabled
                ? "A compra sera iniciada pela loja nativa desta plataforma."
                : checkout.BlockingReason ?? "Checkout indisponivel.";
            PurchaseButton.Text = checkout.ActionLabel;
            PurchaseButton.IsEnabled = checkout.IsNativePurchaseEnabled;

            if (!checkout.IsNativePurchaseEnabled && !string.IsNullOrWhiteSpace(checkout.BlockingReason))
                ShowNotice(checkout.BlockingReason);
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel preparar o pagamento.");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async void OnPurchaseClicked(object? sender, EventArgs e)
    {
        if (_plan is null)
            return;

        HideMessages();
        SetLoading(true);

        try
        {
            var result = await _storeBilling.StartPurchaseAsync(_plan);
            if (result.Started)
                ShowNotice(result.Message);
            else
                ShowError(result.Message);
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel iniciar a compra.");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e)
        => await NavigateBackAsync();

    private void SetLoading(bool loading)
    {
        LoadingIndicator.IsRunning = loading;
        LoadingIndicator.IsVisible = loading;
        PurchaseButton.IsEnabled = !loading && PurchaseButton.IsEnabled;
    }

    private void ShowNotice(string message)
    {
        NoticeLabel.Text = message;
        NoticeBorder.IsVisible = true;
        ErrorBorder.IsVisible = false;
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorBorder.IsVisible = true;
        NoticeBorder.IsVisible = false;
    }

    private void HideMessages()
    {
        NoticeLabel.Text = string.Empty;
        NoticeBorder.IsVisible = false;
        ErrorLabel.Text = string.Empty;
        ErrorBorder.IsVisible = false;
    }

    private static async Task NavigateBackAsync()
    {
        var shell = Shell.Current;
        if (shell is null)
            return;

        await shell.GoToAsync("..");
    }
}
