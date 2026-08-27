namespace SemBroncaAI.Garage.Application.Features.Subscriptions;

public enum BillingCycle
{
    Monthly,
    Annual
}

public sealed record CreateCheckoutCommand(BillingCycle Cycle);
public sealed record BillingRedirectResponse(string Url);

public interface IOwnerBillingService
{
    Task<BillingRedirectResponse> CreateCheckoutAsync(BillingCycle cycle, CancellationToken cancellationToken);
    Task<BillingRedirectResponse> CreatePortalAsync(CancellationToken cancellationToken);
}

public interface IBillingWebhookProcessor
{
    Task ProcessAsync(string payload, string signature, CancellationToken cancellationToken);
}
