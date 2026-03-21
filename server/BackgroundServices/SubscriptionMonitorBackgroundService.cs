using Microsoft.EntityFrameworkCore;
using server.Domain;
using server.Payments;

namespace server.BackgroundServices;

/// <summary>
/// Serviço que monitora assinaturas periodicamente (a cada 6 horas).
/// 
/// Responsabilidades:
/// 1. Renovar automaticamente assinaturas com cartão salvo quando o período vence
/// 2. Marcar como expiradas assinaturas PIX/boleto cujo período venceu sem renovação
/// 3. Verificar no Mercado Pago se pagamentos pendentes foram aprovados
///
/// LEI: Apenas UMA cobrança por mês por assinatura. Verificação rigorosa antes de cobrar.
/// </summary>
public class SubscriptionMonitorBackgroundService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SubscriptionMonitorBackgroundService> _logger;
    private Timer? _timer;

    public SubscriptionMonitorBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<SubscriptionMonitorBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SubscriptionMonitor iniciado. Checagem a cada 6 horas.");
        _timer = new Timer(DoWork, null, TimeSpan.FromMinutes(5), TimeSpan.FromHours(6));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<Context>>();
            using var context = contextFactory.CreateDbContext();
            var payment = scope.ServiceProvider.GetRequiredService<IPaymentProvider>();

            await CheckPendingPayments(context, payment);
            await RenewExpiredCardSubscriptions(context, payment);
            ExpireNonCardSubscriptions(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no SubscriptionMonitor");
        }
    }


    /// <summary>
    /// Verifica pagamentos PIX/boleto pendentes e ativa se aprovados
    /// </summary>
    private async Task CheckPendingPayments(Context context, IPaymentProvider payment)
    {
        var pendingSubs = context.subscriptions
            .Where(s => s.status == SubscriptionStatus.PendingPayment && s.mp_payment_id != null)
            .ToList();

        foreach (var sub in pendingSubs)
        {
            try
            {
                var result = await payment.GetPaymentStatusAsync(sub.mp_payment_id!);

                if (result.status == "approved")
                {
                    sub.status = SubscriptionStatus.Active;
                    sub.current_period_start = DateTime.UtcNow;
                    sub.current_period_end = DateTime.UtcNow.AddMonths(1);
                    sub.updated_at = DateTime.UtcNow;
                    _logger.LogInformation("Assinatura {Id} ativada via pagamento pendente", sub.id);
                }
                else if (result.status == "rejected" || result.status == "cancelled")
                {
                    sub.status = SubscriptionStatus.Cancelled;
                    sub.updated_at = DateTime.UtcNow;
                    _logger.LogInformation("Assinatura {Id} cancelada — pagamento {Status}", sub.id, result.status);
                }
                // Se ainda pending, mantém como está
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao verificar pagamento pendente {PaymentId}", sub.mp_payment_id);
            }
        }

        context.SaveChanges();
    }


    /// <summary>
    /// Renova automaticamente assinaturas com cartão salvo cujo período expirou.
    /// LEI: Verifica rigorosamente se já houve cobrança neste mês antes de cobrar.
    /// </summary>
    private async Task RenewExpiredCardSubscriptions(Context context, IPaymentProvider payment)
    {
        var now = DateTime.UtcNow;
        var expiredCardSubs = context.subscriptions
            .Where(s => s.status == SubscriptionStatus.Active
                        && s.current_period_end < now
                        && s.mp_customer_id != null
                        && s.mp_card_id != null)
            .ToList();

        foreach (var sub in expiredCardSubs)
        {
            try
            {
                var plan = Plans.GetById(sub.plan_id);
                if (plan == null || plan.price <= 0)
                {
                    sub.status = SubscriptionStatus.Expired;
                    sub.updated_at = now;
                    continue;
                }

                // LEI: Verificar se NÃO existe nenhuma outra cobrança ativa neste mês
                var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var endOfMonth = startOfMonth.AddMonths(1);

                bool alreadyChargedThisMonth = context.subscriptions.Any(s =>
                    s.user_id == sub.user_id
                    && s.plan_id == sub.plan_id
                    && s.id != sub.id
                    && (s.status == SubscriptionStatus.Active || s.status == SubscriptionStatus.PendingPayment)
                    && s.current_period_start >= startOfMonth
                    && s.current_period_start < endOfMonth
                );

                if (alreadyChargedThisMonth)
                {
                    _logger.LogWarning("LEI: Cobrança duplicada prevenida para user={UserId} plan={PlanId}", sub.user_id, sub.plan_id);
                    continue;
                }

                var externalRef = $"renewal_{sub.user_id}_{sub.plan_id}_{now:yyyyMMdd}";

                var result = await payment.CreateSavedCardPaymentAsync(new SavedCardPaymentRequest
                {
                    customer_id = sub.mp_customer_id!,
                    card_id = sub.mp_card_id!,
                    amount = plan.price,
                    description = $"RendaTop - Renovação {plan.name}",
                    external_reference = externalRef
                });

                if (result.status == "approved")
                {
                    sub.mp_payment_id = result.payment_id;
                    sub.current_period_start = now;
                    sub.current_period_end = now.AddMonths(1);
                    sub.updated_at = now;
                    _logger.LogInformation("Assinatura {Id} renovada automaticamente", sub.id);
                }
                else
                {
                    sub.status = SubscriptionStatus.Expired;
                    sub.updated_at = now;
                    _logger.LogWarning("Renovação falhou para assinatura {Id}: {Status}", sub.id, result.status);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro na renovação automática da assinatura {Id}", sub.id);
                sub.status = SubscriptionStatus.Expired;
                sub.updated_at = now;
            }
        }

        context.SaveChanges();
    }


    /// <summary>
    /// Expira assinaturas PIX/boleto cujo período venceu sem renovação
    /// </summary>
    private void ExpireNonCardSubscriptions(Context context)
    {
        var now = DateTime.UtcNow;
        var expiredNonCard = context.subscriptions
            .Where(s => s.status == SubscriptionStatus.Active
                        && s.current_period_end < now
                        && (s.mp_customer_id == null || s.mp_card_id == null))
            .ToList();

        foreach (var sub in expiredNonCard)
        {
            sub.status = SubscriptionStatus.Expired;
            sub.updated_at = now;
            _logger.LogInformation("Assinatura {Id} expirada (PIX/boleto sem renovação)", sub.id);
        }

        context.SaveChanges();
    }
}
