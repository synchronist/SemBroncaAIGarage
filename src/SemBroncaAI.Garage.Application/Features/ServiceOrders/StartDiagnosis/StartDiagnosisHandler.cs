using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;
using SemBroncaAI.Garage.Domain.Interfaces;

namespace SemBroncaAI.Garage.Application.Features.ServiceOrders.StartDiagnosis;

public sealed class StartDiagnosisHandler
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public StartDiagnosisHandler(
        IServiceOrderRepository serviceOrderRepository,
        IUnitOfWork unitOfWork)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<StartDiagnosisResponse> HandleAsync(
        Guid serviceOrderId,
        Guid? actorId = null,
        CancellationToken cancellationToken = default)
    {
        var serviceOrder =
            await _serviceOrderRepository.GetByIdAsync(
                serviceOrderId,
                cancellationToken);

        if (serviceOrder is null)
        {
            throw new InvalidOperationException(
                "Ordem de serviço não encontrada.");
        }

        serviceOrder.StartDiagnosis(actorId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new StartDiagnosisResponse(
            serviceOrder.Id,
            serviceOrder.Number,
            serviceOrder.Status,
            serviceOrder.History.Count);
    }
}

public sealed record StartDiagnosisResponse(
    Guid Id,
    int Number,
    ServiceOrderStatus Status,
    int HistoryCount);