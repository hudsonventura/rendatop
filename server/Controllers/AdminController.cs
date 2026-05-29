using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Domain;
using server.Utils;

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

    [HttpGet("admin/users")]
    [ProducesResponseType(typeof(AdminUsersPageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status403Forbidden)]
    public ActionResult<AdminUsersPageResponse> GetUsers(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20)
    {
        EnsureAdmin();

        page = Math.Max(1, page);
        page_size = Math.Clamp(page_size, 5, 100);
        var normalizedSearch = (search ?? string.Empty).Trim().ToLower();

        var query = _context.users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(user =>
                user.name.ToLower().Contains(normalizedSearch) ||
                user.email.ToLower().Contains(normalizedSearch));
        }

        var total = query.Count();
        var users = query
            .OrderBy(user => user.name)
            .ThenBy(user => user.email)
            .Skip((page - 1) * page_size)
            .Take(page_size)
            .Select(user => new
            {
                user.id,
                user.name,
                user.email,
                user.user_type,
                user.auth_provider,
                user.email_verified
            })
            .ToList();

        var userIds = users.Select(user => user.id).ToHashSet();
        var activeSubscriptions = _context.subscriptions
            .AsNoTracking()
            .Where(subscription =>
                userIds.Contains(subscription.user_id) &&
                subscription.status == SubscriptionStatus.Active)
            .ToList();

        var latestActiveByUser = activeSubscriptions
            .GroupBy(subscription => subscription.user_id)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(subscription => subscription.created_at)
                    .First());

        var items = users
            .Select(user =>
            {
                latestActiveByUser.TryGetValue(user.id, out var subscription);
                var planId = subscription?.plan_id ?? "free";
                var plan = Plans.GetById(planId);

                return new AdminUserResponse(
                    user.id,
                    user.name,
                    user.email,
                    user.user_type,
                    user.auth_provider,
                    user.email_verified,
                    planId,
                    plan?.name ?? planId,
                    subscription?.current_period_end,
                    subscription?.payment_method);
            })
            .ToList();

        return Ok(new AdminUsersPageResponse(items, page, page_size, total));
    }

    [HttpPost("admin/users/{userId:guid}/trial")]
    [ProducesResponseType(typeof(AdminUserTrialResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status403Forbidden)]
    public ActionResult<AdminUserTrialResponse> GrantPaidPlanTrial(Guid userId, [FromBody] AdminGrantTrialRequest request)
    {
        EnsureAdmin();

        var plan = Plans.GetById((request.plan_id ?? string.Empty).Trim().ToLowerInvariant())
            ?? throw new ExpectedException("Plano inválido.");

        if (plan.price <= 0)
            throw new ExpectedException("Degustação só pode ser liberada para planos pagos.");

        var userExists = _context.users.AsNoTracking().Any(user => user.id == userId);
        if (!userExists)
            throw new ExpectedException("Usuário não encontrado.", System.Net.HttpStatusCode.NotFound);

        var now = DateTime.UtcNow;
        var subscription = new Subscription
        {
            user_id = userId,
            plan_id = plan.id,
            status = SubscriptionStatus.Active,
            payment_method = "trial",
            current_period_start = now,
            current_period_end = now.AddDays(30),
            cancel_at_period_end = true,
            cancellation_requested_at = now,
            created_at = now,
            updated_at = now
        };

        _context.subscriptions.Add(subscription);
        _context.SaveChanges();

        return Ok(new AdminUserTrialResponse(
            subscription.id,
            subscription.user_id,
            subscription.plan_id,
            plan.name,
            subscription.current_period_start,
            subscription.current_period_end));
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

public record AdminUsersPageResponse(
    IReadOnlyList<AdminUserResponse> items,
    int page,
    int page_size,
    int total
);

public record AdminUserResponse(
    Guid id,
    string name,
    string email,
    UserType user_type,
    AuthProvider auth_provider,
    bool email_verified,
    string active_plan_id,
    string active_plan_name,
    DateTime? active_plan_period_end,
    string? active_payment_method
);

public record AdminGrantTrialRequest(string plan_id);

public record AdminUserTrialResponse(
    Guid subscription_id,
    Guid user_id,
    string plan_id,
    string plan_name,
    DateTime current_period_start,
    DateTime current_period_end
);
