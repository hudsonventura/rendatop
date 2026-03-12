using Microsoft.EntityFrameworkCore;
using server.Domain;
using server.Utils;

namespace server.BackgroundServices;

public class DueTomorrowNotificationBackgroundService : BackgroundService
{
    private readonly ILogger<DueTomorrowNotificationBackgroundService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly INotification _telegram;
    private readonly IWhatsAppNotification _whatsApp;
    private readonly IEmailNotification _email;

    public DueTomorrowNotificationBackgroundService(
        ILogger<DueTomorrowNotificationBackgroundService> logger,
        IServiceScopeFactory scopeFactory,
        INotification telegram,
        IWhatsAppNotification whatsApp,
        IEmailNotification email)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _telegram = telegram;
        _whatsApp = whatsApp;
        _email = email;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Serviço de notificações de vencimento iniciado. Verificação a cada 1 minuto.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await NotifyDueTomorrow(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao processar notificações de vencimento para amanhã.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private async Task NotifyDueTomorrow(CancellationToken stoppingToken)
    {
        var tomorrowLocal = DateTime.Now.Date.AddDays(1);
        var tomorrowStartLocal = new DateTime(
            tomorrowLocal.Year,
            tomorrowLocal.Month,
            tomorrowLocal.Day,
            0, 0, 0,
            DateTimeKind.Local);

        var tomorrowEndLocal = tomorrowStartLocal.AddDays(1);
        var tomorrowStartUtc = tomorrowStartLocal.ToUniversalTime();
        var tomorrowEndUtc = tomorrowEndLocal.ToUniversalTime();

        using var scope = _scopeFactory.CreateScope();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<Context>>();
        await using var context = await contextFactory.CreateDbContextAsync(stoppingToken);

        var investments = await context.investments
            .AsNoTracking()
            .Include(i => i.owner)
            .Include(i => i.bank)
            .Where(i =>
                i.due_date.HasValue &&
                i.due_date.Value >= tomorrowStartUtc &&
                i.due_date.Value < tomorrowEndUtc)
            .OrderBy(i => i.due_date)
            .ThenBy(i => i.title)
            .ToListAsync(stoppingToken);

        if (investments.Count == 0)
        {
            _logger.LogInformation("Nenhum investimento com vencimento para amanhã ({date}).", tomorrowLocal.ToString("dd/MM/yyyy"));
            return;
        }

        foreach (var investment in investments)
        {
            var user = investment.owner;
            var dueLocal = investment.due_date?.ToLocalTime() ?? tomorrowLocal;
            var sourceKey = $"due-tomorrow:{investment.id}:{dueLocal:yyyyMMdd}";

            var alreadyExists = await context.notifications
                .AsNoTracking()
                .AnyAsync(n => n.user_id == user.id && n.source_key == sourceKey, stoppingToken);

            if (alreadyExists)
                continue;

            var title = "📈 RentaTop | Vencimento amanhã";
            var message = BuildMessage(user, investment);
            context.notifications.Add(new Notification
            {
                id = SnowflakeGuid.NewGuid(),
                user_id = user.id,
                title = title,
                message = message,
                source_key = sourceKey,
                is_read = false,
                created_at = DateTime.UtcNow
            });

            if (!user.notify_telegram && !user.notify_whatsapp && !user.notify_email)
                continue;

            if (user.notify_telegram)
            {
                try
                {
                    await _telegram.Notify(title, message);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falha ao enviar notificação Telegram para o usuário {userId}.", user.id);
                }
            }

            if (user.notify_whatsapp && !string.IsNullOrWhiteSpace(user.phone))
            {
                try
                {
                    await _whatsApp.Notify(user.phone, title, message);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falha ao enviar notificação WhatsApp para o usuário {userId}.", user.id);
                }
            }

            if (user.notify_email && !string.IsNullOrWhiteSpace(user.email))
            {
                try
                {
                    await _email.Notify(user.email, title, message);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falha ao enviar notificação Email para o usuário {userId}.", user.id);
                }
            }
        }

        await context.SaveChangesAsync(stoppingToken);

        _logger.LogInformation("Verificação de vencimentos concluída às {time}.", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
    }

    private static string BuildMessage(User user, Investment investment)
    {
        var bankName = investment.bank?.Name ?? "Banco não informado";
        var due = investment.due_date?.ToLocalTime().ToString("dd/MM/yyyy") ?? "-";
        return
            $"Usuário: {user.name}{Environment.NewLine}" +
            $"Investimento: {investment.title}{Environment.NewLine}" +
            $"Banco: {bankName}{Environment.NewLine}" +
            $"Valor investido: R$ {investment.value:N2}{Environment.NewLine}" +
            $"Vencimento: {due}{Environment.NewLine}{Environment.NewLine}" +
            "Revise seus resgates no RentaTop.";
    }
}
