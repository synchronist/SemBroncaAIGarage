namespace SemBroncaAI.Garage.Application.Abstractions.Persistence;

public interface IAuditWriter
{
    void Add(Guid? garageId, string action, string entityType, string entityId, string? summary = null);
}

public static class AuditActions
{
    public const string GarageCreated = "garage.created";
    public const string GarageActivated = "garage.activated";
    public const string GarageDeactivated = "garage.deactivated";
    public const string SubscriptionChanged = "subscription.changed";
    public const string MemberInvited = "member.invited";
    public const string InvitationResent = "member.invitation-resent";
    public const string MemberActivated = "member.activated";
    public const string MemberDeactivated = "member.deactivated";
    public const string MemberRoleChanged = "member.role-changed";
    public const string GarageSettingsChanged = "garage.settings-changed";
    public const string OwnerPendingCreated = "owner.pending-created";
    public const string OwnerInvitationCreated = "owner.invitation-created";
    public const string OwnerInvitationResent = "owner.invitation-resent";
    public const string OwnerActivated = "owner.activated";
}
