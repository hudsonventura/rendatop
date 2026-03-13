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

        return NoContent();
    }

}
