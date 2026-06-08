using Microsoft.EntityFrameworkCore;

namespace server.Domain;

public static class SubscriptionFeatureAccess
{
    public const int FreeMoneyBoxesLimit = 3;
    public const string InvestmentDocumentExtractionFeature = "investment_document_extraction";

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

    public static int GetWalletsLimit(Context context, Guid userId) =>
        GetEffectivePlan(context, userId).wallets_limit;

    public static int GetInvestmentsLimit(Context context, Guid userId) =>
        GetEffectivePlan(context, userId).investments;

    public static int GetActiveInvestmentsCount(Context context, Guid userId) =>
        context.investments.Count(investment => investment.owner.id == userId && !investment.archived);

    public static bool CanCreateInvestments(Context context, Guid userId, int existingCount) =>
        existingCount < GetInvestmentsLimit(context, userId);

    public static bool CanCreateWallets(Context context, Guid userId, int existingCount) =>
        existingCount < GetWalletsLimit(context, userId);

    public static bool CanAccessWallet(Context context, Guid userId, Guid walletId)
    {
        var enabledWalletIds = GetEnabledWalletIds(context, userId);
        return enabledWalletIds.Contains(walletId);
    }

    public static HashSet<Guid> GetEnabledWalletIds(Context context, Guid userId)
    {
        var limit = GetWalletsLimit(context, userId);
        var query = context.wallets
            .AsNoTracking()
            .Where(wallet => wallet.owner_id == userId)
            .OrderBy(wallet => wallet.created_at)
            .ThenBy(wallet => wallet.id)
            .Select(wallet => wallet.id);

        if (limit != int.MaxValue)
            query = query.Take(limit);

        return query.ToHashSet();
    }

    public static bool CanCreateMoneyBoxes(Context context, Guid userId, int existingCount) =>
        existingCount < GetMoneyBoxesLimit(context, userId);

    public static bool CanSelectMoneyBoxes(Context context, Guid userId, int existingCount)
    {
        var plan = GetEffectivePlan(context, userId);
        if (plan.id != "free")
            return true;

        return existingCount <= FreeMoneyBoxesLimit;
    }

    public static int GetAiMonthlyLimit(Context context, Guid userId) =>
        GetEffectivePlan(context, userId).ai_monthly_limit;

    public static int GetAiUsageCountInMonth(
        Context context,
        Guid userId,
        string feature,
        DateTime referenceUtc)
    {
        var monthStart = new DateTime(referenceUtc.Year, referenceUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var nextMonthStart = monthStart.AddMonths(1);

        return context.ai_usages.Count(item =>
            item.user_id == userId &&
            item.feature == feature &&
            item.created_at >= monthStart &&
            item.created_at < nextMonthStart);
    }
}
