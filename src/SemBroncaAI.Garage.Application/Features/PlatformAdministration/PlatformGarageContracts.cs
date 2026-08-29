using SemBroncaAI.Garage.Domain.Entities.Garage;
using SemBroncaAI.Garage.Domain.Entities;

namespace SemBroncaAI.Garage.Application.Features.PlatformAdministration;

public sealed record PlatformDashboardResponse(
    int TotalGarages,
    int ActiveGarages,
    int InactiveGarages,
    int TotalUsers,
    int TrialSubscriptions,
    int ActiveSubscriptions,
    int SuspendedSubscriptions,
    IReadOnlyCollection<PlatformRecentGarage> RecentGarages)
{
    public int PastDueSubscriptions { get; init; }
    public int CancelledSubscriptions { get; init; }
    public int NewGaragesLast30Days { get; init; }
    public int TrialsStartedLast30Days { get; init; }
    public int ActiveGaragesToday { get; init; }
    public int ActiveGaragesLast7Days { get; init; }
    public int ActiveGaragesLast30Days { get; init; }
    public int ServiceOrdersToday { get; init; }
    public int ServiceOrdersLast30Days { get; init; }
    public int DigitalApprovalsLast30Days { get; init; }
    public int DigitalApprovalWaiversLast30Days { get; init; }
    public int TrialsEndingNext7Days { get; init; }
    public int GaragesWithoutServiceOrders { get; init; }
    public int Inactive7Days { get; init; }
    public int Inactive14Days { get; init; }
    public int Inactive30Days { get; init; }
    public IReadOnlyCollection<PlatformRelevantGarage> RelevantGarages { get; init; } = [];
    public IReadOnlyCollection<PlatformDailyVolume> ServiceOrderVolume { get; init; } = [];
}

public sealed record PlatformRecentGarage(Guid Id, string Name, bool Active, DateTime CreatedAt);

public sealed record PlatformRelevantGarage(
    Guid Id, string Name, bool Active, SubscriptionStatus SubscriptionStatus, SubscriptionPlan Plan,
    DateTime? BillingReferenceAt, DateTimeOffset? LastOperationalActivityAt,
    int ServiceOrdersLast30Days, string Situation, int RiskPriority);

public sealed record PlatformDailyVolume(DateOnly Date, int Count);

public sealed record ListPlatformGaragesQuery(string? Search, bool? Active, int Page = 1, int PageSize = 20);

public sealed record PlatformGarageListResponse(
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    IReadOnlyCollection<PlatformGarageListItem> Items);

public sealed record PlatformGarageListItem(
    Guid Id,
    string Name,
    string Document,
    string Email,
    string Phone,
    bool Active,
    DateTime CreatedAt,
    int UserCount,
    string? OwnerName,
    string? OwnerEmail,
    SubscriptionStatus SubscriptionStatus);

public sealed record PlatformSubscriptionResponse(
    SubscriptionStatus Status,
    SubscriptionPlan Plan,
    DateTime StartedAt,
    DateTime? TrialEndsAt,
    DateTime? CurrentPeriodStart,
    DateTime? CurrentPeriodEnd,
    DateTime? SuspendedAt,
    DateTime? CancelledAt,
    bool TrialExpired);

public sealed record PlatformGarageDetailsResponse(
    Guid Id,
    string Name,
    string Document,
    string Email,
    string Phone,
    bool Active,
    DateTime CreatedAt,
    int UserCount,
    string? OwnerName,
    string? OwnerEmail,
    string? OwnerUserName,
    bool? OwnerActive,
    InvitationDeliveryStatus? OwnerInvitationDeliveryStatus,
    PlatformSubscriptionResponse Subscription,
    IReadOnlyCollection<PlatformAuditItem> RecentActivity);

public sealed record PlatformAuditItem(DateTime OccurredAt, string Action, string ActorContext, string? Summary);

public sealed record UpdateGarageSubscriptionCommand(
    SubscriptionStatus Status,
    SubscriptionPlan Plan);

public sealed record CreatePlatformGarageCommand(
    string Name,
    string Document,
    string Phone,
    string Email,
    string OwnerName,
    string OwnerEmail,
    string OwnerUserName);

public sealed record CreatePlatformGarageResponse(
    Guid GarageId, string Name, bool Active, InvitationDeliveryStatus OwnerInvitationDeliveryStatus);

public sealed record PublicGarageSignupCommand(
    string Name, string Document, string Phone, string Email,
    string OwnerName, string OwnerEmail, bool AcceptedTerms);

public interface IPublicGarageSignup
{
    Task<CreatePlatformGarageResponse> SignupAsync(PublicGarageSignupCommand command,
        CancellationToken cancellationToken = default);
}

public sealed record OwnerInvitationOperationResponse(InvitationDeliveryStatus DeliveryStatus);

public interface IPlatformGarageAdministration
{
    Task<PlatformDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<PlatformGarageListResponse> ListAsync(ListPlatformGaragesQuery query, CancellationToken cancellationToken = default);
    Task<PlatformGarageDetailsResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CreatePlatformGarageResponse> CreateAsync(CreatePlatformGarageCommand command, CancellationToken cancellationToken = default);
    Task<OwnerInvitationOperationResponse?> ResendOwnerInvitationAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> SetActiveAsync(Guid id, bool active, CancellationToken cancellationToken = default);
    Task<PlatformSubscriptionResponse?> UpdateSubscriptionAsync(Guid id, UpdateGarageSubscriptionCommand command,
        CancellationToken cancellationToken = default);
}
