using Microsoft.EntityFrameworkCore;
using server.Domain;
using server.Utils;

namespace server.BackgroundServices;

public class RecurringInvestmentsBackgroundService : BackgroundService
{
    private readonly ILogger<RecurringInvestmentsBackgroundService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    private readonly List<string> _tags = new() { "RecurringInvestments", "BackgroundService" };

    public RecurringInvestmentsBackgroundService(
        ILogger<RecurringInvestmentsBackgroundService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Serviço de investimentos recorrentes iniciado. Verificação a cada 1 minuto, com geração diária às 06:00 UTC.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await GenerateRecurringInvestments(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao processar investimentos recorrentes.");
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

    private async Task GenerateRecurringInvestments(CancellationToken stoppingToken)
    {
        using var activity = TraceContext.StartActivity("background.recurring-investments");
        var traceId = TraceContext.GetTraceId();
        var nowUtc = DateTime.UtcNow;
        if (nowUtc.Hour < 6)
            return;

        var today = DateOnly.FromDateTime(nowUtc);

        using var scope = _scopeFactory.CreateScope();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<Context>>();
        await using var context = await contextFactory.CreateDbContextAsync(stoppingToken);

        var recurringInvestments = await context.recurring_investments
            .Include(item => item.owner)
            .Include(item => item.bank)
            .Where(item => item.active)
            .ToListAsync(stoppingToken);

        _logger.LogInformation("Processando investimentos recorrentes. TraceId={TraceId} Count={Count} Date={Date} {_tags_}", traceId, recurringInvestments.Count, today, _tags);

        foreach (var recurringInvestment in recurringInvestments)
        {
            if (!SubscriptionFeatureAccess.CanUseRecurringInvestments(context, recurringInvestment.owner_id))
                continue;

            if (!recurringInvestment.MatchesDate(today))
                continue;

            if (recurringInvestment.last_generated_at.HasValue &&
                DateOnly.FromDateTime(recurringInvestment.last_generated_at.Value) == today)
            {
                continue;
            }

            var investmentRequest = recurringInvestment.ToInvestmentRequest(today);
            var investment = new Investment(investmentRequest, recurringInvestment.owner, recurringInvestment.bank);
            context.Entry(investment.owner).State = EntityState.Unchanged;
            context.Entry(investment.bank).State = EntityState.Unchanged;
            context.investments.Add(investment);

            recurringInvestment.last_generated_at = nowUtc;
            recurringInvestment.updated_at = nowUtc;

            _logger.LogInformation(
                "Investimento recorrente gerado. TraceId={TraceId} UserId={UserId} RecurringId={RecurringId} Title={Title} {_tags_}",
                traceId,
                recurringInvestment.owner_id,
                recurringInvestment.id,
                recurringInvestment.title,
                _tags);
        }

        await context.SaveChangesAsync(stoppingToken);
        _logger.LogInformation("Processamento de investimentos recorrentes concluido. TraceId={TraceId} {_tags_}", traceId, _tags);
    }
}
