namespace server.Domain;

public static class SubscriptionTrials
{
    public const int DefaultDurationDays = 30;

    public static Plan GetHighestPricedPlan() =>
        Plans.All
            .Where(plan => plan.price > 0)
            .OrderByDescending(plan => plan.price)
            .First();

    public static Subscription Create(
        Guid userId,
        Plan plan,
        DateTime now,
        bool showWelcome = false)
    {
        return new Subscription
        {
            user_id = userId,
            plan_id = plan.id,
            status = SubscriptionStatus.Active,
            payment_method = "trial",
            current_period_start = now,
            current_period_end = now.AddDays(DefaultDurationDays),
            cancel_at_period_end = true,
            cancellation_requested_at = now,
            created_at = now,
            updated_at = now,
            trial_welcome_pending = showWelcome
        };
    }
}
