using SemBroncaAI.Garage.Application.Features.ServiceOrders;
using SemBroncaAI.Garage.Domain.Interfaces;

namespace SemBroncaAI.Garage.Application.Features.ServiceOrders.CancelServiceOrder;

public sealed class CancelServiceOrderHandler
{
    private readonly IServiceOrderRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelServiceOrderHandler(
        IServiceOrderRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceOrderTransitionResponse> HandleAsync(
        Guid serviceOrderId,
        Guid? actorId = null,
        CancellationToken cancellationToken = default)
    {
        var serviceOrder = await _repository.GetByIdAsync(
            serviceOrderId,
            cancellationToken);

        if (serviceOrder is null)
        {
            throw new InvalidOperationException(
                "Ordem de serviço não encontrada.");
        }

        serviceOrder.Cancel(actorId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ServiceOrderTransitionResponse(
            serviceOrder.Id,
            serviceOrder.Number,
            serviceOrder.Status,
            serviceOrder.History.Count);
    }
}