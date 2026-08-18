using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;

namespace SemBroncaAI.Garage.Application.Abstractions.Persistence;

public interface IApprovalRequestPersistence
{
    Task<ServiceOrderEstimateApprovalEntity> SaveAsync(
        ServiceOrderEstimateApprovalEntity candidate,
        CancellationToken cancellationToken = default);
}
