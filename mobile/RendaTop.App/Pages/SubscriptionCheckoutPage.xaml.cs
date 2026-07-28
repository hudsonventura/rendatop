using RendaTop.App.Models;
using RendaTop.App.Services;

namespace RendaTop.App.Pages;

public partial class SubscriptionCheckoutPage : ContentPage, IQueryAttributable
{
    private readonly SubscriptionService _subscriptions;
    private readonly UserSettingsService _settingsService;
    private PlanDto? _plan;
    private UserSettingsDto? _settings;
    private string? _planId;
    private bool _checkoutOpened;
    private CheckoutPaymentMethod _paymentMethod = CheckoutPaymentMethod.Card;

    public SubscriptionCheckoutPage(SubscriptionService subscriptions, UserSettingsService settingsService)
    {
        _subscriptions = subscriptions;
        _settingsService = settingsService;
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
            var plansTask = _subscriptions.GetPlansAsync();
            var settingsTask = _settingsService.GetAsync();
            await Task.WhenAll(plansTask, settingsTask);

            var plans = plansTask.Result;
            _settings = settingsTask.Result;
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
            CpfEntry.Text = FormatCpf(_settings.Cpf);
            UpdatePaymentMethodUi();
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

        var cpf = SanitizeCpf(CpfEntry.Text);
        if (!IsValidCpf(cpf))
        {
            ShowError("CPF invalido. Verifique os 11 digitos antes de continuar.");
            return;
        }

        SetLoading(true);
        HideMessages();

        try
        {
            var result = await StartCheckoutAsync(cpf);
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

    private async Task<PaymentResultDto> StartCheckoutAsync(string cpf)
    {
        if (_plan is null)
            throw new InvalidOperationException("Plano nao informado.");

        var (firstName, lastName) = SplitFullName(_settings?.Name);
        return _paymentMethod switch
        {
            CheckoutPaymentMethod.Pix => await _subscriptions.StartPixCheckoutAsync(
                new PixHostedCheckoutRequestDto(_plan.Id, firstName, lastName, cpf)),
            CheckoutPaymentMethod.Boleto => await _subscriptions.StartBoletoCheckoutAsync(
                new BoletoHostedCheckoutRequestDto(_plan.Id, firstName, lastName, cpf)),
            _ => await _subscriptions.StartCardCheckoutAsync(
                new CardHostedCheckoutRequestDto(_plan.Id, cpf))
        };
    }

    private void OnCardClicked(object? sender, EventArgs e)
    {
        _paymentMethod = CheckoutPaymentMethod.Card;
        UpdatePaymentMethodUi();
    }

    private void OnPixClicked(object? sender, EventArgs e)
    {
        _paymentMethod = CheckoutPaymentMethod.Pix;
        UpdatePaymentMethodUi();
    }

    private void OnBoletoClicked(object? sender, EventArgs e)
    {
        _paymentMethod = CheckoutPaymentMethod.Boleto;
        UpdatePaymentMethodUi();
    }

    private void OnCpfChanged(object? sender, TextChangedEventArgs e)
    {
        var formatted = FormatCpf(e.NewTextValue);
        if (CpfEntry.Text != formatted)
            CpfEntry.Text = formatted;
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
        CardButton.IsEnabled = !loading;
        PixButton.IsEnabled = !loading;
        BoletoButton.IsEnabled = !loading;
    }

    private void UpdatePaymentMethodUi()
    {
        ApplyPaymentMethodButton(CardButton, _paymentMethod == CheckoutPaymentMethod.Card);
        ApplyPaymentMethodButton(PixButton, _paymentMethod == CheckoutPaymentMethod.Pix);
        ApplyPaymentMethodButton(BoletoButton, _paymentMethod == CheckoutPaymentMethod.Boleto);
    }

    private static void ApplyPaymentMethodButton(Button button, bool selected)
    {
        button.BackgroundColor = selected ? Color.FromArgb("#111827") : Colors.White;
        button.TextColor = selected ? Colors.White : Color.FromArgb("#111827");
        button.BorderColor = selected ? Color.FromArgb("#111827") : Color.FromArgb("#CBD5E1");
        button.BorderWidth = selected ? 0 : 1;
    }

    private static string SanitizeCpf(string? value)
        => new string((value ?? string.Empty).Where(char.IsDigit).ToArray());

    private static string FormatCpf(string? value)
    {
        var digits = SanitizeCpf(value);
        if (digits.Length <= 3)
            return digits;
        if (digits.Length <= 6)
            return $"{digits[..3]}.{digits[3..]}";
        if (digits.Length <= 9)
            return $"{digits[..3]}.{digits.Substring(3, 3)}.{digits[6..]}";

        return $"{digits[..3]}.{digits.Substring(3, 3)}.{digits.Substring(6, 3)}-{digits.Substring(9, Math.Min(2, digits.Length - 9))}";
    }

    private static bool IsValidCpf(string cpf)
    {
        if (cpf.Length != 11 || cpf.Distinct().Count() == 1)
            return false;

        var firstDigit = CalculateCpfDigit(cpf, 9);
        var secondDigit = CalculateCpfDigit(cpf, 10);
        return cpf[9] - '0' == firstDigit && cpf[10] - '0' == secondDigit;
    }

    private static int CalculateCpfDigit(string cpf, int length)
    {
        var sum = 0;
        for (var index = 0; index < length; index++)
            sum += (cpf[index] - '0') * (length + 1 - index);

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    private static (string FirstName, string LastName) SplitFullName(string? fullName)
    {
        var parts = (fullName ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return parts.Length switch
        {
            0 => ("Cliente", "RendaTop"),
            1 => (parts[0], "RendaTop"),
            _ => (parts[0], string.Join(" ", parts[1..]))
        };
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

    private enum CheckoutPaymentMethod
    {
        Card,
        Pix,
        Boleto
    }
}
