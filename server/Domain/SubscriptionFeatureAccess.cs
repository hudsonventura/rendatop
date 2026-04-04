using Microsoft.EntityFrameworkCore;

namespace server.Domain;

public static class SubscriptionFeatureAccess
{
    public const int FreeMoneyBoxesLimit = 3;

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

    public static Plan GetEffectivePlan(Context context, Guid userId) =>
        GetActivePlan(context, userId) ?? Plans.GetById("free")!;

    public static int GetMoneyBoxesLimit(Context context, Guid userId) =>
        GetEffectivePlan(context, userId).money_boxes_limit;

    public static bool CanCreateMoneyBoxes(Context context, Guid userId, int existingCount) =>
        existingCount < GetMoneyBoxesLimit(context, userId);

    public static bool CanSelectMoneyBoxes(Context context, Guid userId, int existingCount)
    {
        var plan = GetEffectivePlan(context, userId);
        if (plan.id != "free")
            return true;

        return existingCount <= FreeMoneyBoxesLimit;
    }
}
