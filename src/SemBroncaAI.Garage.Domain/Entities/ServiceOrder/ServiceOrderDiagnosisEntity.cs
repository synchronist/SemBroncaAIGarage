using SemBroncaAI.Garage.Domain.Common;

namespace SemBroncaAI.Garage.Domain.Entities.ServiceOrder;

public sealed class ServiceOrderDiagnosisEntity : Entity
{
    public Guid ServiceOrderId { get; private set; }

    public ServiceOrderEntity ServiceOrder { get; private set; } = default!;

    public string Description { get; private set; } = string.Empty;

    public string InternalNotes { get; private set; } = string.Empty;

    public Guid? ActorId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    private ServiceOrderDiagnosisEntity()
    {
    }

    public ServiceOrderDiagnosisEntity(
        Guid serviceOrderId,
        string description,
        string? internalNotes = null,
        Guid? actorId = null)
    {
        ServiceOrderId = Guard.AgainstEmpty(
            serviceOrderId,
            nameof(serviceOrderId));

        Description = Guard.AgainstNullOrWhiteSpace(
            description,
            nameof(description));

        InternalNotes = internalNotes?.Trim() ?? string.Empty;

        ActorId = actorId;

        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public void Update(
        string description,
        string? internalNotes,
        Guid? actorId = null)
    {
        Description = Guard.AgainstNullOrWhiteSpace(
            description,
            nameof(description));

        InternalNotes = internalNotes?.Trim() ?? string.Empty;

        ActorId = actorId;

        UpdatedAt = DateTimeOffset.UtcNow;
    }
}