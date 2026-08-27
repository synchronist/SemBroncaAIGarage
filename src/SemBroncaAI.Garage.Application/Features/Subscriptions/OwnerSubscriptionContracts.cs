using SemBroncaAI.Garage.Domain.Entities.Garage;

namespace SemBroncaAI.Garage.Application.Features.Subscriptions;

public sealed record OwnerSubscriptionResponse(
    SubscriptionPlan Plan,
    SubscriptionStatus Status,
    DateTime StartedAt,
    DateTime? TrialEndsAt,
    DateTime? CurrentPeriodStart,
    DateTime? CurrentPeriodEnd,
    bool OnlineBillingEnabled,
    bool CanManageBilling,
    bool CancelAtPeriodEnd);

public interface IOwnerSubscriptionQuery
{
    Task<OwnerSubscriptionResponse?> GetAsync(CancellationToken cancellationToken);
}
