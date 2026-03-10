
using server.RequestObjects;

namespace server.Domain;

public class Calculator
{
    private Context _context;

    public Calculator(Context context)
    {
        _context = context;
    }

    protected decimal GetIR(bool taxes, DateTime start, DateTime? finish) => (taxes) ? new ImpostoRenda().GetTax(start, (DateTime)finish) / 100 : 0;

    protected decimal GetIOF(DateTime start, DateTime? finish) => new IOF().GetTax(start, (DateTime)finish) / 100;

    protected decimal GetSelic(DateTime start, DateTime? finish)
    {
        // Try to get the average Selic rate for the investment period
        decimal? avg = _context.selics
            .Where(x => x.date > DateOnly.FromDateTime(start.AddDays(1)) && x.date < DateOnly.FromDateTime((DateTime)finish))
            .Average(x => (decimal?)x.value);

        if (avg is not null)
            return avg.Value / 100;

        // Fallback: if no data exists for the date range (e.g. DB still seeding),
        // use the most recent known Selic rate so results are non-zero and meaningful
        decimal? latest = _context.selics
            .OrderByDescending(x => x.date)
            .Select(x => (decimal?)x.value)
            .FirstOrDefault();

        return (latest ?? 0m) / 100;
    }

    protected decimal GetIpca(DateTime start, DateTime? finish)
    {
        // IPCA records are monthly percentages. Convert average monthly IPCA to an annualized rate.
        decimal? avgMonthly = _context.ipcas
            .Where(x => x.date > DateOnly.FromDateTime(start.AddDays(1)) && x.date < DateOnly.FromDateTime((DateTime)finish))
            .Average(x => (decimal?)x.value);

        if (avgMonthly is null)
        {
            avgMonthly = _context.ipcas
                .OrderByDescending(x => x.date)
                .Select(x => (decimal?)x.value)
                .FirstOrDefault();
        }


        if (avgMonthly is null)
            return 0m;

        decimal monthlyRate = avgMonthly.Value / 100m;
        return (decimal)Math.Pow((double)(1m + monthlyRate), 12) - 1m;
    }

    protected int GetDays(DateTime start, DateTime finish) => (finish - start).Days;

    /// <summary>
    /// Devolve 2 calculos. O primeiro considerando a data atual e o segundo considerando a data estimada. Caso não tenha uma data estimada, usa apenas a data atual
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public List<Calculated> Calculate(InvestmentRequest request)
    {
        var now = DateTime.UtcNow;

        ///se não possuir data estimada de venda, usa a data atual
        if (request.date_expected_sell is null)
        {
            return new List<Calculated>(){
                Generate(request, now)
            };
        }

        return new List<Calculated>(){
            Generate(request, now),
            Generate(request, (DateTime) request.date_expected_sell)
        };
    }

    protected virtual Calculated Generate(InvestmentRequest request, DateTime sell)
    {
        return ((ICalculator)this).Generate(request, sell);
    }
}
