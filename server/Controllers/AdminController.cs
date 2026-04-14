using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Domain;

namespace server.Controllers;

[ApiController]
public class AdminController : AuthenticatedController
{
    private readonly Context _context;

    public AdminController(
        IHttpContextAccessor httpContextAccessor,
        IDbContextFactory<Context> contextFactory) : base(httpContextAccessor)
    {
        _context = contextFactory.CreateDbContext();
    }

    [HttpGet("admin/stats")]
    [ProducesResponseType(typeof(AdminStatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status403Forbidden)]
    public ActionResult<AdminStatsResponse> GetStats()
    {
        EnsureAdmin();

        var users = _context.users
            .AsNoTracking()
            .Select(user => new
            {
                user.id,
                user.auth_provider
            })
            .ToList();

        var activeSubscriptions = _context.subscriptions
            .AsNoTracking()
            .Where(subscription => subscription.status == SubscriptionStatus.Active)
            .Select(subscription => new
            {
                subscription.user_id,
                subscription.plan_id,
                subscription.created_at
            })
            .ToList();

        var latestActivePlanByUser = activeSubscriptions
            .GroupBy(subscription => subscription.user_id)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(subscription => subscription.created_at)
                    .Select(subscription => subscription.plan_id)
                    .FirstOrDefault() ?? "free");

        var planCounts = users
            .Select(user => latestActivePlanByUser.TryGetValue(user.id, out var planId) ? planId : "free")
            .GroupBy(planId => planId)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        var knownPlanIds = new HashSet<string>(Plans.All.Select(plan => plan.id), StringComparer.OrdinalIgnoreCase);

        var usersByPlan = Plans.All
            .Select(plan => new AdminPlanCountResponse(
                plan.id,
                plan.name,
                planCounts.GetValueOrDefault(plan.id, 0)))
            .ToList();

        usersByPlan.AddRange(planCounts
            .Where(item => !knownPlanIds.Contains(item.Key))
            .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .Select(item => new AdminPlanCountResponse(item.Key, item.Key, item.Value)));

        var visitsByOrigin = _context.landing_visits
            .AsNoTracking()
            .GroupBy(visit => visit.visit)
            .Select(group => new
            {
                visit = group.Key,
                visits_count = group.Count()
            })
            .ToList()
            .Select(item => new AdminVisitOriginCountResponse(
                item.visit,
                item.visits_count))
            .OrderByDescending(item => item.visits_count)
            .ThenBy(item => item.visit, StringComparer.Ordinal)
            .ToList();

        var response = new AdminStatsResponse(
            users.Count,
            usersByPlan,
            new AdminAuthProviderCountsResponse(
                users.Count(user => user.auth_provider == AuthProvider.Password),
                users.Count(user => user.auth_provider == AuthProvider.Google),
                users.Count(user => user.auth_provider == AuthProvider.Microsoft)),
            visitsByOrigin);

        return Ok(response);
    }
}

public record AdminStatsResponse(
    int total_users,
    IReadOnlyList<AdminPlanCountResponse> users_by_plan,
    AdminAuthProviderCountsResponse auth_provider_counts,
    IReadOnlyList<AdminVisitOriginCountResponse> visits_by_origin
);

public record AdminPlanCountResponse(
    string plan_id,
    string plan_name,
    int users_count
);

public record AdminAuthProviderCountsResponse(
    int without_sso,
    int google,
    int microsoft
);

public record AdminVisitOriginCountResponse(
    string visit,
    int visits_count
);
