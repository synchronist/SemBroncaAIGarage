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
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

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
        Status = status;
        if (status == SubscriptionStatus.Trial)
            TrialEndsAt = trialEndsAt ?? TrialEndsAt ?? now;
        SuspendedAt = status == SubscriptionStatus.Suspended ? now : null;
        if (status == SubscriptionStatus.Cancelled) CancelledAt = now;
        UpdatedAt = now;
    }

}
