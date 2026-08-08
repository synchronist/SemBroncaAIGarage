using SemBroncaAI.Garage.Application.Features.ServiceOrders;
using SemBroncaAI.Garage.Domain.Interfaces;

namespace SemBroncaAI.Garage.Application.Features.ServiceOrders.FinishService;

public sealed class FinishServiceHandler
{
    private readonly IServiceOrderRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public FinishServiceHandler(
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

        serviceOrder.Finish(actorId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ServiceOrderTransitionResponse(
            serviceOrder.Id,
            serviceOrder.Number,
            serviceOrder.Status,
            serviceOrder.History.Count);
    }
}