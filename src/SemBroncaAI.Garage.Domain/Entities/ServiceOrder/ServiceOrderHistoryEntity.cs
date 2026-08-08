using SemBroncaAI.Garage.Domain.Common;

namespace SemBroncaAI.Garage.Domain.Entities.ServiceOrder;

public sealed class ServiceOrderHistoryEntity : Entity
{
    public Guid ServiceOrderId { get; private set; }

    public ServiceOrderStatus? PreviousStatus { get; private set; }

    public ServiceOrderStatus CurrentStatus { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public Guid? ActorId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public ServiceOrderEntity ServiceOrder { get; private set; } = default!;

    private ServiceOrderHistoryEntity()
    {
    }

    internal ServiceOrderHistoryEntity(
        ServiceOrderStatus? previousStatus,
        ServiceOrderStatus currentStatus,
        string description,
        Guid? actorId)
    {
        PreviousStatus = previousStatus;
        CurrentStatus = currentStatus;

        Description = Guard.AgainstNullOrWhiteSpace(
            description,
            nameof(description));

        ActorId = actorId;

        CreatedAt = DateTimeOffset.UtcNow;
    }
}