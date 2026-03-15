using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Domain;
using server.RequestObjects;
using server.Utils;

namespace server.Controllers;

/// <summary>
/// Conjunto de endpoints de controle de investimentos
/// </summary>
[ApiController]
public class InvestmentsController : AuthenticatedController
{
    Context _context;
    ILogger _logger;
    INotification _notify;

    public InvestmentsController(ILogger<InvestmentsController> logger, IHttpContextAccessor httpContextAccessor, IDbContextFactory<Context> contextFactory, INotification notify) : base(httpContextAccessor)
    {
        _context = contextFactory.CreateDbContext();
        _logger = logger;

        _logger.LogInformation("Apenas um teste");

        _notify = notify;
    }

    private Investment GetInvestmentByID(Guid id)
    {
        return _context.investments
                            .Include(x => x.bank)
                            .Where(x => x.owner.id == _user.id && x.id == id)
                            .FirstOrDefault();
    }

    private static List<Calculated> BuildTableCalculated(Investment investment)
    {
        if (investment.archived)
        {
            return (investment.calculated ?? new List<Calculated>())
                .Select(CloneCalculated)
                .ToList();
        }

        var redeemedTotal = investment.redemptions?.Sum(r => r.value) ?? 0m;
        var baseCalculated = investment.calculated ?? new List<Calculated>();

        if (redeemedTotal <= 0m)
        {
            return baseCalculated
                .Select(CloneCalculated)
                .ToList();
        }

        return baseCalculated
            .Select(calc => ApplyRedemptionDisplay(calc, investment.value, redeemedTotal))
            .ToList();
    }

    private static decimal GetDisplayRedemptionRatio(decimal redeemedTotal, decimal valueLiq)
    {
        if (redeemedTotal <= 0m || valueLiq <= 0m)
            return 0m;

        return Math.Min(1m, redeemedTotal / valueLiq);
    }

    private static Calculated ApplyRedemptionDisplay(Calculated calc, decimal principal, decimal redeemedTotal)
    {
        if (calc.value_liq <= 0m)
            return CloneCalculated(calc);

        var ratio = GetDisplayRedemptionRatio(redeemedTotal, calc.value_liq);

        var adjustedPrincipal = principal - (principal * ratio);
        var adjustedIofValue = calc.IOF_value - (calc.IOF_value * ratio);
        var adjustedIrValue = calc.IR_value - (calc.IR_value * ratio);
        var adjustedProfitLiq = calc.profit_liq - (calc.profit_liq * ratio);
        var adjustedProfitBrute = calc.profit_brute - (calc.profit_brute * ratio);
        var adjustedValueBrute = calc.value_brute - (calc.value_brute * ratio);

        return new Calculated
        {
            effective_index_percent_brute = calc.effective_index_percent_brute,
            profit_brute = Math.Max(0m, adjustedProfitBrute),
            value_brute = Math.Max(0m, adjustedValueBrute),
            IOF = calc.IOF,
            IOF_value = Math.Max(0m, adjustedIofValue),
            IR = calc.IR,
            IR_value = Math.Max(0m, adjustedIrValue),
            profit_liq = Math.Max(0m, adjustedProfitLiq),
            value_liq = Math.Max(0m, adjustedPrincipal + adjustedProfitLiq)
        };
    }

    private static Calculated CloneCalculated(Calculated calc)
    {
        return new Calculated
        {
            effective_index_percent_brute = calc.effective_index_percent_brute,
            profit_brute = calc.profit_brute,
            value_brute = calc.value_brute,
            IOF = calc.IOF,
            IOF_value = calc.IOF_value,
            IR = calc.IR,
            IR_value = calc.IR_value,
            profit_liq = calc.profit_liq,
            value_liq = calc.value_liq
        };
    }

    private void ArchiveIfFullyRedeemed(Investment investment)
    {
        if (investment == null)
            return;

        var calcType = typeof(ICalculator).Assembly.GetType(
            $"server.Domain.Calculator_{investment.index}"
        );

        if (calcType == null)
            throw new ExpectedException($"Tipo de calculo nao encontrado: Calculator_{investment.index}");

        var calculator = (ICalculator)Activator.CreateInstance(calcType, _context)!;
        var currentCalculated = calculator.Calculate(investment.ToRequest()).FirstOrDefault();

        if (currentCalculated == null)
            return;

        var redeemedTotal = _context.redemptions
            .Where(r => r.investment.id == investment.id)
            .Sum(r => (decimal?)r.value) ?? 0m;

        if (currentCalculated.value_liq - redeemedTotal <= 0m)
        {
            investment.archived = true;
            _context.investments.Update(investment);
        }
    }

