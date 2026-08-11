using Microsoft.EntityFrameworkCore;
using server.Domain;
using server.Payments;
using server.Utils;

namespace server.Services;

public enum SubscriptionCancellationMode
{
    EndOfPeriod,
    RefundProrated
}

public class SubscriptionCancellationResult
{
    public bool cancelled { get; set; }
    public bool scheduled { get; set; }
    public decimal? refunded_amount { get; set; }
    public DateTime? effective_at { get; set; }
    public string message { get; set; } = string.Empty;
}

public class SubscriptionBillingService
{
    private readonly IDbContextFactory<Context> _contextFactory;
    private readonly IPaymentProvider _paymentProvider;
    private readonly IEmailNotification _emailNotification;
    private readonly ILogger<SubscriptionBillingService> _logger;
    private readonly List<string> _tags = new() { "SubscriptionBillingService" };
    private readonly string? _clientBaseUrl;
    private readonly string? _serverBaseUrl;
    private readonly string? _mercadoPagoWebhookUrl;

    public SubscriptionBillingService(
        IDbContextFactory<Context> contextFactory,
        IPaymentProvider paymentProvider,
        IEmailNotification emailNotification,
        ILogger<SubscriptionBillingService> logger)
    {
        _contextFactory = contextFactory;
        _paymentProvider = paymentProvider;
        _emailNotification = emailNotification;
        _logger = logger;
        _clientBaseUrl = Environment.GetEnvironmentVariable("BASE_URL_CLIENT");
        _serverBaseUrl = Environment.GetEnvironmentVariable("BASE_URL_SERVER");
        _mercadoPagoWebhookUrl = Environment.GetEnvironmentVariable("MERCADO_PAGO_WEBHOOK_URL");
    }

    public async Task<string> SavePayerCpfAsync(Guid userId, string? cpf, CancellationToken cancellationToken = default)
    {
        var traceId = TraceContext.GetTraceId();
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var normalizedCpf = CpfUtility.NormalizeOrThrow(cpf);
        var user = await context.users.FirstOrDefaultAsync(x => x.id == userId, cancellationToken)
            ?? throw new ExpectedException("Usuário não encontrado.");

        if (!string.Equals(user.cpf, normalizedCpf, StringComparison.Ordinal))
        {
            user.cpf = normalizedCpf;
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("CPF do pagador atualizado. TraceId={TraceId} UserId={UserId} {_tags_}", traceId, userId, _tags);
        }

        return normalizedCpf;
    }

    public async Task<PaymentResult> CreateInitialCardSubscriptionAsync(
        Guid userId,
        Plan plan,
        string cardToken,
        string paymentMethodId,
        string cardType,
        string issuerId,
        int installments,
        CancellationToken cancellationToken = default)
    {
        var paymentMethod = paymentMethodId.Contains("debit", StringComparison.OrdinalIgnoreCase) ? "debit_card" : "credit_card";
        return await CreateInitialHostedSubscriptionAsync(userId, plan, paymentMethod, cancellationToken);
    }

    public async Task<PaymentResult> CreateInitialPixSubscriptionAsync(
        Guid userId,
        Plan plan,
        string payerFirstName,
        string payerLastName,
        CancellationToken cancellationToken = default)
    {
        return await CreateInitialHostedCheckoutSubscriptionAsync(userId, plan, "pix", cancellationToken);
    }

    public async Task<PaymentResult> CreateInitialBoletoSubscriptionAsync(
        Guid userId,
        Plan plan,
        string payerFirstName,
        string payerLastName,
        CancellationToken cancellationToken = default)
    {
        return await CreateInitialHostedCheckoutSubscriptionAsync(userId, plan, "boleto", cancellationToken);
    }

