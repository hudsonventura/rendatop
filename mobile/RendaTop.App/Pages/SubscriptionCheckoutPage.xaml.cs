using System.Globalization;
using System.Net;
using System.Text;
using RendaTop.App.Models;
using RendaTop.App.Services;

namespace RendaTop.App.Pages;

public partial class SubscriptionCheckoutPage : ContentPage, IQueryAttributable
{
    private const string PaymentProcessingMessage = "O processamento do pagamento pode demorar um pouco. Aguarde enquanto confirmamos a cobranca.";
    private const string PaymentTimeoutMessage = "O processamento esta demorando um pouco mais do que o previsto, mas, assim que confirmado, o sistema liberara o funcionamento do plano.";

    private readonly SubscriptionService _subscriptions;
    private readonly UserSettingsService _settingsService;
    private readonly AppConfig _config;
    private PlanDto? _plan;
    private UserSettingsDto? _settings;
    private string? _planId;
    private string _selectedMethod = "card";
    private string? _currentPendingPaymentId;
    private CancellationTokenSource? _pollCts;

    public SubscriptionCheckoutPage(SubscriptionService subscriptions, UserSettingsService settingsService, AppConfig config)
    {
        _subscriptions = subscriptions;
        _settingsService = settingsService;
        _config = config;
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
        await LoadAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        MainThread.BeginInvokeOnMainThread(async () => await NavigateBackAsync());
        return true;
    }

    protected override void OnDisappearing()
    {
        _pollCts?.Cancel();
        base.OnDisappearing();
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
            _settings = await _settingsService.GetAsync();

            if (_plan is null)
            {
                ShowError("Plano nao encontrado.");
                return;
            }

            HeadingLabel.Text = $"Assinar {_plan.Name}";
            DescriptionLabel.Text = $"R${_plan.Price.ToString("N2").Replace('.', ',')} /mes";
            UpdateBillingNotice();
            ApplyCpfToForms();
            UpdateMethodUi();
            LoadCardHtml();
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

    private void OnCardTabClicked(object? sender, EventArgs e)
    {
        _selectedMethod = "card";
        UpdateMethodUi();
    }

    private void OnPixTabClicked(object? sender, EventArgs e)
    {
        _selectedMethod = "pix";
        UpdateMethodUi();
    }

    private void OnBoletoTabClicked(object? sender, EventArgs e)
    {
        _selectedMethod = "boleto";
        UpdateMethodUi();
    }

    private async void OnGeneratePixClicked(object? sender, EventArgs e)
    {
        var cpf = SanitizeCpf(PixCpfEntry.Text);
        if (!IsValidCpf(cpf))
        {
            ShowError("CPF invalido. Verifique os 11 digitos antes de continuar.");
            return;
        }

        if (_plan is null || _settings is null)
            return;

        try
        {
            SetLoading(true);
            HideMessages();
            ShowNotice(PaymentProcessingMessage);
            var result = await _subscriptions.SubscribeWithPixAsync(BuildPixRequest(cpf));
            BindPixResult(result);

            if (string.Equals(result.Status, "approved", StringComparison.OrdinalIgnoreCase))
            {
                ShowNotice("Pagamento aprovado!");
            }
            else
            {
                ShowNotice("QR Code PIX gerado. Aguardando pagamento...");
                StartPaymentPolling(result.PaymentId, TimeSpan.FromSeconds(5));
            }
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Erro ao gerar PIX.");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async void OnGenerateBoletoClicked(object? sender, EventArgs e)
    {
        var cpf = SanitizeCpf(BoletoCpfEntry.Text);
        if (!IsValidCpf(cpf))
        {
            ShowError("CPF invalido. Verifique os 11 digitos antes de continuar.");
            return;
        }

        if (_plan is null || _settings is null)
            return;

        try
        {
            SetLoading(true);
            HideMessages();
            ShowNotice(PaymentProcessingMessage);
            var result = await _subscriptions.SubscribeWithBoletoAsync(BuildBoletoRequest(cpf));
            BindBoletoResult(result);
            ShowNotice("Boleto gerado. Aguardando pagamento...");
            StartPaymentPolling(result.PaymentId, TimeSpan.FromSeconds(10));
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Erro ao gerar boleto.");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async void OnCopyPixClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(PixCodeEditor.Text))
            return;

        await Clipboard.Default.SetTextAsync(PixCodeEditor.Text);
        ShowNotice("PIX copia e cola copiado.");
    }

    private async void OnCopyBoletoLineClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(BoletoLineEditor.Text))
            return;

        await Clipboard.Default.SetTextAsync(BoletoLineEditor.Text);
        ShowNotice("Linha digitavel copiada.");
    }

    private async void OnOpenBoletoClicked(object? sender, EventArgs e)
    {
        if (OpenBoletoButton.BindingContext is string url && !string.IsNullOrWhiteSpace(url))
            await Launcher.Default.OpenAsync(url);
    }

    private void OnCpfChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not Entry entry)
            return;

        var digits = SanitizeCpf(e.NewTextValue);
        if (entry.Text != digits)
            entry.Text = digits;
    }

