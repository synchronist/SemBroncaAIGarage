using SemBroncaAI.Garage.Domain.Common;

namespace SemBroncaAI.Garage.Domain.Entities.Garage;

public sealed class GarageSubscriptionEntity : Entity
{
    public Guid GarageId { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public SubscriptionPlan Plan { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? TrialEndsAt { get; private set; }
    public DateTime? CurrentPeriodStart { get; private set; }
    public DateTime? CurrentPeriodEnd { get; private set; }
    public DateTime? SuspendedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public DateTime? PastDueAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string? BillingCustomerId { get; private set; }
    public string? BillingSubscriptionId { get; private set; }
    public string? BillingPriceId { get; private set; }
    public bool CancelAtPeriodEnd { get; private set; }

    private GarageSubscriptionEntity() { }

    public GarageSubscriptionEntity(Guid garageId, SubscriptionPlan plan, DateTime now, int trialDays)
    {
        if (garageId == Guid.Empty) throw new ArgumentException("A oficina é obrigatória.", nameof(garageId));
        if (trialDays <= 0) throw new ArgumentOutOfRangeException(nameof(trialDays));
        GarageId = garageId;
        Plan = plan;
        Status = SubscriptionStatus.Trial;
        StartedAt = now;
        TrialEndsAt = now.AddDays(trialDays);
        CreatedAt = UpdatedAt = now;
    }

    public bool IsTrialExpired(DateTime now) =>
        Status == SubscriptionStatus.Trial && TrialEndsAt.HasValue && TrialEndsAt.Value < now;

    public void ChangePlan(SubscriptionPlan plan, DateTime now)
    {
        Plan = plan;
        UpdatedAt = now;
    }

    public void ChangeStatus(SubscriptionStatus status, DateTime now, DateTime? trialEndsAt = null)
    {
        if (status == SubscriptionStatus.Trial && Status != SubscriptionStatus.Trial)
            throw new InvalidOperationException("O Trial só pode ser iniciado no onboarding.");
        var previousStatus = Status;
        Status = status;
        if (status == SubscriptionStatus.Trial)
            TrialEndsAt = trialEndsAt ?? TrialEndsAt ?? now;
        SuspendedAt = status == SubscriptionStatus.Suspended ? now : null;
        PastDueAt = status == SubscriptionStatus.PastDue
            ? previousStatus == SubscriptionStatus.PastDue ? PastDueAt ?? now : now
            : null;
        if (status == SubscriptionStatus.Cancelled) CancelledAt = now;
        UpdatedAt = now;
    }

    public bool AdvanceLifecycle(DateTime now, TimeSpan pastDueGracePeriod)
    {
        if (pastDueGracePeriod < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(pastDueGracePeriod));

        if (Status == SubscriptionStatus.Trial && TrialEndsAt <= now)
        {
            ChangeStatus(SubscriptionStatus.Suspended, now);
            return true;
        }

        if (Status == SubscriptionStatus.PastDue && PastDueAt.HasValue &&
            PastDueAt.Value.Add(pastDueGracePeriod) <= now)
        {
            ChangeStatus(SubscriptionStatus.Suspended, now);
            return true;
        }

        if (Status == SubscriptionStatus.Active && CancelAtPeriodEnd && CurrentPeriodEnd <= now)
        {
            ChangeStatus(SubscriptionStatus.Cancelled, now);
            return true;
        }

        return false;
    }

    public void SetBillingCustomer(string customerId, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(customerId)) throw new ArgumentException("O cliente de cobrança é obrigatório.", nameof(customerId));
        BillingCustomerId = customerId;
        UpdatedAt = now;
    }

    public void SynchronizeBilling(
        string customerId,
        string subscriptionId,
        string priceId,
        SubscriptionStatus status,
        DateTime? currentPeriodStart,
        DateTime? currentPeriodEnd,
        bool cancelAtPeriodEnd,
        DateTime now)
    {
        if (string.IsNullOrWhiteSpace(customerId)) throw new ArgumentException("O cliente de cobrança é obrigatório.", nameof(customerId));
        if (string.IsNullOrWhiteSpace(subscriptionId)) throw new ArgumentException("A assinatura externa é obrigatória.", nameof(subscriptionId));
        if (string.IsNullOrWhiteSpace(priceId)) throw new ArgumentException("O preço externo é obrigatório.", nameof(priceId));

        BillingCustomerId = customerId;
        BillingSubscriptionId = subscriptionId;
        BillingPriceId = priceId;
        CurrentPeriodStart = currentPeriodStart;
        CurrentPeriodEnd = currentPeriodEnd;
        CancelAtPeriodEnd = cancelAtPeriodEnd;
        ChangeStatus(status, now);
    }

}
