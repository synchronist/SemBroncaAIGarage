namespace SemBroncaAI.Garage.Domain.Entities.Garage;

public static class SubscriptionOperationalPolicy
{
    public static readonly TimeSpan PastDueGracePeriod = TimeSpan.FromDays(3);

    public static bool? RequiredGarageActive(SubscriptionStatus status) => status switch
    {
        SubscriptionStatus.Trial or SubscriptionStatus.Active => true,
        SubscriptionStatus.Suspended or SubscriptionStatus.Cancelled => false,
        SubscriptionStatus.PastDue => null,
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static bool CanWrite(SubscriptionStatus status) => status is
        SubscriptionStatus.Trial or SubscriptionStatus.Active or SubscriptionStatus.PastDue;
}
