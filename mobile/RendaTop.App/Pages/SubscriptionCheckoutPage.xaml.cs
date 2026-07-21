using RendaTop.App.Models;
using RendaTop.App.Services;

namespace RendaTop.App.Pages;

public partial class SubscriptionCheckoutPage : ContentPage, IQueryAttributable
{
    private readonly SubscriptionService _subscriptions;
    private PlanDto? _plan;
    private string? _planId;
    private bool _checkoutOpened;

    public SubscriptionCheckoutPage(SubscriptionService subscriptions)
    {
        _subscriptions = subscriptions;
        InitializeComponent();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("planId", out var rawPlanId))
            _planId = rawPlanId?.ToString();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_checkoutOpened)
        {
            CheckoutOpenedBorder.IsVisible = true;
            return;
        }

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
            _plan = plans.FirstOrDefault(item =>
                string.Equals(item.Id, _planId, StringComparison.OrdinalIgnoreCase));

            if (_plan is null || _plan.Price <= 0)
            {
                ShowError("Plano pago nao encontrado.");
                return;
            }

            HeadingLabel.Text = $"Assinar {_plan.Name}";
            PlanLabel.Text = _plan.Name;
            PriceLabel.Text = $"R${_plan.Price.ToString("N2").Replace('.', ',')} /mes";
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel carregar o checkout.");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async void OnContinueClicked(object? sender, EventArgs e)
    {
        if (_plan is null)
            return;

        SetLoading(true);
        HideMessages();

        try
        {
            var result = await _subscriptions.StartHostedCheckoutAsync(_plan.Id);
            if (!Uri.TryCreate(result.CheckoutUrl, UriKind.Absolute, out var checkoutUri) ||
                checkoutUri.Scheme != Uri.UriSchemeHttps)
            {
                ShowError("O Mercado Pago nao retornou um link de pagamento valido.");
                return;
            }

            _checkoutOpened = await Launcher.Default.OpenAsync(checkoutUri);
            if (!_checkoutOpened)
            {
                ShowError("Nao foi possivel abrir o Mercado Pago ou o navegador neste aparelho.");
                return;
            }

            CheckoutOpenedBorder.IsVisible = true;
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel iniciar o pagamento no Mercado Pago.");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async void OnPaymentCompletedClicked(object? sender, EventArgs e)
        => await NavigateBackAsync();

    private async void OnBackClicked(object? sender, EventArgs e)
        => await NavigateBackAsync();

    private void SetLoading(bool loading)
    {
        LoadingIndicator.IsRunning = loading;
        LoadingIndicator.IsVisible = loading;
        ContinueButton.IsEnabled = !loading && _plan is not null;
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorBorder.IsVisible = true;
        CheckoutOpenedBorder.IsVisible = false;
    }

    private void HideMessages()
    {
        ErrorLabel.Text = string.Empty;
        ErrorBorder.IsVisible = false;
    }

    private static async Task NavigateBackAsync()
    {
        var shell = Shell.Current;
        if (shell is not null)
            await shell.GoToAsync("..");
    }
}
