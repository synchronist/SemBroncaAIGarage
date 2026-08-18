using SemBroncaAI.Garage.Domain.Common;

namespace SemBroncaAI.Garage.Domain.Entities;

public sealed class TeamInvitationEntity : Entity
{
    public Guid GarageId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid InvitedByUserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UsedAt { get; private set; }

    private TeamInvitationEntity() { }
    public TeamInvitationEntity(Guid garageId, Guid userId, Guid invitedByUserId, string tokenHash, DateTime expiresAt)
    {
        if (garageId == Guid.Empty || userId == Guid.Empty || invitedByUserId == Guid.Empty) throw new ArgumentException("Convite inválido.");
        GarageId = garageId; UserId = userId; InvitedByUserId = invitedByUserId; TokenHash = tokenHash;
        ExpiresAt = expiresAt; CreatedAt = DateTime.UtcNow;
    }
    public bool CanUse(DateTime now) => UsedAt is null && ExpiresAt > now;
    public void MarkUsed(DateTime now) { if (!CanUse(now)) throw new InvalidOperationException("Convite inválido."); UsedAt = now; }
}
