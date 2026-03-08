using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Domain;

namespace server.Controllers;

/// <summary>
/// Endpoints relacionados a bancos
/// </summary>
[ApiController]
[Route("[controller]")]
public class Banks : AuthenticatedController
{
    Context _context;

    public Banks(IHttpContextAccessor httpContextAccessor, Context context) : base(httpContextAccessor)
    {
        _context = context;
    }

    /// <summary>
    /// Lista todos os bancos disponíveis (para uso em selectbox no frontend)
    /// </summary>
    /// <returns>Lista de bancos ativos ordenados por nome</returns>
    [ProducesResponseType(typeof(List<Bank>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [HttpGet]
    public List<Bank> Get()
    {
        return _context.banks
            .AsNoTracking()
            .Where(b => b.Active)
            .OrderBy(b => b.Name)
            .ToList();
    }
}

