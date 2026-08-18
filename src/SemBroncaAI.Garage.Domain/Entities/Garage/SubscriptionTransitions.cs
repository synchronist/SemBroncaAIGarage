namespace SemBroncaAI.Garage.Domain.Entities.Garage;

public static class SubscriptionTransitions
{
    public static IReadOnlyCollection<SubscriptionStatus> AllowedTargets(SubscriptionStatus current) => current switch
    {
        SubscriptionStatus.Trial => [SubscriptionStatus.Active, SubscriptionStatus.Suspended, SubscriptionStatus.Cancelled],
        SubscriptionStatus.Active => [SubscriptionStatus.PastDue, SubscriptionStatus.Suspended, SubscriptionStatus.Cancelled],
        SubscriptionStatus.PastDue => [SubscriptionStatus.Active, SubscriptionStatus.Suspended, SubscriptionStatus.Cancelled],
        SubscriptionStatus.Suspended => [SubscriptionStatus.Active, SubscriptionStatus.Cancelled],
        SubscriptionStatus.Cancelled => [SubscriptionStatus.Active],
        _ => throw new ArgumentOutOfRangeException(nameof(current))
    };

    public static bool IsAllowed(SubscriptionStatus current, SubscriptionStatus target) =>
        AllowedTargets(current).Contains(target);
}
