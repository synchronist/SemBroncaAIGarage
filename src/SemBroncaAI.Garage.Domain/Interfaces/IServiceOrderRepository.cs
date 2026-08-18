using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;

namespace SemBroncaAI.Garage.Domain.Interfaces;

public interface IServiceOrderRepository
{
    Task<ServiceOrderEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    async Task<ServiceOrderEntity?> GetByIdAsync(
        Guid id,
        Guid garageId,
        CancellationToken cancellationToken = default)
    {
        var order = await GetByIdAsync(id, cancellationToken);
        return order?.GarageId == garageId ? order : null;
    }

    Task<ServiceOrderEntity?> GetByNumberAsync(
        Guid garageId,
        int number,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ServiceOrderEntity>> ListVehicleHistoryAsync(
        Guid garageId,
        Guid vehicleId,
        Guid excludeServiceOrderId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<ServiceOrderEntity>>([]);

    Task<ServiceOrderTechnicalHistoryPage> ListVehicleTechnicalHistoryAsync(
        Guid garageId, Guid vehicleId, Guid excludeServiceOrderId,
        int offset, int pageSize, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ServiceOrderTechnicalHistoryPage(0, []));

    Task AddAsync(
        ServiceOrderEntity serviceOrder,
        CancellationToken cancellationToken = default);

    Task<ServiceOrderEntity?> GetByApprovalTokenHashAsync(string tokenHash,
        CancellationToken cancellationToken = default) => Task.FromResult<ServiceOrderEntity?>(null);

    void RemoveEstimateItems(
        IEnumerable<ServiceOrderEstimateItemEntity> items);
}

public sealed record ServiceOrderTechnicalHistoryPage(
    int TotalCount,
    IReadOnlyCollection<ServiceOrderEntity> Items);
