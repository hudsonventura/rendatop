using Lib.Net.Http.WebPush;
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
    private readonly IBrowserPushNotification _browserPush;
    private readonly string? _clientBaseUrl;

    private readonly List<string> _tags = new() { "DueTomorrowNotification", "BackgroundService" };
    private string _TraceId = string.Empty;

    public DueTomorrowNotificationBackgroundService(
        ILogger<DueTomorrowNotificationBackgroundService> logger,
        IServiceScopeFactory scopeFactory,
        INotification telegram,
        IWhatsAppNotification whatsApp,
        IEmailNotification email,
        IBrowserPushNotification browserPush)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _telegram = telegram;
        _whatsApp = whatsApp;
        _email = email;
        _browserPush = browserPush;
        _clientBaseUrl = Environment.GetEnvironmentVariable("BASE_URL_CLIENT");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _TraceId = Guid.NewGuid().ToString();
        _logger.LogInformation("Serviço de notificações de vencimento iniciado. Verificação a cada 1 minuto. {TraceId} {_tags_}", _TraceId, _tags);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (DateTime.UtcNow.Hour >= 9 && DateTime.UtcNow.Hour < 17) // Executa a verificação diariamente às 8h00 UTC (5h00 no horário de Brasília)
                {
                    await NotifyDueTomorrow(stoppingToken);
                }
                else
                {
                    _logger.LogInformation("Fora do horário de envio de notificações. Verificação adiada. {TraceId} {_tags_}", _TraceId, _tags);
                }
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao processar notificações de vencimento para amanhã. {TraceId} {_tags_}", _TraceId, _tags);
            }

            try
            {
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private async Task NotifyDueTomorrow(CancellationToken stoppingToken)
    {
        using var activity = TraceContext.StartActivity("background.due-tomorrow-notifications");
        var traceId = TraceContext.GetTraceId();
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
            _logger.LogInformation("Nenhum investimento com vencimento para amanhã. TraceId={TraceId} Date={Date} {_tags_}", traceId, tomorrowLocal.ToString("dd/MM/yyyy"), _tags);
            return;
        }

        _logger.LogInformation("Processando notificacoes de vencimento. TraceId={TraceId} Count={Count} Date={Date} {_tags_}", traceId, investments.Count, tomorrowLocal.ToString("dd/MM/yyyy"), _tags);

        var activeWhatsAppUserIds = (await context.subscriptions
                .AsNoTracking()
                .Where(s => s.status == SubscriptionStatus.Active)
                .ToListAsync(stoppingToken))
            .Where(s => Plans.GetById(s.plan_id)?.whatsapp_notifications == true)
            .Select(s => s.user_id)
            .ToHashSet();

        var browserSubscriptionsByUser = await context.browser_push_subscriptions
            .AsNoTracking()
            .GroupBy(x => x.user_id)
            .ToDictionaryAsync(
                group => group.Key,
                group => group.ToList(),
                stoppingToken);

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
            var notificationSummary = BuildNotificationSummary(context, investment);
            var message = BuildMessage(user, investment, notificationSummary);
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

            if (user.notify_telegram && !string.IsNullOrWhiteSpace(user.telegram_chat_id))
            {
                try
                {
                    await _telegram.Notify(title, BuildTelegramMessage(user, investment, notificationSummary), user.telegram_chat_id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falha ao enviar notificacao Telegram. TraceId={TraceId} Payload={@Payload} {_tags_}", traceId, new { userId = user.id, investmentId = investment.id, title, sourceKey }, _tags);
                }
            }

            if (user.notify_whatsapp && activeWhatsAppUserIds.Contains(user.id) && !string.IsNullOrWhiteSpace(user.phone))
            {
                try
                {
                    await _whatsApp.Notify(user.phone, title, BuildWhatsAppMessage(user, investment, notificationSummary));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falha ao enviar notificacao WhatsApp. TraceId={TraceId} Payload={@Payload} {_tags_}", traceId, new { userId = user.id, investmentId = investment.id, title, sourceKey, user.phone }, _tags);
                }
            }

            if (user.notify_email && !string.IsNullOrWhiteSpace(user.email))
            {
                try
                {
                    var emailMessage = DueTomorrowEmailTemplate.Build(user, investment, notificationSummary, _clientBaseUrl);
                    await _email.Notify(user.email, title, emailMessage, isHtml: true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falha ao enviar notificacao Email. TraceId={TraceId} Payload={@Payload} {_tags_}", traceId, new { userId = user.id, investmentId = investment.id, title, sourceKey, user.email }, _tags);
                }
            }

            if (user.notify_browser &&
                _browserPush.IsConfigured &&
                browserSubscriptionsByUser.TryGetValue(user.id, out var browserSubscriptions))
            {
                var pushMessage = BuildBrowserPushMessage(title, investment, notificationSummary);

                foreach (var browserSubscription in browserSubscriptions)
                {
                    try
                    {
                        await _browserPush.SendAsync(browserSubscription, pushMessage, stoppingToken);
                    }
                    catch (PushServiceClientException ex) when (
                        ex.StatusCode == System.Net.HttpStatusCode.Gone ||
                        ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        var staleSubscription = await context.browser_push_subscriptions
                            .FirstOrDefaultAsync(x => x.endpoint == browserSubscription.endpoint, stoppingToken);

                        if (staleSubscription is not null)
                        {
                            context.browser_push_subscriptions.Remove(staleSubscription);
                        }

                        _logger.LogInformation(
                            ex,
                            "Inscricao Browser Push removida apos erro. TraceId={TraceId} StatusCode={StatusCode} UserId={UserId} Endpoint={Endpoint} {_tags_}",
                            traceId,
                            ex.StatusCode,
                            user.id,
                            browserSubscription.endpoint,
                            _tags);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Falha ao enviar notificacao Browser Push. TraceId={TraceId} Payload={@Payload} {_tags_}", traceId, new { userId = user.id, investmentId = investment.id, title, sourceKey, browserSubscription.endpoint }, _tags);
                    }
                }
            }
        }

        await context.SaveChangesAsync(stoppingToken);

        _logger.LogInformation("Verificacao de vencimentos concluida. TraceId={TraceId} Time={Time} {_tags_}", traceId, DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), _tags);
    }

    internal static DueTomorrowNotificationSummary BuildNotificationSummary(Context context, Investment investment)
    {
        var calcType = typeof(ICalculator).Assembly.GetType(
            $"server.Domain.Calculator_{investment.index}"
        );

        if (calcType is null)
        {
            return new DueTomorrowNotificationSummary(0m, 0m, investment.value);
        }

        var calculator = (ICalculator)Activator.CreateInstance(calcType, context)!;
        var calculated = calculator.Calculate(investment.ToRequest()).LastOrDefault();

        if (calculated is null)
            return new DueTomorrowNotificationSummary(0m, 0m, investment.value);

        return new DueTomorrowNotificationSummary(
            calculated.profit_brute,
            calculated.IR_value,
            calculated.value_liq);
    }

    internal static string BuildMessage(User user, Investment investment, DueTomorrowNotificationSummary summary)
        => BuildMessage(user, investment, summary, DueTomorrowTextStyle.Plain);

    internal static string BuildTelegramMessage(User user, Investment investment, DueTomorrowNotificationSummary summary)
        => BuildMessage(user, investment, summary, DueTomorrowTextStyle.TelegramHtml);

    internal static string BuildWhatsAppMessage(User user, Investment investment, DueTomorrowNotificationSummary summary)
        => BuildMessage(user, investment, summary, DueTomorrowTextStyle.WhatsAppMarkdown);

    internal BrowserPushMessage BuildBrowserPushMessage(string title, Investment investment, DueTomorrowNotificationSummary summary)
    {
        var bankName = investment.bank?.Name ?? "Banco não informado";
        var due = investment.due_date?.ToLocalTime().ToString("dd/MM/yyyy") ?? "-";
        var body =
            $"{investment.title} | {bankName}\n" +
            $"Valor investido: R$ {investment.value:N2}\n" +
            $"Rendimento bruto: R$ {summary.GrossProfit:N2}\n" +
            $"IR: R$ {summary.IncomeTax:N2}\n" +
            $"Valor líquido: R$ {summary.NetValue:N2}\n" +
            $"Vencimento: {due}";

        return new BrowserPushMessage(
            title,
            body,
            BuildNotificationsPageUrl(),
            $"due-tomorrow:{investment.id}"
        );
    }

    private static string BuildMessage(User user, Investment investment, DueTomorrowNotificationSummary summary, DueTomorrowTextStyle textStyle)
    {
        var bankName = investment.bank?.Name ?? "Banco não informado";
        var due = investment.due_date?.ToLocalTime().ToString("dd/MM/yyyy") ?? "-";
        var netValue = textStyle switch
        {
            DueTomorrowTextStyle.TelegramHtml => $"<b>R$ {summary.NetValue:N2}</b>",
            DueTomorrowTextStyle.WhatsAppMarkdown => $"*R$ {summary.NetValue:N2}*",
            _ => $"R$ {summary.NetValue:N2}"
        };

        return
            $"Usuário: {user.name}{Environment.NewLine}" +
            $"Investimento: {investment.title}{Environment.NewLine}" +
            $"Banco: {bankName}{Environment.NewLine}" +
            $"Valor investido: R$ {investment.value:N2}{Environment.NewLine}" +
            $"Rendimento bruto: R$ {summary.GrossProfit:N2}{Environment.NewLine}" +
            $"IR: R$ {summary.IncomeTax:N2}{Environment.NewLine}" +
            $"Valor líquido: {netValue}{Environment.NewLine}" +
            $"Vencimento: {due}{Environment.NewLine}{Environment.NewLine}" +
            "Revise seus resgates no RentaTop.";
    }

    private string? BuildNotificationsPageUrl()
    {
        var normalizedBaseUrl = (_clientBaseUrl ?? string.Empty).Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(normalizedBaseUrl))
            return null;

        return $"{normalizedBaseUrl}/notifications";
    }
}

internal sealed record DueTomorrowNotificationSummary(decimal GrossProfit, decimal IncomeTax, decimal NetValue);
internal enum DueTomorrowTextStyle
{
    Plain,
    TelegramHtml,
    WhatsAppMarkdown
}