    /// <summary>
	/// Lista todos os investimentos do usuário
	/// </summary>
	/// <returns>Lista todos os investimentos do usuário</returns>
	[ProducesResponseType(typeof(List<Error>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [HttpGet("Investments")]
    public List<Investment> Get()
    {
        List<Investment> investments = _context.investments
                                                .AsNoTracking()
                                                .Include(x => x.owner)
                                                .Include(x => x.bank)
                                                .Include(x => x.redemptions)
                                                .Where(x => x.owner.id == _user.id)
                                                .ToList();
        foreach (var invest in investments)
        {
            var calcType = typeof(ICalculator).Assembly.GetType(
                $"server.Domain.Calculator_{invest.index.ToString()}"
            );

            if (calcType == null)
            {
                throw new ExpectedException($"Tipo de calculo nao encontrado: Calculator_{invest.index}");
            }

            var calc = (ICalculator)Activator.CreateInstance(calcType, _context)!;

            invest.calculated = calc.Calculate(invest.ToRequest());
            invest.table_calculated = BuildTableCalculated(invest);
            if (invest.archived)
            {
                invest.table_value = invest.value;
                continue;
            }

            var redeemedTotal = invest.redemptions?.Sum(r => r.value) ?? 0m;
            var currentValueLiq = invest.calculated.FirstOrDefault()?.value_liq ?? 0m;
            var currentRatio = GetDisplayRedemptionRatio(redeemedTotal, currentValueLiq);
            invest.table_value = Math.Max(0m, invest.value - (invest.value * currentRatio));
        }


        return investments;
    }




    /// <summary>
    /// Lista todos os investimentos do usuário
    /// </summary>
    /// <returns>Lista todos os investimentos do usuário</returns>
    [ProducesResponseType(typeof(List<Error>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [HttpGet("Investments/{id}")]
    public Investment Get(Guid id)
        => GetInvestmentByID(id);


    /// <summary>
	/// Adiciona um novo investimento
	/// </summary>
	/// <returns>Retono com o id do investimento</returns>
	[ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [HttpPost("Investments")]
    public Guid Insert([FromBody] InvestmentRequest request)
    {
        var bank = _context.banks.FirstOrDefault(b => b.Code == (ushort)request.bank_code)
            ?? throw new ExpectedException($"Banco com código {request.bank_code} não encontrado.");

        Investment investment = new Investment(request, _user, bank);
        _context.Entry(investment.owner).State = EntityState.Unchanged;
        _context.Entry(investment.bank).State = EntityState.Unchanged;
        _context.investments.Add(investment);
        _context.SaveChanges();

        return investment.id;
    }



    /// <summary>
    /// Edita um investimento já adicionado anteiormente
    /// </summary>
    /// <returns>Retono vazio</returns>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [HttpPatch("Investments/{id}")]
    public IActionResult Update(Guid id, [FromBody] InvestmentRequest request)
    {
        try
        {
            var bank = _context.banks.FirstOrDefault(b => b.Code == (ushort)request.bank_code)
                ?? throw new ExpectedException($"Banco com código {request.bank_code} não encontrado.");

            Investment investment = GetInvestmentByID(id);
            investment.Update(request, bank);
            _context.investments.Update(investment);
            _context.SaveChanges();
            return Ok();
        }
        catch (Exception e)
        {
            throw new ExpectedException(e.Message);
        }
    }

    /// <summary>
    /// Arquiva ou desarquiva um investimento.
    /// </summary>
    /// <returns>Retorno vazio</returns>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [HttpPatch("Investments/{id}/archive")]
    public IActionResult Archive(Guid id, [FromBody] ArchiveInvestmentRequest request)
    {
        var investment = GetInvestmentByID(id);

        if (investment == null)
            throw new ExpectedException("Investimento não encontrado.");

        if (request.archived && (!investment.due_date.HasValue || investment.due_date.Value.Date > DateTime.UtcNow.Date))
            throw new ExpectedException("Somente investimentos vencidos podem ser arquivados.");

        investment.archived = request.archived;
        _context.investments.Update(investment);
        _context.SaveChanges();

        return NoContent();
    }


    /// <summary>
    /// Remove um investimento
    /// </summary>
    /// <returns>Retono vazio</returns>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [HttpDelete("Investments/{id}")]
    public void Delete(Guid id)
    {
        var investment = GetInvestmentByID(id);

        if (investment == null)
            throw new ExpectedException("Investimento não encontrado ou já removido");


        _context.investments.Remove(investment);
        _context.SaveChanges();

    }


    /// <summary>
    /// Resgata um investimento parcial ou totalmente
    /// </summary>
    /// <returns>Retono vazio</returns>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [HttpPut("Investments/{id}")]
    public Investment Redeem(Guid id, [FromBody] RedemptionRequest request)
    {
        var invest = _context.investments.Where(x => x.id == id && x.owner.id == _user.id && x.id == id).FirstOrDefault();

        Redemption redemption = new Redemption(invest, request);

        _context.redemptions.Add(redemption);
        _context.SaveChanges();
        ArchiveIfFullyRedeemed(invest);
        _context.SaveChanges();

        return invest;
    }

    /// <summary>
    /// Edita um resgate já registrado
    /// </summary>
    /// <returns>Retorno vazio</returns>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [HttpPatch("Redemptions/{id}")]
    public IActionResult UpdateRedemption(Guid id, [FromBody] RedemptionRequest request)
    {
        var redemption = _context.redemptions
            .Include(r => r.investment)
            .ThenInclude(i => i.owner)
            .FirstOrDefault(r => r.id == id && r.investment.owner.id == _user.id);

        if (redemption == null)
            throw new ExpectedException("Resgate não encontrado.");

        redemption.title = request.title;
        redemption.value = request.value;
        redemption.date = DateTime.SpecifyKind(request.date, DateTimeKind.Utc);

        _context.redemptions.Update(redemption);
        _context.SaveChanges();
        ArchiveIfFullyRedeemed(redemption.investment);
        _context.SaveChanges();

        return NoContent();
    }

    /// <summary>
    /// Remove um resgate já registrado
    /// </summary>
    /// <returns>Retorno vazio</returns>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [HttpDelete("Redemptions/{id}")]
    public IActionResult DeleteRedemption(Guid id)
    {
        var redemption = _context.redemptions
            .Include(r => r.investment)
            .ThenInclude(i => i.owner)
            .FirstOrDefault(r => r.id == id && r.investment.owner.id == _user.id);

        if (redemption == null)
            throw new ExpectedException("Resgate não encontrado.");

        _context.redemptions.Remove(redemption);
        _context.SaveChanges();

        return NoContent();
    }

}
