using Microsoft.EntityFrameworkCore;

namespace server.Domain;

public static class SubscriptionFeatureAccess
{
    public static Plan? GetActivePlan(Context context, Guid userId)
    {
        var planId = context.subscriptions
            .AsNoTracking()
            .Where(subscription => subscription.user_id == userId && subscription.status == SubscriptionStatus.Active)
            .OrderByDescending(subscription => subscription.created_at)
            .Select(subscription => subscription.plan_id)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(planId))
            return null;

        return Plans.GetById(planId);
    }

    public static bool CanUseRecurringInvestments(Context context, Guid userId) =>
        GetActivePlan(context, userId)?.recurring_investments == true;
}
