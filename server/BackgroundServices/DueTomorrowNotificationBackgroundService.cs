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
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var nextRun = new DateTime(now.Year, now.Month, now.Day, 6, 0, 0, DateTimeKind.Local);
            if (now >= nextRun)
                nextRun = nextRun.AddDays(1);

            var delay = nextRun - now;
            _logger.LogInformation("Notificações de vencimento aguardando próxima execução em {hours:F2} horas.", delay.TotalHours);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            try
            {
                await NotifyDueTomorrow(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao processar notificações de vencimento para amanhã.");
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

        var groups = investments
            .GroupBy(i => i.owner.id)
            .ToList();

        foreach (var group in groups)
        {
            var user = group.First().owner;
            var title = "📈 RentaTop | Vencimentos amanhã";
            var message = BuildMessage(user, group.ToList(), tomorrowLocal);
            context.notifications.Add(new Notification
            {
                id = SnowflakeGuid.NewGuid(),
                user_id = user.id,
                title = title,
                message = message,
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

        _logger.LogInformation("Notificações de vencimento enviadas para {users} usuário(s).", groups.Count);
    }

    private static string BuildMessage(User user, List<Investment> investments, DateTime tomorrowLocal)
    {
        var lines = new List<string>
        {
            $"Usuário: {user.name}",
            $"Data de vencimento: {tomorrowLocal:dd/MM/yyyy}",
            string.Empty,
            "Investimentos:",
        };

        foreach (var investment in investments)
        {
            var bankName = investment.bank?.Name ?? "Banco não informado";
            var due = investment.due_date?.ToLocalTime().ToString("dd/MM/yyyy") ?? tomorrowLocal.ToString("dd/MM/yyyy");
            lines.Add($"- {investment.title} | {bankName} | R$ {investment.value:N2} | Vencimento: {due}");
        }

        lines.Add(string.Empty);
        lines.Add("Revise seus resgates no RentaTop.");
        return string.Join(Environment.NewLine, lines);
    }
}
