using Microsoft.EntityFrameworkCore;
using SemBroncaAI.Garage.Application.Abstractions.Security;
using SemBroncaAI.Garage.Application.Features.Subscriptions;
using SemBroncaAI.Garage.Infrastructure.Persistence;

namespace SemBroncaAI.Garage.Infrastructure.Services;

public sealed class OwnerSubscriptionQuery(
    GarageDbContext context,
    ICurrentGarage currentGarage,
    Microsoft.Extensions.Options.IOptions<StripeBillingOptions> billingOptions)
    : IOwnerSubscriptionQuery
{
    public Task<OwnerSubscriptionResponse?> GetAsync(CancellationToken cancellationToken)
    {
        var garageId = currentGarage.RequireGarageId();
        return context.GarageSubscriptions
            .AsNoTracking()
            .Where(subscription => subscription.GarageId == garageId)
            .Select(subscription => new OwnerSubscriptionResponse(
                subscription.Plan,
                subscription.Status,
                subscription.StartedAt,
                subscription.TrialEndsAt,
                subscription.CurrentPeriodStart,
                subscription.CurrentPeriodEnd,
                billingOptions.Value.Enabled,
                subscription.BillingSubscriptionId != null,
                subscription.CancelAtPeriodEnd))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
