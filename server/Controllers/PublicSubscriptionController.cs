using Microsoft.AspNetCore.Mvc;
using server.Domain;

namespace server.Controllers;

/// <summary>
/// Endpoints públicos para gerenciamento de assinaturas (sem autenticação)
/// </summary>
[ApiController]
[Route("api/public/subscription")]
public class PublicSubscriptionController : ControllerBase
{
    /// <summary>
    /// Lista todos os planos disponíveis (endpoint público)
    /// </summary>
    [HttpGet("plans")]
    [ProducesResponseType(typeof(List<Plan>), StatusCodes.Status200OK)]
    public List<Plan> GetPlans() => Plans.All;
}
