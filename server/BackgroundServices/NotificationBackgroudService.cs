
using server.Domain;
using server.Utils;

namespace server.BackgroundServices;

public class NotificationBackgroudService : IHostedService
{
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Task task = Task.Run(() => Run());
    }

    ILogger _logger;
    Context _context;
    INotification _notification;
    int dias_distantes = 1000;


    public NotificationBackgroudService(ILogger logger, Context context, INotification notification)
    {
        _logger = logger;
        _context = context;
        _notification = notification;
    }

    public Task Run(){
        while (true)
        {
            var now = DateTime.Now;
            var target = new DateTime(now.Year, now.Month, now.Day, 6, 0, 0);
            if (now > target)
            {
                target = target.AddDays(1);
            }
            var delay = target - now;
            _logger.LogInformation("Esperando {delay} horas para verificar notificações a serem enviadas", delay.TotalHours);
            Task.Delay(delay, CancellationToken.None).Wait();

            _logger.LogInformation("Verificando notificações a serem enviadas");
            
            var investiments = _context.investments
                                        .Where(i => i.due_date <= DateTime.Now.AddDays(7).ToUniversalTime())
                                        .ToList();

            NotificarDiasDistantes(investiments);

            var investimentsHJ = _context.investments
                                        .Where(i => i.due_date <= DateTime.Now.AddDays(1).ToUniversalTime())
                                        .ToList();

            NotificarDiasHoje(investimentsHJ);
        }
    }

    private void NotificarDiasHoje(List<Investment> investiments)
    {
        foreach (var item in investiments)
        {
            var calc = new Calculator_CDI(_context);

			item.calculated = calc.Calculate(item.ToRequest());

            _notification.Notify(
                "RESGATE DE INVESTIMENTO HOJE!!!", 
                $"{item.title}<br>Valor: R$ {item.value:N2}<br>Rend. Líq.: R$ {item.calculated[1].value_liq:N2}<br>Banco: {item.bank}<br>Vencimento: {((DateTime)item.due_date).ToString("dd/MM/yyyy")}");
        }
    }

    private void NotificarDiasDistantes(List<Investment> investiments)
    {
        foreach (var item in investiments)
        {
            var calc = new Calculator_CDI(_context);

			item.calculated = calc.Calculate(item.ToRequest());

            _notification.Notify(
                "Investimento próximo do vencimento", 
                $"{item.title}<br>Valor: R$ {item.value:N2}<br>Rend. Líq.: R$ {item.calculated[1].value_liq:N2}<br>Banco: {item.bank}<br>Vencimento: {((DateTime)item.due_date).ToString("dd/MM/yyyy")}");
        }
    }
    


    

    
}
