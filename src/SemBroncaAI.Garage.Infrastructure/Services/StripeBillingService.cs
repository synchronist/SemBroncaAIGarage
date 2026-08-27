using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SemBroncaAI.Garage.Application.Abstractions.Security;
using SemBroncaAI.Garage.Application.Features.Subscriptions;
using SemBroncaAI.Garage.Domain.Entities.Garage;
using SemBroncaAI.Garage.Infrastructure.Persistence;
using Stripe;
using Stripe.Checkout;

namespace SemBroncaAI.Garage.Infrastructure.Services;

public sealed class StripeBillingService(
    GarageDbContext context,
    ICurrentGarage currentGarage,
    ICurrentUser currentUser,
    IOptions<StripeBillingOptions> options,
    IConfiguration configuration,
    ILogger<StripeBillingService> logger) : IOwnerBillingService, IBillingWebhookProcessor
{
    private readonly StripeBillingOptions _options = options.Value;

    public async Task<BillingRedirectResponse> CreateCheckoutAsync(
        BillingCycle cycle,
        CancellationToken cancellationToken)
    {
        EnsureEnabled();
        var garageId = currentGarage.RequireGarageId();
        var subscription = await context.GarageSubscriptions
            .SingleAsync(x => x.GarageId == garageId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(subscription.BillingSubscriptionId) &&
            subscription.Status != SubscriptionStatus.Cancelled)
            throw new InvalidOperationException("A oficina já possui uma assinatura gerenciável.");

        var customerId = subscription.BillingCustomerId;
        var client = CreateClient();
        if (string.IsNullOrWhiteSpace(customerId))
        {
            var garage = await context.Garages.AsNoTracking().SingleAsync(x => x.Id == garageId, cancellationToken);
            var user = await context.Users.AsNoTracking().SingleAsync(x => x.Id == currentUser.UserId, cancellationToken);
            var customer = await new CustomerService(client).CreateAsync(new CustomerCreateOptions
            {
                Name = garage.Name,
                Email = user.Email ?? garage.Email,
                Metadata = new Dictionary<string, string> { ["garage_id"] = garageId.ToString("D") }
            }, cancellationToken: cancellationToken);
            customerId = customer.Id;
            subscription.SetBillingCustomer(customerId, DateTime.UtcNow);
            await context.SaveChangesAsync(cancellationToken);
        }

        var priceId = cycle == BillingCycle.Monthly ? _options.MonthlyPriceId : _options.AnnualPriceId;
        var baseUrl = PublicBaseUrl();
        var session = await new SessionService(client).CreateAsync(new SessionCreateOptions
        {
            Mode = "subscription",
            Customer = customerId,
            ClientReferenceId = garageId.ToString("D"),
            SuccessUrl = $"{baseUrl}/subscription?checkout=success",
            CancelUrl = $"{baseUrl}/subscription?checkout=cancelled",
            PaymentMethodTypes = ["card"],
            LineItems = [new SessionLineItemOptions { Price = priceId, Quantity = 1 }],
            Metadata = new Dictionary<string, string> { ["garage_id"] = garageId.ToString("D") },
            SubscriptionData = new SessionSubscriptionDataOptions
            {
                Metadata = new Dictionary<string, string> { ["garage_id"] = garageId.ToString("D") }
            }
        }, cancellationToken: cancellationToken);

        return new BillingRedirectResponse(session.Url);
    }

    public async Task<BillingRedirectResponse> CreatePortalAsync(CancellationToken cancellationToken)
    {
        EnsureEnabled();
        var garageId = currentGarage.RequireGarageId();
        var customerId = await context.GarageSubscriptions
            .Where(x => x.GarageId == garageId)
            .Select(x => x.BillingCustomerId)
            .SingleAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(customerId))
            throw new InvalidOperationException("A oficina ainda não possui cadastro de cobrança.");

        var session = await new Stripe.BillingPortal.SessionService(CreateClient()).CreateAsync(
            new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = customerId,
                ReturnUrl = $"{PublicBaseUrl()}/subscription"
            }, cancellationToken: cancellationToken);
        return new BillingRedirectResponse(session.Url);
    }

    public async Task ProcessAsync(string payload, string signature, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        var stripeEvent = EventUtility.ConstructEvent(payload, signature, _options.WebhookSecret);
        if (await context.ProcessedBillingEvents.AnyAsync(x => x.Id == stripeEvent.Id, cancellationToken))
            return;

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        switch (stripeEvent.Type)
        {
            case "checkout.session.completed" when stripeEvent.Data.Object is Session session:
                await LinkCheckoutAsync(session, cancellationToken);
                break;
            case "customer.subscription.created":
            case "customer.subscription.updated":
            case "customer.subscription.deleted":
                if (stripeEvent.Data.Object is Stripe.Subscription stripeSubscription)
                    await SynchronizeSubscriptionAsync(stripeSubscription, cancellationToken);
                break;
            case "invoice.paid":
            case "invoice.payment_failed":
                if (stripeEvent.Data.Object is Invoice invoice)
                    await SynchronizeInvoiceSubscriptionAsync(invoice, cancellationToken);
                break;
        }

        context.ProcessedBillingEvents.Add(new ProcessedBillingEvent(stripeEvent.Id, stripeEvent.Type, DateTime.UtcNow));
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task LinkCheckoutAsync(Session session, CancellationToken cancellationToken)
    {
        if (!TryGarageId(session.Metadata, session.ClientReferenceId, out var garageId))
            throw new InvalidOperationException("Checkout Stripe sem GarageId válido.");
        var subscription = await context.GarageSubscriptions.SingleAsync(x => x.GarageId == garageId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(session.CustomerId))
            subscription.SetBillingCustomer(session.CustomerId, DateTime.UtcNow);
    }

    private async Task SynchronizeSubscriptionAsync(Stripe.Subscription external, CancellationToken cancellationToken)
    {
        var subscription = await FindSubscriptionAsync(external, cancellationToken);
        var item = external.Items?.Data.FirstOrDefault();
        var priceId = item?.Price?.Id;
        if (string.IsNullOrWhiteSpace(external.CustomerId) || string.IsNullOrWhiteSpace(priceId))
            throw new InvalidOperationException("Assinatura Stripe sem cliente ou preço.");

        subscription.SynchronizeBilling(
            external.CustomerId,
            external.Id,
            priceId,
            MapStatus(external.Status),
            item?.CurrentPeriodStart,
            item?.CurrentPeriodEnd,
            external.CancelAtPeriodEnd,
            DateTime.UtcNow);
    }

    private async Task SynchronizeInvoiceSubscriptionAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        var subscriptionId = invoice.Parent?.SubscriptionDetails?.SubscriptionId;
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            logger.LogInformation("Fatura Stripe {StripeInvoiceId} não pertence a uma assinatura.", invoice.Id);
            return;
        }

        var external = await new Stripe.SubscriptionService(CreateClient())
            .GetAsync(subscriptionId, cancellationToken: cancellationToken);
        await SynchronizeSubscriptionAsync(external, cancellationToken);
    }

    private async Task<GarageSubscriptionEntity> FindSubscriptionAsync(
        Stripe.Subscription external,
        CancellationToken cancellationToken)
    {
        var existing = await context.GarageSubscriptions.SingleOrDefaultAsync(
            x => x.BillingSubscriptionId == external.Id || x.BillingCustomerId == external.CustomerId,
            cancellationToken);
        if (existing is not null) return existing;
        if (!TryGarageId(external.Metadata, null, out var garageId))
            throw new InvalidOperationException("Assinatura Stripe sem GarageId válido.");
        return await context.GarageSubscriptions.SingleAsync(x => x.GarageId == garageId, cancellationToken);
    }

    private static bool TryGarageId(IDictionary<string, string>? metadata, string? fallback, out Guid garageId)
    {
        var value = fallback;
        if (metadata is not null && metadata.TryGetValue("garage_id", out var configured)) value = configured;
        return Guid.TryParse(value, out garageId) && garageId != Guid.Empty;
    }

    private static SubscriptionStatus MapStatus(string status) => status switch
    {
        "active" or "trialing" => SubscriptionStatus.Active,
        "past_due" or "unpaid" or "incomplete" => SubscriptionStatus.PastDue,
        "paused" => SubscriptionStatus.Suspended,
        "canceled" or "incomplete_expired" => SubscriptionStatus.Cancelled,
        _ => SubscriptionStatus.PastDue
    };

    private StripeClient CreateClient() => new(_options.SecretKey);

    private string PublicBaseUrl() => configuration["App:PublicBaseUrl"]!.TrimEnd('/');

    private void EnsureEnabled()
    {
        if (!_options.Enabled)
            throw new InvalidOperationException("A cobrança online não está habilitada.");
    }
}
