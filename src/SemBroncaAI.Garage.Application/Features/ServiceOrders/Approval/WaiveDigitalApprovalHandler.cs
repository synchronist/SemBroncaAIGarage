using SemBroncaAI.Garage.Application.Features.ServiceOrders;
using SemBroncaAI.Garage.Domain.Interfaces;

namespace SemBroncaAI.Garage.Application.Features.ServiceOrders.Approval;

public sealed class WaiveDigitalApprovalHandler(
    IServiceOrderRepository repository,
    IUnitOfWork unitOfWork)
{
    public async Task<ServiceOrderTransitionResponse> HandleAsync(
        Guid serviceOrderId,
        Guid? actorId = null,
        CancellationToken cancellationToken = default)
    {
        var serviceOrder = await repository.GetByIdAsync(serviceOrderId, cancellationToken)
            ?? throw new InvalidOperationException("Ordem de serviço não encontrada.");

        serviceOrder.WaiveDigitalApproval(DateTimeOffset.UtcNow, actorId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ServiceOrderTransitionResponse(
            serviceOrder.Id,
            serviceOrder.Number,
            serviceOrder.Status,
            serviceOrder.History.Count);
    }
}
