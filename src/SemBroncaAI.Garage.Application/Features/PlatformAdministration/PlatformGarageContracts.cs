using SemBroncaAI.Garage.Domain.Entities.Garage;

namespace SemBroncaAI.Garage.Application.Features.PlatformAdministration;

public sealed record PlatformDashboardResponse(
    int TotalGarages,
    int ActiveGarages,
    int InactiveGarages,
    int TotalUsers,
    int TrialSubscriptions,
    int ActiveSubscriptions,
    int SuspendedSubscriptions,
    IReadOnlyCollection<PlatformRecentGarage> RecentGarages);

public sealed record PlatformRecentGarage(Guid Id, string Name, bool Active, DateTime CreatedAt);

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
    string OwnerUserName,
    string InitialPassword,
    string ConfirmPassword);

public sealed record CreatePlatformGarageResponse(Guid GarageId, string Name, bool Active);

public interface IPlatformGarageAdministration
{
    Task<PlatformDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<PlatformGarageListResponse> ListAsync(ListPlatformGaragesQuery query, CancellationToken cancellationToken = default);
    Task<PlatformGarageDetailsResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CreatePlatformGarageResponse> CreateAsync(CreatePlatformGarageCommand command, CancellationToken cancellationToken = default);
    Task<bool> SetActiveAsync(Guid id, bool active, CancellationToken cancellationToken = default);
    Task<PlatformSubscriptionResponse?> UpdateSubscriptionAsync(Guid id, UpdateGarageSubscriptionCommand command,
        CancellationToken cancellationToken = default);
}
