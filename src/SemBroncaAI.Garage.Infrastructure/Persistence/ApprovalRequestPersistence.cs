using Microsoft.EntityFrameworkCore;
using Npgsql;
using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;

namespace SemBroncaAI.Garage.Infrastructure.Persistence;

public sealed class ApprovalRequestPersistence(GarageDbContext context)
    : IApprovalRequestPersistence
{
    public const string ActiveApprovalConstraint =
        "UX_Approval_ActivePendingVersion";

    public async Task<ServiceOrderEstimateApprovalEntity> SaveAsync(
        ServiceOrderEstimateApprovalEntity candidate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return candidate;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: ActiveApprovalConstraint
            })
        {
            context.ChangeTracker.Clear();

            return await context.ServiceOrderEstimateApprovals
                .AsNoTracking()
                .SingleAsync(
                    approval =>
                        approval.ServiceOrderId == candidate.ServiceOrderId &&
                        approval.EstimateUpdatedAt == candidate.EstimateUpdatedAt &&
                        approval.Status == EstimateApprovalStatus.Pending &&
                        approval.InvalidatedAt == null,
                    cancellationToken);
        }
    }
}
