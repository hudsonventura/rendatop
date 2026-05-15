using RendaTop.App.Controls;
using RendaTop.App.Models;
using RendaTop.App.Services;

namespace RendaTop.App.Pages;

public partial class SubscriptionPage : ContentPage
{
    private readonly SubscriptionService _subscriptions;
    private readonly UserSettingsService _settingsService;
    private readonly AppPlatformService _platform;
    private readonly StoreBillingService _storeBilling;
    private readonly NotificationTitleView _titleView;
    private IReadOnlyList<PlanDto> _plans = [];
    private SubscriptionOverviewDto? _overview;
    private UserSettingsDto? _settings;
    private CancellationTokenSource? _pollCts;

    public SubscriptionPage(
        SubscriptionService subscriptions,
        UserSettingsService settingsService,
        AppPlatformService platform,
        StoreBillingService storeBilling,
        NotificationService notifications)
    {
        _subscriptions = subscriptions;
        _settingsService = settingsService;
        _platform = platform;
        _storeBilling = storeBilling;
        InitializeComponent();
        _titleView = NotificationChrome.Apply(this, "Assinatura", notifications);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _ = _titleView.RefreshAsync();
        await LoadAsync();
        StartPendingChargePolling();
    }

    protected override void OnDisappearing()
    {
        _pollCts?.Cancel();
        base.OnDisappearing();
    }