    public async Task<PaymentResult> RefreshPaymentStatusAsync(Guid userId, string paymentId, CancellationToken cancellationToken = default)
    {
        var traceId = TraceContext.GetTraceId();
        _logger.LogInformation(
            "Atualizando status de pagamento. TraceId={TraceId} UserId={UserId} PaymentId={PaymentId} Tags={_tags_}",
            traceId,
            userId,
            paymentId,
            _tags);
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var charge = await FindChargeForStatusRefreshAsync(context, userId, paymentId, cancellationToken);

        if (charge == null)
        {
            var directResult = await ResolveProviderResultByReferenceAsync(paymentId, cancellationToken);
            charge = await FindChargeForProviderResultAsync(context, directResult, cancellationToken);
            if (charge == null || charge.user_id != userId)
                return directResult;

            await ApplyPaymentResultAsync(context, charge, directResult, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return directResult;
        }

        var result = await QueryChargeStatusAsync(charge, paymentId, cancellationToken);
        if (charge.status == SubscriptionChargeStatus.Cancelled)
            return result;

        await ApplyPaymentResultAsync(context, charge, result, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Status de pagamento atualizado. TraceId={TraceId} ChargeId={ChargeId} PaymentId={PaymentId} Status={Status} Tags={_tags_}",
            traceId,
            charge.id,
            paymentId,
            result.status,
            _tags);
        return result;
    }

    private async Task<PaymentResult> ResolveProviderResultByReferenceAsync(string reference, CancellationToken cancellationToken)
    {
        if (IsNumericProviderPaymentId(reference))
            return await _paymentProvider.GetPaymentStatusAsync(reference);

        if (LooksLikeMercadoPagoPreapprovalId(reference))
            return await _paymentProvider.GetSubscriptionStatusAsync(reference, cancellationToken);

        var paymentByExternalReference = await _paymentProvider.FindPaymentByExternalReferenceAsync(reference, cancellationToken);
        if (paymentByExternalReference != null)
            return paymentByExternalReference;

        throw new ExpectedException("Cobrança não encontrada.");
    }

    public async Task<SubscriptionCancellationResult> CancelActiveSubscriptionAsync(
        Guid userId,
        bool confirmed,
        SubscriptionCancellationMode mode,
        CancellationToken cancellationToken = default)
    {
        if (!confirmed)
        {
            return new SubscriptionCancellationResult
            {
                cancelled = false,
                scheduled = false,
                message = "Cancelamento ignorado."
            };
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var subscription = await context.subscriptions
            .Include(x => x.user)
            .Where(s => s.user_id == userId && s.plan_id != "free" && s.status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.created_at)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ExpectedException("Nenhuma assinatura ativa para cancelar.");

        if (!SupportsProratedRefund(subscription.payment_method))
        {
            await ScheduleCancellationAtPeriodEndAsync(context, subscription, now, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var result = new SubscriptionCancellationResult
            {
                cancelled = true,
                scheduled = true,
                effective_at = subscription.current_period_end,
                message = "Devido ao método de pagamento utilizado, o cancelamento foi programado para o fim do período atual. Nenhuma nova cobrança será enviada."
            };

            await SendCancellationEmailAsync(subscription.user, subscription, result);

            return result;
        }

        if (mode == SubscriptionCancellationMode.RefundProrated)
        {
            var currentCharge = await FindCurrentApprovedChargeAsync(
                context,
                subscription.id,
                subscription.current_period_start,
                subscription.current_period_end,
                cancellationToken)
                ?? throw new ExpectedException("Não foi possível localizar a cobrança atual para calcular o reembolso proporcional.");

            if (string.IsNullOrWhiteSpace(currentCharge.provider_payment_id))
                throw new ExpectedException("Não foi possível localizar o pagamento atual para solicitar o reembolso.");

            var refundAmount = CalculateProratedRefund(
                currentCharge.amount,
                subscription.current_period_start,
                subscription.current_period_end,
                now);

            if (refundAmount > 0)
            {
                await _paymentProvider.RefundPaymentAsync(currentCharge.provider_payment_id, refundAmount, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(subscription.mp_preapproval_id))
            {
                await _paymentProvider.CancelSubscriptionAsync(subscription.mp_preapproval_id, cancellationToken);
            }

            subscription.status = SubscriptionStatus.Cancelled;
            subscription.cancel_at_period_end = false;
            subscription.cancellation_requested_at = now;
            subscription.updated_at = now;
            await CancelPendingRenewalChargesAsync(context, subscription.id, now, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var result = new SubscriptionCancellationResult
            {
                cancelled = true,
                scheduled = false,
                refunded_amount = refundAmount,
                effective_at = now,
                message = refundAmount > 0
                    ? $"Assinatura cancelada imediatamente. Reembolso proporcional solicitado: R$ {refundAmount:N2}."
                    : "Assinatura cancelada imediatamente. Não havia saldo proporcional disponível para reembolso."
            };

            await SendCancellationEmailAsync(subscription.user, subscription, result);
            return result;
        }

        await ScheduleCancellationAtPeriodEndAsync(context, subscription, now, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var scheduledResult = new SubscriptionCancellationResult
        {
            cancelled = true,
            scheduled = true,
            effective_at = subscription.current_period_end,
            message = "O cancelamento foi programado para o fim do período atual. Sua assinatura permanecerá ativa até lá e nenhuma nova cobrança será enviada."
        };

        await SendCancellationEmailAsync(subscription.user, subscription, scheduledResult);
        return scheduledResult;
    }

    public async Task<SubscriptionCancellationResult> RevertScheduledCancellationAsync(
        Guid userId,
        bool confirmed,
        CancellationToken cancellationToken = default)
    {
        if (!confirmed)
        {
            return new SubscriptionCancellationResult
            {
                cancelled = false,
                scheduled = true,
                message = "Reversão do cancelamento ignorada."
            };
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var subscription = await context.subscriptions
            .Where(s =>
                s.user_id == userId &&
                s.plan_id != "free" &&
                s.status == SubscriptionStatus.Active &&
                s.cancel_at_period_end)
            .OrderByDescending(s => s.created_at)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ExpectedException("Não existe uma programação de cancelamento para reverter.");

        subscription.cancel_at_period_end = false;
        subscription.cancellation_requested_at = null;
        subscription.updated_at = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        return new SubscriptionCancellationResult
        {
            cancelled = false,
            scheduled = false,
            effective_at = subscription.current_period_end,
            message = "A programação de cancelamento foi revertida. Sua assinatura continuará renovando normalmente."
        };
    }

    public async Task ProcessPendingChargesAsync(CancellationToken cancellationToken = default)
    {
        var traceId = TraceContext.GetTraceId();
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var pendingCharges = await context.subscription_charges
            .Include(x => x.subscription)
            .Include(x => x.user)
            .Where(x => x.status == SubscriptionChargeStatus.Pending && (
                x.provider_payment_id != null ||
                x.provider_subscription_id != null ||
                x.provider_preference_id != null ||
                x.provider_external_reference != null))
            .OrderBy(x => x.created_at)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Iniciando reconciliacao de cobrancas pendentes. TraceId={TraceId} Count={Count} Tags={_tags_}", traceId, pendingCharges.Count, _tags);
        foreach (var charge in pendingCharges)
        {
            try
            {
                var result = await QueryChargeStatusAsync(charge, charge.id.ToString(), cancellationToken);
                await ApplyPaymentResultAsync(context, charge, result, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao reconciliar cobranca pendente. TraceId={TraceId} ChargeId={ChargeId} PaymentId={PaymentId} Tags={_tags_}", traceId, charge.id, charge.provider_payment_id, _tags);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task ProcessDueTomorrowRenewalNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var traceId = TraceContext.GetTraceId();
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var tomorrowStart = now.Date.AddDays(1);
        var tomorrowEnd = tomorrowStart.AddDays(1);

        var subscriptions = await context.subscriptions
            .Include(x => x.user)
            .Where(x =>
                x.status == SubscriptionStatus.Active &&
                !x.cancel_at_period_end &&
                x.plan_id != "free" &&
                x.current_period_end >= tomorrowStart &&
                x.current_period_end < tomorrowEnd)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Processando lembretes de renovacao. TraceId={TraceId} Count={Count} Tags={_tags_}", traceId, subscriptions.Count, _tags);
        foreach (var subscription in subscriptions)
        {
            try
            {
                if (IsCardPaymentMethod(subscription.payment_method))
                {
                    await ProcessCardReminderAsync(context, subscription, cancellationToken);
                    continue;
                }

                await ProcessOfflineRenewalReminderAsync(context, subscription, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao processar lembrete de renovacao. TraceId={TraceId} SubscriptionId={SubscriptionId} Tags={_tags_}", traceId, subscription.id, _tags);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task ProcessDueCardRenewalsAsync(CancellationToken cancellationToken = default)
    {
        var traceId = TraceContext.GetTraceId();
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var subscriptions = await context.subscriptions
            .Include(x => x.user)
            .Where(x =>
                x.status == SubscriptionStatus.Active &&
                !x.cancel_at_period_end &&
                IsCardPaymentMethod(x.payment_method) &&
                x.current_period_end <= now &&
                x.mp_preapproval_id == null &&
                x.mp_customer_id != null &&
                x.mp_card_id != null)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Processando renovacoes em cartao. TraceId={TraceId} Count={Count} Tags={_tags_}", traceId, subscriptions.Count, _tags);
        foreach (var subscription in subscriptions)
        {
            try
            {
                var existingCharge = await FindRenewalChargeAsync(context, subscription.id, subscription.current_period_end, cancellationToken);
                if (existingCharge != null)
                {
                    if (existingCharge.status == SubscriptionChargeStatus.Approved)
                    {
                        await ApplyApprovedRenewalAsync(context, existingCharge, cancellationToken);
                    }
                    continue;
                }

                var plan = Plans.GetById(subscription.plan_id);
                if (plan == null || plan.price <= 0)
                {
                    subscription.status = SubscriptionStatus.Expired;
                    subscription.updated_at = now;
                    continue;
                }

                var billingPeriodStart = subscription.current_period_end;
                var billingPeriodEnd = subscription.current_period_end.AddMonths(1);
                var externalReference = BuildExternalReference("renewal", subscription.user_id, subscription.plan_id);

                var result = await _paymentProvider.CreateSavedCardPaymentAsync(new SavedCardPaymentRequest
                {
                    customer_id = subscription.mp_customer_id!,
                    card_id = subscription.mp_card_id!,
                    amount = plan.price,
                    description = $"RendaTop - Renovação {plan.name}",
                    external_reference = externalReference
                });

                var charge = CreateCharge(
                    context,
                    subscription,
                    subscription.user_id,
                    subscription.plan_id,
                    subscription.payment_method,
                    plan.price,
                    subscription.user.cpf,
                    SubscriptionChargeKind.Renewal,
                    MapChargeStatus(result.status),
                    billingPeriodStart,
                    billingPeriodEnd,
                    subscription.current_period_end,
                    externalReference,
                    result);

                if (IsApproved(result.status))
                {
                    _logger.LogInformation(
                        "Renovacao com cartao aprovada. TraceId={TraceId} SubscriptionId={SubscriptionId} PaymentId={PaymentId} Tags={_tags_}",
                        traceId,
                        subscription.id,
                        result.payment_id,
                        _tags);
                    await ApplyApprovedRenewalAsync(context, charge, cancellationToken);
                }
                else
                {
                    _logger.LogWarning(
                        "Renovacao com cartao nao aprovada. TraceId={TraceId} SubscriptionId={SubscriptionId} PaymentId={PaymentId} Status={Status} StatusDetail={StatusDetail} Tags={_tags_}",
                        traceId,
                        subscription.id,
                        result.payment_id,
                        result.status,
                        result.status_detail,
                        _tags);
                    subscription.status = SubscriptionStatus.Expired;
                    subscription.updated_at = now;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao renovar assinatura com cartao. TraceId={TraceId} SubscriptionId={SubscriptionId}", traceId, subscription.id);
                subscription.status = SubscriptionStatus.Expired;
                subscription.updated_at = now;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task ExpireUnpaidRenewalsAsync(CancellationToken cancellationToken = default)
    {
        var traceId = TraceContext.GetTraceId();
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var subscriptions = await context.subscriptions
            .Include(x => x.user)
            .Where(x =>
                x.status == SubscriptionStatus.Active &&
                !x.cancel_at_period_end &&
                !IsCardPaymentMethod(x.payment_method) &&
                x.current_period_end < now)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Processando expiracao de renovacoes nao pagas. TraceId={TraceId} Count={Count} Tags={_tags_}", traceId, subscriptions.Count, _tags);
        foreach (var subscription in subscriptions)
        {
            var charge = await FindRenewalChargeAsync(context, subscription.id, subscription.current_period_end, cancellationToken);
            if (charge?.status == SubscriptionChargeStatus.Approved)
            {
                await ApplyApprovedRenewalAsync(context, charge, cancellationToken);
                continue;
            }

            if (charge != null && charge.status == SubscriptionChargeStatus.Pending)
            {
                charge.status = SubscriptionChargeStatus.Expired;
                charge.updated_at = now;
            }

            subscription.status = SubscriptionStatus.Expired;
            subscription.updated_at = now;
        }

        var expiredInitialCharges = await context.subscription_charges
            .Include(x => x.subscription)
            .Where(x =>
                x.status == SubscriptionChargeStatus.Pending &&
                x.charge_kind == SubscriptionChargeKind.Initial &&
                x.due_at.HasValue &&
                x.due_at < now)
            .ToListAsync(cancellationToken);

        foreach (var charge in expiredInitialCharges)
        {
            charge.status = SubscriptionChargeStatus.Expired;
            charge.updated_at = now;
            if (charge.subscription.status == SubscriptionStatus.PendingPayment)
            {
                charge.subscription.status = SubscriptionStatus.Cancelled;
                charge.subscription.updated_at = now;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task ProcessScheduledCancellationsAsync(CancellationToken cancellationToken = default)
    {
        var traceId = TraceContext.GetTraceId();
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var subscriptions = await context.subscriptions
            .Where(x =>
                x.status == SubscriptionStatus.Active &&
                x.cancel_at_period_end &&
                x.current_period_end <= now)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Processando cancelamentos agendados. TraceId={TraceId} Count={Count} Tags={_tags_}", traceId, subscriptions.Count, _tags);
        foreach (var subscription in subscriptions)
        {
            if (!string.IsNullOrWhiteSpace(subscription.mp_preapproval_id))
            {
                await _paymentProvider.CancelSubscriptionAsync(subscription.mp_preapproval_id, cancellationToken);
            }

            subscription.status = SubscriptionStatus.Cancelled;
            subscription.cancel_at_period_end = false;
            subscription.updated_at = now;
            await CancelPendingRenewalChargesAsync(context, subscription.id, now, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelPendingSubscriptionAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var traceId = TraceContext.GetTraceId();
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var sub = await context.subscriptions
            .Where(s => s.user_id == userId && s.plan_id != "free" && s.status == SubscriptionStatus.PendingPayment)
            .OrderByDescending(s => s.created_at)
            .FirstOrDefaultAsync(cancellationToken);

        if (sub == null)
            throw new ExpectedException("Nenhuma pendência para cancelar.");

        if (!string.IsNullOrWhiteSpace(sub.mp_preapproval_id))
        {
            await _paymentProvider.CancelSubscriptionAsync(sub.mp_preapproval_id, cancellationToken);
        }

        sub.status = SubscriptionStatus.Cancelled;
        sub.updated_at = DateTime.UtcNow;

        var charges = await context.subscription_charges
            .Where(x => x.subscription_id == sub.id && x.status == SubscriptionChargeStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var charge in charges)
        {
            charge.status = SubscriptionChargeStatus.Cancelled;
            charge.updated_at = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Assinatura pendente cancelada. TraceId={TraceId} UserId={UserId} SubscriptionId={SubscriptionId} Tags={_tags_}", traceId, userId, sub.id, _tags);
    }

    public async Task HandleMercadoPagoPaymentWebhookAsync(string paymentId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var result = await _paymentProvider.GetPaymentStatusAsync(paymentId);
        var charge = await FindChargeForProviderResultAsync(context, result, cancellationToken);
        if (charge == null)
            return;

        await ApplyPaymentResultAsync(context, charge, result, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleMercadoPagoSubscriptionWebhookAsync(string preapprovalId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var result = await _paymentProvider.GetSubscriptionStatusAsync(preapprovalId, cancellationToken);
        var charge = await context.subscription_charges
            .Include(x => x.subscription)
            .Include(x => x.user)
            .Where(x => x.provider_subscription_id == preapprovalId)
            .OrderByDescending(x => x.created_at)
            .FirstOrDefaultAsync(cancellationToken);

        if (charge == null)
            return;

        UpdateChargeFromResult(charge, result);
        charge.updated_at = DateTime.UtcNow;

        if (IsRejected(result.status) && charge.status == SubscriptionChargeStatus.Pending)
        {
            charge.status = SubscriptionChargeStatus.Cancelled;
            charge.subscription.status = SubscriptionStatus.Cancelled;
            charge.subscription.updated_at = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleMercadoPagoAuthorizedPaymentWebhookAsync(string authorizedPaymentId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var result = await _paymentProvider.GetAuthorizedPaymentStatusAsync(authorizedPaymentId, cancellationToken);
        var charge = await FindChargeForProviderResultAsync(context, result, cancellationToken);

        if (charge == null)
        {
            var subscription = await context.subscriptions
                .Include(x => x.user)
                .Where(x => x.mp_preapproval_id == result.preapproval_id && x.status == SubscriptionStatus.Active)
                .OrderByDescending(x => x.created_at)
                .FirstOrDefaultAsync(cancellationToken);

            if (subscription == null)
                return;

            var plan = Plans.GetById(subscription.plan_id)
                ?? throw new ExpectedException("Plano inválido.");

            charge = CreateCharge(
                context,
                subscription,
                subscription.user_id,
                subscription.plan_id,
                subscription.payment_method,
                result.amount ?? plan.price,
                subscription.user.cpf,
                SubscriptionChargeKind.Renewal,
                MapChargeStatus(result.status),
                subscription.current_period_end,
                subscription.current_period_end.AddMonths(1),
                subscription.current_period_end.AddMonths(1),
                result.external_reference ?? BuildExternalReference("renewal", subscription.user_id, subscription.plan_id),
                result);
        }

        await ApplyPaymentResultAsync(context, charge, result, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<PaymentResult> CreateInitialHostedSubscriptionAsync(
        Guid userId,
        Plan plan,
        string requestedPaymentMethod,
        CancellationToken cancellationToken)
    {
        var traceId = TraceContext.GetTraceId();
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var user = await context.users.FirstOrDefaultAsync(x => x.id == userId, cancellationToken)
            ?? throw new ExpectedException("Usuário não encontrado.");

        EnsureNoDuplicateCharge(context, userId, plan.id);

        var now = DateTime.UtcNow;
        var billingPeriodStart = now;
        var billingPeriodEnd = now.AddMonths(1);
        var externalReference = BuildExternalReference("sub", userId, plan.id);

        var result = await _paymentProvider.CreateHostedSubscriptionAsync(new HostedSubscriptionRequest
        {
            amount = plan.price,
            description = $"RendaTop - Plano {plan.name}",
            payer_email = user.email,
            external_reference = externalReference,
            back_url = BuildHostedCheckoutReturnUrl(),
            notification_url = BuildMercadoPagoWebhookUrl(),
            start_date = now,
            end_date = now.AddYears(10)
        }, cancellationToken);

        if (string.IsNullOrWhiteSpace(result.checkout_url) || string.IsNullOrWhiteSpace(result.preapproval_id))
            throw new ExpectedException("O Mercado Pago não retornou a URL do checkout hospedado.");

        _logger.LogInformation(
            "Assinatura hospedada criada. TraceId={TraceId} UserId={UserId} RequestedMethod={RequestedMethod} PreapprovalId={PreapprovalId} ExternalReference={ExternalReference} Tags={_tags_}",
            traceId,
            userId,
            requestedPaymentMethod,
            result.preapproval_id,
            externalReference,
            _tags);

        var subscription = CancelOldAndCreateSubscription(
            context,
            user.id,
            plan.id,
            SubscriptionStatus.PendingPayment,
            requestedPaymentMethod,
            null,
            null,
            null,
            billingPeriodStart,
            billingPeriodEnd,
            cancelExisting: false);

        subscription.mp_preapproval_id = result.preapproval_id;

        CreateCharge(
            context,
            subscription,
            user.id,
            plan.id,
            requestedPaymentMethod,
            plan.price,
            user.cpf,
            SubscriptionChargeKind.Initial,
            SubscriptionChargeStatus.Pending,
            billingPeriodStart,
            billingPeriodEnd,
            billingPeriodEnd,
            externalReference,
            result);

        await context.SaveChangesAsync(cancellationToken);
        return result;
    }

    private async Task<PaymentResult> CreateInitialHostedCheckoutSubscriptionAsync(
        Guid userId,
        Plan plan,
        string requestedPaymentMethod,
        CancellationToken cancellationToken)
    {
        var traceId = TraceContext.GetTraceId();
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var user = await context.users.FirstOrDefaultAsync(x => x.id == userId, cancellationToken)
            ?? throw new ExpectedException("Usuário não encontrado.");

        EnsureNoDuplicateCharge(context, userId, plan.id);

        var now = DateTime.UtcNow;
        var billingPeriodStart = now;
        var billingPeriodEnd = now.AddMonths(1);
        var externalReference = BuildExternalReference("sub", userId, plan.id);

        // Pix e boleto usam Checkout Pro avulso no Mercado Pago.
        // A recorrencia e o controle de acesso permanecem no nosso backend.
        var result = await _paymentProvider.CreateHostedCheckoutPreferenceAsync(new HostedCheckoutPreferenceRequest
        {
            title = $"RendaTop - Plano {plan.name}",
            description = $"Assinatura {plan.name} - pagamento inicial",
            amount = plan.price,
            payer_email = user.email,
            external_reference = externalReference,
            success_url = BuildHostedCheckoutReturnUrl(),
            pending_url = BuildHostedCheckoutReturnUrl(),
            failure_url = BuildHostedCheckoutReturnUrl(),
            notification_url = BuildMercadoPagoWebhookUrl(),
            payment_method = requestedPaymentMethod,
            date_of_expiration = now.AddDays(2)
        }, cancellationToken);

        if (string.IsNullOrWhiteSpace(result.checkout_url))
            throw new ExpectedException("O Mercado Pago não retornou a URL do Checkout Pro.");

        _logger.LogInformation(
            "Checkout Pro inicial criado. TraceId={TraceId} UserId={UserId} RequestedMethod={RequestedMethod} PreferenceId={PreferenceId} ExternalReference={ExternalReference} Tags={_tags_}",
            traceId,
            userId,
            requestedPaymentMethod,
            result.preference_id,
            externalReference,
            _tags);

        var subscription = CancelOldAndCreateSubscription(
            context,
            user.id,
            plan.id,
            SubscriptionStatus.PendingPayment,
            requestedPaymentMethod,
            null,
            null,
            null,
            billingPeriodStart,
            billingPeriodEnd,
            cancelExisting: false);

        CreateCharge(
            context,
            subscription,
            user.id,
            plan.id,
            requestedPaymentMethod,
            plan.price,
            user.cpf,
            SubscriptionChargeKind.Initial,
            SubscriptionChargeStatus.Pending,
            billingPeriodStart,
            billingPeriodEnd,
            result.date_of_expiration ?? now.AddDays(2),
            externalReference,
            result);

        await context.SaveChangesAsync(cancellationToken);
        return result;
    }

    private async Task<PaymentResult> CreateInitialOfflineSubscriptionAsync(
        Guid userId,
        Plan plan,
        string paymentMethod,
        string payerFirstName,
        string payerLastName,
        CancellationToken cancellationToken)
    {
        var traceId = TraceContext.GetTraceId();
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var user = await context.users.FirstOrDefaultAsync(x => x.id == userId, cancellationToken)
            ?? throw new ExpectedException("Usuário não encontrado.");

        EnsureNoDuplicateCharge(context, userId, plan.id);

        var now = DateTime.UtcNow;
        var billingPeriodStart = now;
        var billingPeriodEnd = now.AddMonths(1);
        var externalReference = BuildExternalReference("sub", userId, plan.id);
        var expiration = now.AddDays(1);

        _logger.LogInformation(
            "Iniciando assinatura offline. TraceId={TraceId} UserId={UserId} Payload={@Payload} Tags={_tags_}",
            traceId,
            userId,
            new
            {
                planId = plan.id,
                planName = plan.name,
                plan.price,
                paymentMethod,
                payerFirstName,
                payerLastName,
                externalReference,
                expiration
            },
            _tags);

        PaymentResult result;
        if (paymentMethod == "pix")
        {
            result = await _paymentProvider.CreatePixPaymentAsync(new PixPaymentRequest
            {
                amount = plan.price,
                description = $"RendaTop - Plano {plan.name}",
                payer_email = user.email,
                payer_first_name = payerFirstName,
                payer_last_name = payerLastName,
                payer_cpf = user.cpf,
                external_reference = externalReference,
                date_of_expiration = UtcDateTime.EnsureUtc(expiration)
            });
        }
        else
        {
            result = await _paymentProvider.CreateBoletoPaymentAsync(new BoletoPaymentRequest
            {
                amount = plan.price,
                description = $"RendaTop - Plano {plan.name}",
                payer_email = user.email,
                payer_first_name = payerFirstName,
                payer_last_name = payerLastName,
                payer_cpf = user.cpf,
                external_reference = externalReference,
                date_of_expiration = UtcDateTime.EnsureUtc(expiration)
            });
        }

        var subStatus = IsApproved(result.status) ? SubscriptionStatus.Active : SubscriptionStatus.PendingPayment;
        var subscription = CancelOldAndCreateSubscription(
            context,
            user.id,
            plan.id,
            subStatus,
            paymentMethod,
            result.payment_id,
            null,
            null,
            billingPeriodStart,
            billingPeriodEnd,
            cancelExisting: subStatus == SubscriptionStatus.Active);

        var charge = CreateCharge(
            context,
            subscription,
            user.id,
            plan.id,
            paymentMethod,
            plan.price,
            user.cpf,
            SubscriptionChargeKind.Initial,
            MapChargeStatus(result.status),
            billingPeriodStart,
            billingPeriodEnd,
            result.date_of_expiration ?? UtcDateTime.EnsureUtc(expiration),
            externalReference,
            result);

        await context.SaveChangesAsync(cancellationToken);

        if (IsApproved(result.status))
        {
            _logger.LogInformation(
                "Pagamento offline aprovado. TraceId={TraceId} UserId={UserId} PaymentId={PaymentId} Method={PaymentMethod} Tags={_tags_}",
                traceId,
                userId,
                result.payment_id,
                paymentMethod,
                _tags);
            await SendReceiptIfNeededAsync(context, user, charge, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
        else
        {
            _logger.LogInformation(
                "Pagamento offline criado. TraceId={TraceId} UserId={UserId} PaymentId={PaymentId} Method={PaymentMethod} Status={Status} StatusDetail={StatusDetail} Tags={_tags_}",
                traceId,
                userId,
                result.payment_id,
                paymentMethod,
                result.status,
                result.status_detail,
                _tags);
        }

        return result;
    }

    private async Task ProcessCardReminderAsync(Context context, Subscription subscription, CancellationToken cancellationToken)
    {
        var currentCharge = await context.subscription_charges
            .Include(x => x.user)
            .Where(x =>
                x.subscription_id == subscription.id &&
                x.status == SubscriptionChargeStatus.Approved &&
                x.billing_period_end == subscription.current_period_end)
            .OrderByDescending(x => x.created_at)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentCharge == null || currentCharge.reminder_sent_at.HasValue)
            return;

        await _emailNotification.Notify(
            subscription.user.email,
            "RendaTop | Renovação automática amanhã",
            BuildReminderMessage(subscription.user, currentCharge, true));

        currentCharge.reminder_sent_at = DateTime.UtcNow;
        currentCharge.updated_at = DateTime.UtcNow;
    }

    private async Task ProcessOfflineRenewalReminderAsync(Context context, Subscription subscription, CancellationToken cancellationToken)
    {
        var renewalCharge = await FindRenewalChargeAsync(context, subscription.id, subscription.current_period_end, cancellationToken);
        if (renewalCharge == null)
        {
            renewalCharge = await CreateOfflineRenewalChargeAsync(context, subscription, cancellationToken);
        }

        if (renewalCharge.status == SubscriptionChargeStatus.Pending && !renewalCharge.reminder_sent_at.HasValue)
        {
            await _emailNotification.Notify(
                subscription.user.email,
                "RendaTop | Renovação da assinatura amanhã",
                BuildReminderMessage(subscription.user, renewalCharge, false));

            renewalCharge.reminder_sent_at = DateTime.UtcNow;
            renewalCharge.updated_at = DateTime.UtcNow;
        }
    }

    private async Task<SubscriptionCharge> CreateOfflineRenewalChargeAsync(Context context, Subscription subscription, CancellationToken cancellationToken)
    {
        var traceId = TraceContext.GetTraceId();
        var plan = Plans.GetById(subscription.plan_id)
            ?? throw new ExpectedException("Plano inválido.");

        if (plan.price <= 0)
            throw new ExpectedException("O plano Free não requer renovação paga.");

        var billingPeriodStart = subscription.current_period_end;
        var billingPeriodEnd = subscription.current_period_end.AddMonths(1);
        var externalReference = BuildExternalReference("renewal", subscription.user_id, subscription.plan_id);

        _logger.LogInformation(
            "Criando cobranca de renovacao offline. TraceId={TraceId} SubscriptionId={SubscriptionId} PaymentMethod={PaymentMethod} ExternalReference={ExternalReference} Tags={_tags_}",
            traceId,
            subscription.id,
            subscription.payment_method,
            externalReference,
            _tags);

        // Renovacoes de Pix/boleto seguem como cobranca avulsa.
        // Se o usuario nao pagar ate o vencimento, a assinatura local expira.
        var result = await _paymentProvider.CreateHostedCheckoutPreferenceAsync(new HostedCheckoutPreferenceRequest
        {
            title = $"RendaTop - Renovação {plan.name}",
            description = $"Assinatura {plan.name} - renovação manual",
            amount = plan.price,
            payer_email = subscription.user.email,
            external_reference = externalReference,
            success_url = BuildHostedCheckoutReturnUrl(),
            pending_url = BuildHostedCheckoutReturnUrl(),
            failure_url = BuildHostedCheckoutReturnUrl(),
            notification_url = BuildMercadoPagoWebhookUrl(),
            payment_method = subscription.payment_method,
            date_of_expiration = UtcDateTime.EnsureUtc(subscription.current_period_end)
        }, cancellationToken);

        var charge = CreateCharge(
            context,
            subscription,
            subscription.user_id,
            subscription.plan_id,
            subscription.payment_method,
            plan.price,
            subscription.user.cpf,
            SubscriptionChargeKind.Renewal,
            MapChargeStatus(result.status),
            billingPeriodStart,
            billingPeriodEnd,
            result.date_of_expiration ?? UtcDateTime.EnsureUtc(subscription.current_period_end),
            externalReference,
            result);

        if (charge.status == SubscriptionChargeStatus.Approved)
        {
            await ApplyApprovedRenewalAsync(context, charge, cancellationToken);
        }

        _logger.LogInformation(
            "Cobranca de renovacao offline registrada. TraceId={TraceId} SubscriptionId={SubscriptionId} ChargeId={ChargeId} PaymentId={PaymentId} Status={Status} Tags={_tags_}",
            traceId,
            subscription.id,
            charge.id,
            charge.provider_payment_id,
            charge.status,
            _tags);

        return charge;
    }

    private async Task ApplyPaymentResultAsync(Context context, SubscriptionCharge charge, PaymentResult result, CancellationToken cancellationToken)
    {
        var traceId = TraceContext.GetTraceId();
        if (charge.status == SubscriptionChargeStatus.Cancelled)
        {
            charge.provider_status_detail = result.status_detail;
            charge.updated_at = DateTime.UtcNow;
            _logger.LogInformation("Resultado ignorado para cobranca cancelada. TraceId={TraceId} ChargeId={ChargeId} PaymentId={PaymentId} Tags={_tags_}", traceId, charge.id, charge.provider_payment_id, _tags);
            return;
        }

        UpdateChargeFromResult(charge, result);
        _logger.LogInformation(
            "Aplicando resultado de pagamento. TraceId={TraceId} ChargeId={ChargeId} PaymentId={PaymentId} Status={Status} StatusDetail={StatusDetail} Tags={_tags_}",
            traceId,
            charge.id,
            result.payment_id,
            result.status,
            result.status_detail, _tags);

        if (IsApproved(result.status))
        {
            charge.status = SubscriptionChargeStatus.Approved;
            charge.approved_at ??= result.approved_at ?? DateTime.UtcNow;
            charge.updated_at = DateTime.UtcNow;

            if (charge.charge_kind == SubscriptionChargeKind.Initial)
            {
                await ActivateInitialSubscriptionAsync(context, charge, cancellationToken);
            }
            else
            {
                await ApplyApprovedRenewalAsync(context, charge, cancellationToken);
            }

            await SendReceiptIfNeededAsync(context, charge.user, charge, cancellationToken);
            return;
        }

        if (IsRejected(result.status))
        {
            charge.status = MapChargeStatus(result.status);
            charge.updated_at = DateTime.UtcNow;

            if (charge.charge_kind == SubscriptionChargeKind.Initial && charge.subscription.status == SubscriptionStatus.PendingPayment)
            {
                charge.subscription.status = SubscriptionStatus.Cancelled;
                charge.subscription.updated_at = DateTime.UtcNow;
            }
        }
    }

    private async Task ActivateInitialSubscriptionAsync(Context context, SubscriptionCharge charge, CancellationToken cancellationToken)
    {
        charge.subscription.status = SubscriptionStatus.Active;
        charge.subscription.mp_payment_id = charge.provider_payment_id;
        charge.subscription.mp_preapproval_id = charge.provider_subscription_id ?? charge.subscription.mp_preapproval_id;
        charge.subscription.payment_method = charge.payment_method;
        charge.subscription.current_period_start = charge.billing_period_start;
        charge.subscription.current_period_end = charge.billing_period_end;
        charge.subscription.updated_at = DateTime.UtcNow;

        var others = await context.subscriptions
            .Where(s =>
                s.user_id == charge.user_id &&
                s.id != charge.subscription_id &&
                (s.status == SubscriptionStatus.Active || s.status == SubscriptionStatus.PendingPayment))
            .ToListAsync(cancellationToken);

        foreach (var item in others)
        {
            item.status = SubscriptionStatus.Cancelled;
            item.updated_at = DateTime.UtcNow;
        }
    }

    private async Task ApplyApprovedRenewalAsync(Context context, SubscriptionCharge charge, CancellationToken cancellationToken)
    {
        charge.subscription.status = SubscriptionStatus.Active;
        charge.subscription.mp_payment_id = charge.provider_payment_id;
        charge.subscription.mp_preapproval_id = charge.provider_subscription_id ?? charge.subscription.mp_preapproval_id;
        charge.subscription.payment_method = charge.payment_method;
        charge.subscription.current_period_start = charge.billing_period_start;
        charge.subscription.current_period_end = charge.billing_period_end;
        charge.subscription.updated_at = DateTime.UtcNow;
        charge.updated_at = DateTime.UtcNow;

        if (!charge.approved_at.HasValue)
            charge.approved_at = DateTime.UtcNow;

        await SendReceiptIfNeededAsync(context, charge.user, charge, cancellationToken);
    }

    private async Task SendReceiptIfNeededAsync(Context context, User user, SubscriptionCharge charge, CancellationToken cancellationToken)
    {
        var traceId = TraceContext.GetTraceId();
        if (charge.receipt_sent_at.HasValue)
            return;

        _logger.LogInformation("Enviando recibo de assinatura. TraceId={TraceId} UserId={UserId} ChargeId={ChargeId} Tags={_tags_}", traceId, user.id, charge.id, _tags);
        await _emailNotification.Notify(
            user.email,
            "RendaTop | Recibo de assinatura",
            SubscriptionReceiptEmailTemplate.Build(user, charge, _clientBaseUrl),
            isHtml: true);

        charge.receipt_sent_at = DateTime.UtcNow;
        charge.updated_at = DateTime.UtcNow;
    }

    private async Task SendCancellationEmailAsync(User user, Subscription subscription, SubscriptionCancellationResult result)
    {
        _logger.LogInformation(
            "Enviando email de cancelamento. TraceId={TraceId} UserId={UserId} SubscriptionId={SubscriptionId} Payload={@Payload} Tags={_tags_}",
            TraceContext.GetTraceId(),
            user.id,
            subscription.id,
            new
            {
                result.cancelled,
                result.scheduled,
                result.refunded_amount,
                result.effective_at
            },
            _tags);
        await _emailNotification.Notify(
            user.email,
            "RendaTop | Cancelamento de assinatura",
            SubscriptionCancellationEmailTemplate.Build(user, subscription, result, _clientBaseUrl),
            isHtml: true);
    }

    private void EnsureNoDuplicateCharge(Context context, Guid userId, string planId)
    {
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfMonth = startOfMonth.AddMonths(1);

        bool alreadyCharged = context.subscriptions.Any(s =>
            s.user_id == userId &&
            s.plan_id == planId &&
            (s.status == SubscriptionStatus.Active || s.status == SubscriptionStatus.PendingPayment) &&
            s.current_period_start >= startOfMonth &&
            s.current_period_start < endOfMonth);

        if (alreadyCharged)
        {
            throw new ExpectedException("Você já possui uma assinatura ativa ou pagamento pendente para este plano neste mês. Nenhuma cobrança adicional será realizada.");
        }
    }

    private Subscription CancelOldAndCreateSubscription(
        Context context,
        Guid userId,
        string planId,
        SubscriptionStatus status,
        string paymentMethod,
        string? paymentId,
        string? customerId,
        string? cardId,
        DateTime billingPeriodStart,
        DateTime billingPeriodEnd,
        bool cancelExisting)
    {
        if (cancelExisting)
        {
            var oldSubs = context.subscriptions
                .Where(s =>
                    s.user_id == userId &&
                    (s.status == SubscriptionStatus.Active || s.status == SubscriptionStatus.PendingPayment))
                .ToList();

            foreach (var old in oldSubs)
            {
                old.status = SubscriptionStatus.Cancelled;
                old.updated_at = DateTime.UtcNow;
            }
        }

        var sub = new Subscription
        {
            user_id = userId,
            plan_id = planId,
            status = status,
            payment_method = paymentMethod,
            mp_payment_id = paymentId,
            mp_customer_id = customerId,
            mp_card_id = cardId,
            mp_preapproval_id = null,
            current_period_start = billingPeriodStart,
            current_period_end = billingPeriodEnd,
            cancel_at_period_end = false,
            created_at = DateTime.UtcNow,
            updated_at = DateTime.UtcNow
        };

        context.subscriptions.Add(sub);
        return sub;
    }

    private SubscriptionCharge CreateCharge(
        Context context,
        Subscription subscription,
        Guid userId,
        string planId,
        string paymentMethod,
        decimal amount,
        string payerCpf,
        SubscriptionChargeKind chargeKind,
        SubscriptionChargeStatus status,
        DateTime billingPeriodStart,
        DateTime billingPeriodEnd,
        DateTime? dueAt,
        string externalReference,
        PaymentResult result)
    {
        var charge = new SubscriptionCharge
        {
            subscription = subscription,
            user_id = userId,
            plan_id = planId,
            payment_method = paymentMethod,
            amount = amount,
            payer_cpf = payerCpf,
            provider_payment_id = string.IsNullOrWhiteSpace(result.payment_id) ? null : result.payment_id,
            provider_preference_id = string.IsNullOrWhiteSpace(result.preference_id) ? null : result.preference_id,
            provider_subscription_id = string.IsNullOrWhiteSpace(result.preapproval_id) ? null : result.preapproval_id,
            provider_external_reference = externalReference,
            provider_checkout_url = result.checkout_url,
            provider_status_detail = result.status_detail,
            status = status,
            charge_kind = chargeKind,
            billing_period_start = billingPeriodStart,
            billing_period_end = billingPeriodEnd,
            due_at = UtcDateTime.EnsureUtc(dueAt) ?? UtcDateTime.EnsureUtc(result.date_of_expiration),
            approved_at = status == SubscriptionChargeStatus.Approved ? UtcDateTime.EnsureUtc(result.approved_at) ?? DateTime.UtcNow : null,
            created_at = DateTime.UtcNow,
            updated_at = DateTime.UtcNow
        };

        UpdateChargeFromResult(charge, result);
        context.subscription_charges.Add(charge);
        return charge;
    }

    private static void UpdateChargeFromResult(SubscriptionCharge charge, PaymentResult result)
    {
        charge.provider_payment_id = string.IsNullOrWhiteSpace(result.payment_id) ? charge.provider_payment_id : result.payment_id;
        charge.provider_preference_id = string.IsNullOrWhiteSpace(result.preference_id) ? charge.provider_preference_id : result.preference_id;
        charge.provider_subscription_id = string.IsNullOrWhiteSpace(result.preapproval_id) ? charge.provider_subscription_id : result.preapproval_id;
        charge.provider_status_detail = result.status_detail;
        charge.provider_checkout_url = result.checkout_url ?? charge.provider_checkout_url;
        charge.amount = result.amount ?? charge.amount;
        charge.due_at = UtcDateTime.EnsureUtc(result.date_of_expiration) ?? UtcDateTime.EnsureUtc(charge.due_at)!.Value;
        charge.payment_method = NormalizePaymentMethod(result.payment_method) ?? charge.payment_method;
        charge.pix_qr_code = result.pix_qr_code ?? charge.pix_qr_code;
        charge.pix_qr_code_base64 = result.pix_qr_code_base64 ?? charge.pix_qr_code_base64;
        charge.boleto_barcode_content = result.boleto_barcode_content ?? charge.boleto_barcode_content;
        charge.boleto_barcode_image_base64 = result.boleto_barcode_image_base64 ?? charge.boleto_barcode_image_base64;
        charge.boleto_digitable_line = result.boleto_digitable_line ?? charge.boleto_digitable_line;
        charge.boleto_url = result.boleto_url ?? charge.boleto_url;
    }

    private async Task<SubscriptionCharge?> FindRenewalChargeAsync(Context context, Guid subscriptionId, DateTime billingPeriodStart, CancellationToken cancellationToken)
    {
        return await context.subscription_charges
            .Include(x => x.subscription)
            .Include(x => x.user)
            .Where(x =>
                x.subscription_id == subscriptionId &&
                x.charge_kind == SubscriptionChargeKind.Renewal &&
                x.billing_period_start == billingPeriodStart)
            .OrderByDescending(x => x.created_at)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<SubscriptionCharge?> FindCurrentApprovedChargeAsync(
        Context context,
        Guid subscriptionId,
        DateTime billingPeriodStart,
        DateTime billingPeriodEnd,
        CancellationToken cancellationToken)
    {
        return await context.subscription_charges
            .Where(x =>
                x.subscription_id == subscriptionId &&
                x.status == SubscriptionChargeStatus.Approved &&
                x.billing_period_start == billingPeriodStart &&
                x.billing_period_end == billingPeriodEnd)
            .OrderByDescending(x => x.created_at)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task ScheduleCancellationAtPeriodEndAsync(Context context, Subscription subscription, DateTime now, CancellationToken cancellationToken)
    {
        subscription.cancel_at_period_end = true;
        subscription.cancellation_requested_at = now;
        subscription.updated_at = now;
        await CancelPendingRenewalChargesAsync(context, subscription.id, now, cancellationToken);
    }

    private async Task CancelPendingRenewalChargesAsync(Context context, Guid subscriptionId, DateTime now, CancellationToken cancellationToken)
    {
        var charges = await context.subscription_charges
            .Where(x =>
                x.subscription_id == subscriptionId &&
                x.charge_kind == SubscriptionChargeKind.Renewal &&
                x.status == SubscriptionChargeStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var charge in charges)
        {
            charge.status = SubscriptionChargeStatus.Cancelled;
            charge.updated_at = now;
        }
    }

    private static decimal CalculateProratedRefund(decimal amount, DateTime currentPeriodStart, DateTime currentPeriodEnd, DateTime now)
    {
        if (amount <= 0)
            return 0;

        if (now >= currentPeriodEnd)
            return 0;

        if (now <= currentPeriodStart)
            return amount;

        var totalSeconds = (decimal)(currentPeriodEnd - currentPeriodStart).TotalSeconds;
        if (totalSeconds <= 0)
            return 0;

        var remainingSeconds = (decimal)(currentPeriodEnd - now).TotalSeconds;
        if (remainingSeconds <= 0)
            return 0;

        var prorated = Math.Round(amount * (remainingSeconds / totalSeconds), 2, MidpointRounding.AwayFromZero);
        if (prorated < 0) return 0;
        if (prorated > amount) return amount;
        return prorated;
    }

    private async Task<SubscriptionCharge?> FindChargeForStatusRefreshAsync(Context context, Guid userId, string reference, CancellationToken cancellationToken)
    {
        var query = context.subscription_charges
            .Include(x => x.subscription)
            .Include(x => x.user)
            .Where(x => x.user_id == userId);

        if (Guid.TryParse(reference, out var chargeId))
        {
            return await query
                .Where(x => x.id == chargeId)
                .OrderByDescending(x => x.created_at)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return await query
            .Where(x => x.provider_payment_id == reference || x.provider_subscription_id == reference || x.provider_preference_id == reference)
            .OrderByDescending(x => x.created_at)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<PaymentResult> QueryChargeStatusAsync(SubscriptionCharge charge, string reference, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(charge.provider_payment_id))
            return await _paymentProvider.GetPaymentStatusAsync(charge.provider_payment_id!);

        if (!string.IsNullOrWhiteSpace(charge.provider_subscription_id))
            return await _paymentProvider.GetSubscriptionStatusAsync(charge.provider_subscription_id!, cancellationToken);

        if (!string.IsNullOrWhiteSpace(charge.provider_external_reference))
        {
            var paymentByExternalReference = await _paymentProvider.FindPaymentByExternalReferenceAsync(charge.provider_external_reference!, cancellationToken);
            if (paymentByExternalReference != null)
                return paymentByExternalReference;
        }

        if (!string.IsNullOrWhiteSpace(charge.provider_preference_id))
        {
            return new PaymentResult
            {
                status = charge.status == SubscriptionChargeStatus.Approved ? "approved" : "pending",
                status_detail = charge.provider_status_detail,
                checkout_url = charge.provider_checkout_url,
                external_reference = charge.provider_external_reference,
                preference_id = charge.provider_preference_id,
                amount = charge.amount,
                payment_method = charge.payment_method,
                date_of_expiration = charge.due_at
            };
        }

        throw new ExpectedException($"Cobrança {reference} não possui identificadores do provedor para consulta.");
    }

    private async Task<SubscriptionCharge?> FindChargeForProviderResultAsync(Context context, PaymentResult result, CancellationToken cancellationToken)
    {
        var query = context.subscription_charges
            .Include(x => x.subscription)
            .Include(x => x.user)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(result.payment_id))
        {
            var byPayment = await query
                .Where(x => x.provider_payment_id == result.payment_id)
                .OrderByDescending(x => x.created_at)
                .FirstOrDefaultAsync(cancellationToken);

            if (byPayment != null)
                return byPayment;
        }

        if (!string.IsNullOrWhiteSpace(result.external_reference))
        {
            var byExternalReference = await query
                .Where(x => x.provider_external_reference == result.external_reference)
                .OrderByDescending(x => x.created_at)
                .FirstOrDefaultAsync(cancellationToken);

            if (byExternalReference != null)
                return byExternalReference;
        }

        if (!string.IsNullOrWhiteSpace(result.preapproval_id))
        {
            return await query
                .Where(x => x.provider_subscription_id == result.preapproval_id)
                .OrderByDescending(x => x.created_at)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return null;
    }

    private static SubscriptionChargeStatus MapChargeStatus(string providerStatus)
    {
        if (IsApproved(providerStatus)) return SubscriptionChargeStatus.Approved;
        if (IsRejected(providerStatus)) return SubscriptionChargeStatus.Rejected;
        return SubscriptionChargeStatus.Pending;
    }

    private static bool IsApproved(string status) =>
        string.Equals(status, "approved", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "authorized", StringComparison.OrdinalIgnoreCase);

    private static bool IsPending(string status) =>
        string.Equals(status, "pending", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "in_process", StringComparison.OrdinalIgnoreCase);

    private static bool IsRejected(string status) =>
        string.Equals(status, "rejected", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "cancelled", StringComparison.OrdinalIgnoreCase);

    private static bool IsCardPaymentMethod(string paymentMethod) =>
        string.Equals(paymentMethod, "credit_card", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(paymentMethod, "debit_card", StringComparison.OrdinalIgnoreCase);

    private static bool IsPixPaymentMethod(string paymentMethod) =>
        string.Equals(paymentMethod, "pix", StringComparison.OrdinalIgnoreCase);

    private static bool SupportsProratedRefund(string paymentMethod) =>
        IsCardPaymentMethod(paymentMethod) || IsPixPaymentMethod(paymentMethod);

    private static bool IsNumericProviderPaymentId(string reference) =>
        !string.IsNullOrWhiteSpace(reference) && reference.All(char.IsDigit);

    private static bool LooksLikeMercadoPagoPreapprovalId(string reference) =>
        !string.IsNullOrWhiteSpace(reference) &&
        reference.Length >= 20 &&
        reference.All(char.IsLetterOrDigit);

    private static string? NormalizePaymentMethod(string? providerPaymentMethod)
    {
        if (string.IsNullOrWhiteSpace(providerPaymentMethod))
            return null;

        if (string.Equals(providerPaymentMethod, "pix", StringComparison.OrdinalIgnoreCase))
            return "pix";

        if (providerPaymentMethod.StartsWith("bol", StringComparison.OrdinalIgnoreCase))
            return "boleto";

        if (providerPaymentMethod.Contains("debit", StringComparison.OrdinalIgnoreCase))
            return "debit_card";

        return "credit_card";
    }

    private static string BuildExternalReference(string prefix, Guid userId, string planId)
    {
        var compactGuid = Guid.NewGuid().ToString("N")[..8];
        return $"{prefix}_{userId}_{planId}_{DateTime.UtcNow:yyyyMMddHHmmss}_{compactGuid}";
    }

    private string BuildHostedCheckoutReturnUrl()
    {
        var publicUrl = GetMercadoPagoPublicUrl();
        if (!Uri.TryCreate(publicUrl.Trim(), UriKind.Absolute, out var callbackUri))
            throw new ExpectedException("MERCADO_PAGO_WEBHOOK_CALLBACK configurada de forma inválida para retorno do checkout.");

        EnsureMercadoPagoPublicUrl(callbackUri, "MERCADO_PAGO_WEBHOOK_CALLBACK");
        return callbackUri.ToString();
    }

    private string BuildMercadoPagoWebhookUrl()
    {
        if (!string.IsNullOrWhiteSpace(_mercadoPagoWebhookUrl))
        {
            if (Uri.TryCreate(_mercadoPagoWebhookUrl.Trim(), UriKind.Absolute, out var explicitWebhookUri))
            {
                EnsureMercadoPagoPublicUrl(explicitWebhookUri, "MERCADO_PAGO_WEBHOOK_URL");
                return explicitWebhookUri.ToString();
            }

            throw new ExpectedException("MERCADO_PAGO_WEBHOOK_URL configurada de forma inválida.");
        }

        if (string.IsNullOrWhiteSpace(_serverBaseUrl))
            throw new ExpectedException("BASE_URL_SERVER não configurado para receber webhooks do Mercado Pago.");

        var webhookUrl = $"{_serverBaseUrl.TrimEnd('/')}/subscription/webhook/mercado-pago";
        if (!Uri.TryCreate(webhookUrl, UriKind.Absolute, out var webhookUri))
            throw new ExpectedException("BASE_URL_SERVER configurado de forma inválida para receber webhooks do Mercado Pago.");

        EnsureMercadoPagoPublicUrl(webhookUri, "BASE_URL_SERVER");
        return webhookUri.ToString();
    }

    private static string GetMercadoPagoPublicUrl()
    {
        string url = Environment.GetEnvironmentVariable("MERCADO_PAGO_WEBHOOK_CALLBACK") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(url))
            throw new ExpectedException("Variavel de ambiente MERCADO_PAGO_WEBHOOK_CALLBACK não configurada para retorno do checkout do Mercado Pago. Informe a URL pública completa de retorno do usuário.");

        return url;
    }

    private static void EnsureMercadoPagoPublicUrl(Uri uri, string envName)
    {
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new ExpectedException($"{envName} deve usar http:// ou https:// para integração com o Mercado Pago.");
        }

        var host = uri.Host.Trim().ToLowerInvariant();
        if (host == "localhost" || host == "127.0.0.1" || host == "::1")
        {
            throw new ExpectedException(
                $"{envName} está apontando para '{uri}'. O Mercado Pago não aceita localhost como back_url ou webhook. Use uma URL pública do frontend/backend, por exemplo via domínio de teste, ngrok ou Cloudflare Tunnel.");
        }
    }

    private static (string firstName, string lastName) SplitName(string fullName)
    {
        var cleaned = (fullName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(cleaned))
            return ("Cliente", "RendaTop");

        var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
            return (parts[0], parts[0]);

        return (parts[0], string.Join(" ", parts.Skip(1)));
    }

    private static string BuildReminderMessage(User user, SubscriptionCharge charge, bool automaticCardCharge)
    {
        var plan = Plans.GetById(charge.plan_id);
        var dueAt = FormatLocalDateTime(charge.due_at ?? charge.billing_period_end);
        var body = automaticCardCharge
            ? "Amanhã será feita uma nova cobrança automática no cartão cadastrado para renovar sua assinatura."
            : "Amanhã você deverá concluir o novo pagamento via PIX ou boleto. Se o pagamento não for identificado, a assinatura será cancelada.";

        return
            $"Olá, {user.name}!{Environment.NewLine}{Environment.NewLine}" +
            $"Sua assinatura {plan?.name ?? charge.plan_id} vence em {dueAt}.{Environment.NewLine}" +
            body;
    }

    private static string FormatLocalDateTime(DateTime value)
    {
        return value.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
    }

}
