using Microsoft.AspNetCore.Mvc;
using server.Domain;

namespace server.Controllers;

/// <summary>
/// Endpoints públicos para gerenciamento de assinaturas (sem autenticação)
/// </summary>
[ApiController]
public class PublicSubscriptionController : ControllerBase
{
    /// <summary>
    /// Lista todos os planos disponíveis (endpoint público)
    /// </summary>
    [HttpGet("/public/subscription/plans")]
    [HttpGet("/api/public/subscription/plans")]
    [ProducesResponseType(typeof(List<PublicPlanResponse>), StatusCodes.Status200OK)]
    public List<PublicPlanResponse> GetPlans() => Plans.All
        .Select(plan => new PublicPlanResponse
        {
            id = plan.id,
            name = plan.name,
            description = plan.description,
            price = plan.price,
            ai_monthly_limit = plan.ai_monthly_limit,
            calendar_ics = plan.calendar_ics,
            stoks = plan.stoks,
            whatsapp_notifications = plan.whatsapp_notifications,
            recurring_investments = plan.recurring_investments,
            money_boxes_limit = plan.money_boxes_limit,
            features = new Dictionary<string, string>(plan.features)
        })
        .ToList();
}

public sealed class PublicPlanResponse
{
    public string id { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
    public decimal price { get; set; }
    public int ai_monthly_limit { get; set; }
    public bool calendar_ics { get; set; }
    public int stoks { get; set; }
    public bool whatsapp_notifications { get; set; }
    public bool recurring_investments { get; set; }
    public int money_boxes_limit { get; set; }
    public Dictionary<string, string> features { get; set; } = new();
}
