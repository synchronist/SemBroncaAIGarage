using SemBroncaAI.Garage.Domain.Interfaces;

namespace SemBroncaAI.Garage.Application.Features.ServiceOrders.SaveDiagnosis;

public sealed class SaveDiagnosisHandler
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SaveDiagnosisHandler(
        IServiceOrderRepository serviceOrderRepository,
        IUnitOfWork unitOfWork)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SaveDiagnosisResponse> HandleAsync(
        Guid serviceOrderId,
        SaveDiagnosisCommand command,
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

        serviceOrder.SaveDiagnosis(
            command.Description,
            command.InternalNotes,
            actorId);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        var diagnosis = serviceOrder.Diagnosis!;

        return new SaveDiagnosisResponse(
            serviceOrder.Id,
            diagnosis.Description,
            diagnosis.InternalNotes,
            diagnosis.UpdatedAt);
    }
}