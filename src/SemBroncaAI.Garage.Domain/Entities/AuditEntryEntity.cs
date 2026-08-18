using SemBroncaAI.Garage.Domain.Common;

namespace SemBroncaAI.Garage.Domain.Entities;

public sealed class AuditEntryEntity : Entity
{
    public DateTime OccurredAt { get; private set; }
    public Guid ActorUserId { get; private set; }
    public string ActorContext { get; private set; } = string.Empty;
    public Guid? GarageId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public string? Summary { get; private set; }

    private AuditEntryEntity() { }

    public AuditEntryEntity(DateTime occurredAt, Guid actorUserId, string actorContext, Guid? garageId,
        string action, string entityType, string entityId, string? summary = null)
    {
        if (actorUserId == Guid.Empty) throw new ArgumentException("O ator é obrigatório.", nameof(actorUserId));
        OccurredAt = occurredAt;
        ActorUserId = actorUserId;
        ActorContext = Required(actorContext, nameof(actorContext), 100);
        GarageId = garageId;
        Action = Required(action, nameof(action), 100);
        EntityType = Required(entityType, nameof(entityType), 100);
        EntityId = Required(entityId, nameof(entityId), 100);
        Summary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim()[..Math.Min(summary.Trim().Length, 500)];
    }

    private static string Required(string value, string parameter, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Valor obrigatório.", parameter);
        var normalized = value.Trim();
        return normalized.Length <= maximumLength ? normalized : normalized[..maximumLength];
    }
}
