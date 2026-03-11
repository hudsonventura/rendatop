using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Domain;
using server.RequestObjects;
using server.Utils;

namespace server.Controllers;

// <summary>
/// Controller para calculo de investimentos
/// </summary>
[ApiController]

public class CalculateController : ControllerBase
{
	Context _context;

    public CalculateController(IDbContextFactory<Context> contextFactory)
    {
		_context = contextFactory.CreateDbContext();
    }

    /// <summary>
	/// Calcula dados sobre um investimento
	/// </summary>
	/// <returns>Aqui é a descrição do que esse troço retorna</returns>
	[ProducesResponseType(typeof(Investment),StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
	[ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
	[HttpGet("/Calculate/")]
	//[AllowAnonymous]
	public List<Calculated> Authenticated(InvestmentRequest request){
		return Calculator(request, DateTime.UtcNow);
	}


	private List<Calculated> Calculator(InvestmentRequest invesment, DateTime finish)
    {
        var calcType = typeof(ICalculator).Assembly.GetType(
            $"server.Domain.Calculator_{invesment.index.ToString()}"
        );

        if (calcType == null)
        {
            throw new ExpectedException("Tipo de calculo nao encontrado");
        }

        var calc = (ICalculator)Activator.CreateInstance(calcType, _context);

        return calc.Calculate(invesment);
    }


}
