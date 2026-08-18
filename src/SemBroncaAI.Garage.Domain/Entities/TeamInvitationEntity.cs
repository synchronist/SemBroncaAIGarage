using System.Security.Cryptography;
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
    public InvitationDeliveryStatus DeliveryStatus { get; private set; }
    public DateTime? LastDeliveryAttemptAt { get; private set; }
    public DateTime? SentAt { get; private set; }

    private TeamInvitationEntity() { }
    public TeamInvitationEntity(Guid garageId, Guid userId, Guid invitedByUserId, string tokenHash, DateTime expiresAt)
    {
        if (garageId == Guid.Empty || userId == Guid.Empty || invitedByUserId == Guid.Empty) throw new ArgumentException("Convite inválido.");
        GarageId = garageId; UserId = userId; InvitedByUserId = invitedByUserId; TokenHash = tokenHash;
        ExpiresAt = expiresAt; CreatedAt = DateTime.UtcNow;
        DeliveryStatus = InvitationDeliveryStatus.Created;
    }
    public bool CanUse(DateTime now) => UsedAt is null && ExpiresAt > now;
    public bool MatchesTokenHash(string tokenHash, DateTime now) =>
        CanUse(now) &&
        CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(TokenHash),
            Convert.FromHexString(tokenHash));
    public void MarkUsed(DateTime now) { if (!CanUse(now)) throw new InvalidOperationException("Convite inválido."); UsedAt = now; }
    public void Renew(string tokenHash, DateTime expiresAt)
    {
        if (UsedAt is not null) throw new InvalidOperationException("Convite já utilizado.");
        TokenHash = string.IsNullOrWhiteSpace(tokenHash) ? throw new ArgumentException("Token inválido.", nameof(tokenHash)) : tokenHash;
        ExpiresAt = expiresAt;
        DeliveryStatus = InvitationDeliveryStatus.Created;
    }
    public void Invalidate(DateTime now)
    {
        if (UsedAt is null && ExpiresAt > now) ExpiresAt = now;
    }
    public void MarkSent(DateTime now) { DeliveryStatus = InvitationDeliveryStatus.Sent; LastDeliveryAttemptAt = SentAt = now; }
    public void MarkDeliveryFailed(DateTime now) { DeliveryStatus = InvitationDeliveryStatus.Failed; LastDeliveryAttemptAt = now; }
}

public enum InvitationDeliveryStatus { Created = 1, Sent = 2, Failed = 3 }
