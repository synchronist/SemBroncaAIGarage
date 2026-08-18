namespace SemBroncaAI.Garage.Domain.Entities.Garage;

public static class SubscriptionOperationalPolicy
{
    public static bool? RequiredGarageActive(SubscriptionStatus status) => status switch
    {
        SubscriptionStatus.Trial or SubscriptionStatus.Active => true,
        SubscriptionStatus.Suspended or SubscriptionStatus.Cancelled => false,
        SubscriptionStatus.PastDue => null,
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };
}
