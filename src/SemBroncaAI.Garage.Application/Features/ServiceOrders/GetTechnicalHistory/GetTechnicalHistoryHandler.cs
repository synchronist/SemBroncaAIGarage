using SemBroncaAI.Garage.Application.Features.ServiceOrders.GetServiceOrderById;
using SemBroncaAI.Garage.Domain.Interfaces;

using SemBroncaAI.Garage.Application.Common;

namespace SemBroncaAI.Garage.Application.Features.ServiceOrders.GetTechnicalHistory;

public sealed class GetTechnicalHistoryHandler(IServiceOrderRepository repository)
{
    public async Task<TechnicalHistoryPageResponse> HandleAsync(
        Guid serviceOrderId, Guid garageId, int offset, int pageSize,
        CancellationToken cancellationToken = default)
    {
        PaginationRules.ValidateOffset(offset, pageSize);
        var current = await repository.GetByIdAsync(serviceOrderId, garageId, cancellationToken)
            ?? throw new InvalidOperationException("Ordem de serviço não encontrada.");
        var result = await repository.ListVehicleTechnicalHistoryAsync(
            garageId, current.VehicleId, current.Id, offset, pageSize, cancellationToken);
        return new(offset, pageSize, result.TotalCount, result.Items.Select(Map).ToArray());
    }

    private static ServiceOrderTechnicalHistoryResponse Map(Domain.Entities.ServiceOrder.ServiceOrderEntity item) =>
        new(item.Id, item.Number, item.Status, item.CustomerComplaint, item.Mileage, item.CreatedAt,
            item.Diagnosis?.Description, item.Diagnosis?.InternalNotes,
            item.Estimate?.Items.Select(x => x.Description).ToArray() ?? []);
}

public sealed record TechnicalHistoryPageResponse(
    int Offset, int PageSize, int TotalCount,
    IReadOnlyCollection<ServiceOrderTechnicalHistoryResponse> Items);