    private async void OnCardWebViewNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (!e.Url.StartsWith("rendatop://", StringComparison.OrdinalIgnoreCase))
            return;

        e.Cancel = true;

        var uri = new Uri(e.Url);
        if (!string.Equals(uri.Host, "card-token", StringComparison.OrdinalIgnoreCase))
            return;

        var parameters = ParseQuery(uri.Query);
        var status = parameters.GetValueOrDefault("status");

        if (string.Equals(status, "error", StringComparison.OrdinalIgnoreCase))
        {
            ShowError(WebUtility.UrlDecode(parameters.GetValueOrDefault("message")) ?? "Erro ao processar cartao.");
            return;
        }

        var token = WebUtility.UrlDecode(parameters.GetValueOrDefault("token")) ?? string.Empty;
        var paymentMethodId = WebUtility.UrlDecode(parameters.GetValueOrDefault("payment_method_id")) ?? string.Empty;
        var issuerId = WebUtility.UrlDecode(parameters.GetValueOrDefault("issuer_id")) ?? string.Empty;
        var cardType = WebUtility.UrlDecode(parameters.GetValueOrDefault("card_type")) ?? "credit_card";
        var cpf = WebUtility.UrlDecode(parameters.GetValueOrDefault("cpf")) ?? string.Empty;

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(paymentMethodId) || !IsValidCpf(cpf) || _plan is null)
        {
            ShowError("Dados do cartao invalidos. Confira as informacoes e tente novamente.");
            return;
        }

        try
        {
            SetLoading(true);
            ShowNotice(PaymentProcessingMessage);
            var result = await _subscriptions.SubscribeWithCardAsync(
                new CardSubscriptionRequestDto(_plan.Id, token, paymentMethodId, cardType, issuerId, 1, cpf));

            if (string.Equals(result.Status, "approved", StringComparison.OrdinalIgnoreCase))
            {
                ShowNotice("Assinatura ativada com sucesso!");
                await Task.Delay(800);
                await NavigateBackToSubscriptionAsync();
            }
            else
            {
                ShowError($"Pagamento {result.Status}: {result.StatusDetail}");
            }
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Erro ao processar pagamento.");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private void LoadCardHtml()
    {
        if (_plan is null)
            return;

        if (string.IsNullOrWhiteSpace(AppConfig.MercadoPagoPublicKey))
        {
            CardConfigLabel.Text = "Preencha AppConfig.MercadoPagoPublicKey para habilitar o checkout de cartao no app.";
            CardWebView.Source = new HtmlWebViewSource { Html = "<html><body style='font-family:sans-serif;padding:16px;color:#111827'>Checkout de cartao indisponivel ate configurar a chave publica do Mercado Pago no app.</body></html>" };
            return;
        }

        CardConfigLabel.Text = "Pagamento com tokenizacao local do cartao via Mercado Pago.";
        var cpf = WebUtility.HtmlEncode(FormatCpf(SanitizeCpf(_settings?.Cpf)));
        var amount = _plan.Price.ToString("0.00", CultureInfo.InvariantCulture);
        var html = LoadCardTemplate()
            .Replace("__MP_PUBLIC_KEY__", AppConfig.MercadoPagoPublicKey)
            .Replace("__DEFAULT_CPF__", cpf)
            .Replace("__AMOUNT__", amount);

        CardWebView.Source = new HtmlWebViewSource { Html = html };
    }

    private static string LoadCardTemplate()
    {
        using var stream = FileSystem.OpenAppPackageFileAsync("subscription_card_checkout.html").GetAwaiter().GetResult();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private void UpdateMethodUi()
    {
        CardSection.IsVisible = _selectedMethod == "card";
        PixSection.IsVisible = _selectedMethod == "pix";
        BoletoSection.IsVisible = _selectedMethod == "boleto";

        ApplyTabState(CardTabButton, _selectedMethod == "card");
        ApplyTabState(PixTabButton, _selectedMethod == "pix");
        ApplyTabState(BoletoTabButton, _selectedMethod == "boleto");
        UpdateBillingNotice();
    }

    private void ApplyCpfToForms()
    {
        var cpf = SanitizeCpf(_settings?.Cpf);
        PixCpfEntry.Text = cpf;
        BoletoCpfEntry.Text = cpf;
    }

    private void UpdateBillingNotice()
    {
        BillingNoticeLabel.Text = _selectedMethod switch
        {
            "pix" or "boleto" => "Voce esta contratando uma assinatura. Para continuar com a assinatura no proximo ciclo, sera necessario realizar um novo pagamento manualmente.",
            _ => "Voce esta contratando uma assinatura. No proximo ciclo, uma nova cobranca sera feita automaticamente no cartao para manter a assinatura ativa."
        };
    }

    private PixSubscriptionRequestDto BuildPixRequest(string cpf)
    {
        var (firstName, lastName) = SplitFullName(_settings?.Name);
        return new PixSubscriptionRequestDto(_plan!.Id, firstName, lastName, cpf);
    }

    private BoletoSubscriptionRequestDto BuildBoletoRequest(string cpf)
    {
        var (firstName, lastName) = SplitFullName(_settings?.Name);
        return new BoletoSubscriptionRequestDto(_plan!.Id, firstName, lastName, cpf);
    }

    private void BindPixResult(PaymentResultDto result)
    {
        PixResultBorder.IsVisible = true;
        PixCodeEditor.Text = result.PixQrCode ?? string.Empty;
        PixQrImage.IsVisible = !string.IsNullOrWhiteSpace(result.PixQrCodeBase64);
        PixQrImage.Source = string.IsNullOrWhiteSpace(result.PixQrCodeBase64)
            ? null
            : ImageSource.FromStream(() => new MemoryStream(Convert.FromBase64String(result.PixQrCodeBase64)));
    }

    private void BindBoletoResult(PaymentResultDto result)
    {
        BoletoResultBorder.IsVisible = true;
        BoletoLineEditor.Text = result.BoletoDigitableLine ?? string.Empty;
        OpenBoletoButton.IsVisible = !string.IsNullOrWhiteSpace(result.BoletoUrl);
        OpenBoletoButton.BindingContext = result.BoletoUrl;
        BoletoImage.IsVisible = !string.IsNullOrWhiteSpace(result.BoletoBarcodeImageBase64);
        BoletoImage.Source = string.IsNullOrWhiteSpace(result.BoletoBarcodeImageBase64)
            ? null
            : ImageSource.FromStream(() => new MemoryStream(Convert.FromBase64String(result.BoletoBarcodeImageBase64)));
    }

    private void StartPaymentPolling(string paymentId, TimeSpan interval)
    {
        _pollCts?.Cancel();
        _currentPendingPaymentId = paymentId;
        _pollCts = new CancellationTokenSource();
        _ = PollPaymentAsync(paymentId, interval, _pollCts.Token);
    }

    private async Task PollPaymentAsync(string paymentId, TimeSpan interval, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(interval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                var result = await _subscriptions.GetPaymentStatusAsync(paymentId, cancellationToken);
                if (string.Equals(result.Status, "approved", StringComparison.OrdinalIgnoreCase))
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        ShowNotice("Pagamento aprovado!");
                        await Task.Delay(800);
                        await NavigateBackToSubscriptionAsync();
                    });
                    return;
                }
            }
        }
        catch
        {
            await MainThread.InvokeOnMainThreadAsync(() => ShowNotice(PaymentTimeoutMessage));
        }
    }

    private static void ApplyTabState(Button button, bool active)
    {
        button.BackgroundColor = active ? Color.FromArgb("#111827") : Colors.White;
        button.TextColor = active ? Colors.White : Color.FromArgb("#111827");
        button.BorderColor = active ? Color.FromArgb("#111827") : Color.FromArgb("#CBD5E1");
        button.BorderWidth = active ? 0 : 1;
    }

    private static (string FirstName, string LastName) SplitFullName(string? fullName)
    {
        var cleaned = (fullName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(cleaned))
            return ("", "");

        var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
            return (parts[0], parts[0]);

        return (parts[0], string.Join(' ', parts.Skip(1)));
    }

    private static string SanitizeCpf(string? value)
        => new((value ?? string.Empty).Where(char.IsDigit).Take(11).ToArray());

    private static string FormatCpf(string cpf)
    {
        var digits = SanitizeCpf(cpf);
        return digits.Length == 11
            ? $"{digits[..3]}.{digits.Substring(3, 3)}.{digits.Substring(6, 3)}-{digits.Substring(9, 2)}"
            : digits;
    }

    private static bool IsValidCpf(string? cpf)
    {
        var digits = SanitizeCpf(cpf);
        if (digits.Length != 11 || digits.Distinct().Count() == 1)
            return false;

        static int Calc(string source, int factor)
        {
            var sum = 0;
            foreach (var ch in source)
                sum += (ch - '0') * factor--;
            var mod = sum % 11;
            return mod < 2 ? 0 : 11 - mod;
        }

        var d1 = Calc(digits[..9], 10);
        var d2 = Calc(digits[..9] + d1, 11);
        return digits.EndsWith($"{d1}{d2}");
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pieces = part.Split('=', 2);
            result[pieces[0]] = pieces.Length > 1 ? pieces[1] : string.Empty;
        }
        return result;
    }

    private void SetLoading(bool loading)
    {
        LoadingIndicator.IsRunning = loading;
        LoadingIndicator.IsVisible = loading;
        GeneratePixButton.IsEnabled = !loading;
        GenerateBoletoButton.IsEnabled = !loading;
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
        NoticeBorder.IsVisible = false;
        ErrorBorder.IsVisible = false;
        NoticeLabel.Text = string.Empty;
        ErrorLabel.Text = string.Empty;
    }

    private async void OnBackClicked(object? sender, EventArgs e)
        => await NavigateBackAsync();

    private async Task NavigateBackAsync()
    {
        _pollCts?.Cancel();
        await NavigateBackToSubscriptionAsync();
    }

    private static async Task NavigateBackToSubscriptionAsync()
        => await Shell.Current.GoToAsync("//subscription");
}
