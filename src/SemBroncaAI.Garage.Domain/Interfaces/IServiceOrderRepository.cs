using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;

namespace SemBroncaAI.Garage.Domain.Interfaces;

public interface IServiceOrderRepository
{
    Task<ServiceOrderEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ServiceOrderEntity?> GetByNumberAsync(
        Guid garageId,
        int number,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ServiceOrderEntity serviceOrder,
        CancellationToken cancellationToken = default);

    Task<ServiceOrderEntity?> GetByApprovalTokenHashAsync(string tokenHash,
        CancellationToken cancellationToken = default) => Task.FromResult<ServiceOrderEntity?>(null);

    void RemoveEstimateItems(
        IEnumerable<ServiceOrderEstimateItemEntity> items);
}