    private async Task LoadAsync()
    {
        SetLoading(true);
        HideError();

        try
        {
            var plansTask = _subscriptions.GetPlansAsync();
            var overviewTask = _subscriptions.GetOverviewAsync();
            var settingsTask = _settingsService.GetAsync();

            await Task.WhenAll(plansTask, overviewTask, settingsTask);

            _plans = plansTask.Result;
            _overview = overviewTask.Result;
            _settings = settingsTask.Result;

            BindPlans();
            BindOverview();
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel carregar os dados da assinatura.");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private void BindPlans()
    {
        var activePlanId = _overview?.ActiveSubscription?.PlanId ?? "free";
        var pendingPlanId = _overview?.PendingSubscription?.PlanId;

        PlansCollection.ItemsSource = _plans.Select(plan =>
        {
            var isActive = activePlanId == plan.Id;
            var isPending = pendingPlanId == plan.Id;
            var isPopular = plan.Id == "plus" && !isActive && !isPending;

            var badgeText = isPending ? "Pagamento pendente" : isActive ? "Atual" : isPopular ? "Mais popular" : string.Empty;
            var actionLabel = isActive
                ? plan.Price > 0
                    ? _overview?.ActiveSubscription?.CancelAtPeriodEnd == true ? "Cancelamento agendado" : "Cancelar assinatura"
                    : "Plano atual"
                : isPending
                    ? "Cancelar pendencia"
                    : plan.Price > 0 ? $"Assinar {plan.Name}" : "Plano basico";

            return new PlanRow(
                plan,
                plan.Name,
                plan.Price > 0 ? $"R${plan.Price.ToString("N2").Replace('.', ',')} /mes" : "Gratis",
                plan.Features.Values.ToList(),
                actionLabel,
                isActive,
                isPending,
                isPopular,
                badgeText,
                !string.IsNullOrWhiteSpace(badgeText),
                isActive ? Color.FromArgb("#111827") : isPending ? Color.FromArgb("#F59E0B") : Color.FromArgb("#E2E8F0"),
                isPending ? Color.FromArgb("#FFFBEB") : isActive ? Color.FromArgb("#EFF6FF") : Color.FromArgb("#FFFFFF"),
                isPending ? Color.FromArgb("#92400E") : isActive ? Color.FromArgb("#1D4ED8") : Color.FromArgb("#111827"),
                isActive || isPending || plan.Price <= 0
                    ? Colors.White
                    : Color.FromArgb("#111827"),
                isActive || isPending || plan.Price <= 0
                    ? Color.FromArgb("#CBD5E1")
                    : Color.FromArgb("#111827"),
                isActive || isPending || plan.Price <= 0 ? 1 : 0,
                isActive || isPending || plan.Price <= 0 ? Color.FromArgb("#111827") : Colors.White);
        }).ToList();
    }

    private void BindOverview()
    {
        var active = _overview?.ActiveSubscription;
        ActiveSubscriptionBorder.IsVisible = active is not null && active.PlanId != "free";
        if (active is not null)
        {
            ActivePlanLabel.Text = active.Plan?.Name ?? active.PlanId;
            ActivePaymentMethodLabel.Text = NormalizePaymentMethod(active.PaymentMethod);
            ActiveDueDateLabel.Text = active.CurrentPeriodEnd.ToLocalTime().ToString("dd/MM/yyyy");
            CancelScheduledBorder.IsVisible = active.CancelAtPeriodEnd;
        }

        var pending = _overview?.PendingSubscription;
        PendingSubscriptionBorder.IsVisible = pending is not null && pending.PlanId != "free";
        if (pending is not null)
        {
            PendingPlanLabel.Text = pending.Plan?.Name ?? pending.PlanId;
            PendingPaymentMethodLabel.Text = NormalizePaymentMethod(pending.PaymentMethod);
        }

        BindPendingCharge(_overview?.PendingCharge);
    }

    private void BindPendingCharge(SubscriptionChargeDto? charge)
    {
        PendingChargeBorder.IsVisible = charge is not null;
        if (charge is null)
            return;

        PendingChargeTitleLabel.Text = string.Equals(charge.ChargeKind, "Renewal", StringComparison.OrdinalIgnoreCase)
            ? "Cobranca pendente de renovacao"
            : "Cobranca pendente da assinatura";
        PendingChargePlanLabel.Text = _overview?.PendingSubscription?.Plan?.Name
            ?? _overview?.PendingSubscription?.PlanId
            ?? charge.PlanId;
        PendingChargeStatusLabel.Text = charge.Status;
        PendingChargeAmountLabel.Text = MoneyFormatter.Currency(charge.Amount);
        PendingChargeDueLabel.Text = charge.DueAt?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "—";

        PixSection.IsVisible = !string.IsNullOrWhiteSpace(charge.PixQrCode);
        PixCodeEditor.Text = charge.PixQrCode ?? string.Empty;

        BoletoLineSection.IsVisible = !string.IsNullOrWhiteSpace(charge.BoletoDigitableLine);
        BoletoLineEditor.Text = charge.BoletoDigitableLine ?? string.Empty;
        OpenBoletoButton.IsVisible = !string.IsNullOrWhiteSpace(charge.BoletoUrl);

        var base64 = charge.PixQrCodeBase64 ?? charge.BoletoBarcodeImageBase64;
        PendingChargeImage.IsVisible = !string.IsNullOrWhiteSpace(base64);
        PendingChargeImage.Source = string.IsNullOrWhiteSpace(base64)
            ? null
            : ImageSource.FromStream(() => new MemoryStream(Convert.FromBase64String(base64)));
    }

    private async void OnPlanActionClicked(object? sender, EventArgs e)
    {
        if (sender is not BindableObject { BindingContext: PlanRow row })
            return;

        if (row.IsPending)
        {
            await CancelPendingAsync();
            return;
        }

        if (row.IsActive)
        {
            if (row.Plan.Price > 0 && _overview?.ActiveSubscription?.CancelAtPeriodEnd != true)
                await CancelActiveAsync();
            return;
        }

        if (row.Plan.Price <= 0)
            return;

        if (_platform.UsesNativeStoreBilling)
        {
            var checkout = _storeBilling.GetCheckoutInfo(row.Plan);
            await Shell.Current.GoToAsync(
                $"{nameof(StoreSubscriptionCheckoutPage)}?planId={row.Plan.Id}");

            if (!checkout.IsNativePurchaseEnabled)
                ShowNotice(checkout.BlockingReason ?? "Checkout nativo indisponivel.");

            return;
        }

        await Shell.Current.GoToAsync($"{nameof(SubscriptionCheckoutPage)}?planId={row.Plan.Id}");
    }

    private async void OnCancelPendingClicked(object? sender, EventArgs e)
        => await CancelPendingAsync();

    private async void OnRevertScheduledCancellationClicked(object? sender, EventArgs e)
    {
        var confirmed = await DisplayAlertAsync(
            "Cancelar a solicitacao de cancelamento?",
            "Se voce continuar, a programacao de cancelamento sera revertida e a sua assinatura podera renovar normalmente.",
            "Sim",
            "Nao");

        if (!confirmed)
            return;

        try
        {
            SetLoading(true);
            ShowNotice(await _subscriptions.RevertScheduledCancellationAsync());
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Erro ao reverter o cancelamento agendado.");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async Task CancelPendingAsync()
    {
        var confirmed = await DisplayAlertAsync(
            "Cancelar pendencia?",
            "Se voce continuar, esta pendencia sera cancelada. Mesmo que o boleto e/ou o PIX seja compensado depois disso, o seu plano nao entrara em vigor.",
            "Sim, cancelar",
            "Nao");

        if (!confirmed)
            return;

        try
        {
            SetLoading(true);
            await _subscriptions.CancelPendingSubscriptionAsync();
            ShowNotice("Cobranca pendente cancelada.");
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel cancelar a pendencia.");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async Task CancelActiveAsync()
    {
        var paymentMethod = _overview?.ActiveSubscription?.PaymentMethod ?? string.Empty;
        var supportsImmediateRefund = paymentMethod.Contains("card", StringComparison.OrdinalIgnoreCase)
            || string.Equals(paymentMethod, "pix", StringComparison.OrdinalIgnoreCase);

        string mode;
        if (supportsImmediateRefund)
        {
            var action = await DisplayActionSheetAsync(
                "Escolha como deseja encerrar sua assinatura paga.",
                "Cancelar",
                null,
                "Manter ate o fim",
                "Receber proporcional");

            if (action == "Cancelar")
                return;

            mode = action == "Receber proporcional" ? "refund_prorated" : "end_of_period";
        }
        else
        {
            var confirmed = await DisplayAlertAsync(
                "Cancelar assinatura?",
                "Vamos programar o cancelamento para o final do periodo ja pago e nenhuma cobranca futura sera enviada.",
                "Sim, programar cancelamento",
                "Nao");

            if (!confirmed)
                return;

            mode = "end_of_period";
        }

        try
        {
            SetLoading(true);
            ShowNotice(await _subscriptions.CancelActiveSubscriptionAsync(mode));
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Erro ao cancelar assinatura.");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async void OnRefreshPaymentStatusClicked(object? sender, EventArgs e)
    {
        var paymentId = _overview?.PendingCharge?.ProviderPaymentId;
        if (string.IsNullOrWhiteSpace(paymentId))
        {
            await LoadAsync();
            return;
        }

        try
        {
            SetLoading(true);
            await _subscriptions.GetPaymentStatusAsync(paymentId);
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel atualizar o status da cobranca.");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async void OnOpenBoletoClicked(object? sender, EventArgs e)
    {
        var url = _overview?.PendingCharge?.BoletoUrl;
        if (string.IsNullOrWhiteSpace(url))
            return;

        await Launcher.Default.OpenAsync(url);
    }

    private async void OnCopyPixClicked(object? sender, EventArgs e)
    {
        var value = _overview?.PendingCharge?.PixQrCode;
        if (string.IsNullOrWhiteSpace(value))
            return;

        await Clipboard.Default.SetTextAsync(value);
        ShowNotice("PIX copia e cola copiado.");
    }

    private async void OnCopyBoletoLineClicked(object? sender, EventArgs e)
    {
        var value = _overview?.PendingCharge?.BoletoDigitableLine;
        if (string.IsNullOrWhiteSpace(value))
            return;

        await Clipboard.Default.SetTextAsync(value);
        ShowNotice("Linha digitavel copiada.");
    }

    private void StartPendingChargePolling()
    {
        _pollCts?.Cancel();
        var charge = _overview?.PendingCharge;
        if (charge is null || !string.Equals(charge.Status, "Pending", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(charge.ProviderPaymentId))
            return;

        var interval = string.Equals(charge.PaymentMethod, "boleto", StringComparison.OrdinalIgnoreCase)
            ? TimeSpan.FromSeconds(10)
            : TimeSpan.FromSeconds(5);

        _pollCts = new CancellationTokenSource();
        _ = PollPendingChargeAsync(charge.ProviderPaymentId, interval, _pollCts.Token);
    }

    private async Task PollPendingChargeAsync(string paymentId, TimeSpan interval, CancellationToken cancellationToken)
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
                        await LoadAsync();
                    });
                    return;
                }
            }
        }
        catch
        {
            // ignore polling errors
        }
    }

    private static string NormalizePaymentMethod(string paymentMethod)
        => paymentMethod?.Replace('_', ' ') switch
        {
            null => "—",
            "" => "—",
            var value => char.ToUpperInvariant(value[0]) + value[1..]
        };

    private void SetLoading(bool loading)
    {
        LoadingIndicator.IsRunning = loading;
        LoadingIndicator.IsVisible = loading;
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

    private void HideError()
    {
        ErrorLabel.Text = string.Empty;
        ErrorBorder.IsVisible = false;
    }

    private sealed record PlanRow(
        PlanDto Plan,
        string Name,
        string PriceLabel,
        List<string> FeatureItems,
        string ActionLabel,
        bool IsActive,
        bool IsPending,
        bool IsPopular,
        string BadgeText,
        bool HasBadge,
        Color BorderColor,
        Color BadgeBackground,
        Color BadgeTextColor,
        Color ActionBackground,
        Color ActionBorder,
        double ActionBorderWidth,
        Color ActionTextColor);
}
