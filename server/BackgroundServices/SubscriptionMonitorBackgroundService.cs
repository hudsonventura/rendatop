using server.Services;
using server.Utils;

namespace server.BackgroundServices;

/// <summary>
/// Serviço que monitora assinaturas periodicamente (a cada 6 horas).
/// 
/// Responsabilidades:
/// 1. Reconciliar cobranças pendentes
/// 2. Enviar avisos de renovação um dia antes do vencimento
/// 3. Renovar assinaturas com cartão salvo no vencimento
/// 4. Expirar assinaturas cujo pagamento de renovação não foi identificado
/// </summary>
public class SubscriptionMonitorBackgroundService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SubscriptionMonitorBackgroundService> _logger;
    private Timer? _timer;
    private string _TraceId = string.Empty;
    private readonly List<string> _tags = new() { "SubscriptionMonitor", "BackgroundService" };

    public SubscriptionMonitorBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<SubscriptionMonitorBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SubscriptionMonitor iniciado. Checagem a cada 6 horas. {TraceId} {_tags_}", _TraceId, _tags);
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
        _TraceId = Guid.NewGuid().ToString();
        try
        {
            _logger.LogInformation("Ciclo do SubscriptionMonitor iniciado. TraceId={TraceId} {_tags_}", _TraceId, _tags);
            using var scope = _serviceProvider.CreateScope();
            var billing = scope.ServiceProvider.GetRequiredService<SubscriptionBillingService>();

            await billing.ProcessPendingChargesAsync();
            await billing.ProcessScheduledCancellationsAsync();
            await billing.ProcessDueTomorrowRenewalNotificationsAsync();
            await billing.ProcessDueCardRenewalsAsync();
            await billing.ExpireUnpaidRenewalsAsync();
            _logger.LogInformation("Ciclo do SubscriptionMonitor concluido. TraceId={TraceId} {_tags_}", _TraceId, _tags);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no SubscriptionMonitor. TraceId={TraceId} {_tags_}", _TraceId, _tags);
        }
    }
}
