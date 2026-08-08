using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;
using SemBroncaAI.Garage.Domain.Interfaces;

namespace SemBroncaAI.Garage.Application.Features.ServiceOrders.SaveEstimate;

public sealed class SaveEstimateHandler
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SaveEstimateHandler(IServiceOrderRepository serviceOrderRepository, IUnitOfWork unitOfWork)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SaveEstimateResponse> HandleAsync(
        Guid serviceOrderId,
        SaveEstimateCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var serviceOrder =
            await _serviceOrderRepository.GetByIdAsync(
                serviceOrderId,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Ordem de serviço não encontrada.");

        var replacedItems =
            serviceOrder.Estimate?.Items.ToArray() ?? [];

        serviceOrder.SaveEstimate(command.Items.Select(item => new ServiceOrderEstimateItemData(
            item.Description,
            item.Type,
            item.Quantity,
            item.UnitPrice)));

        if (replacedItems.Length > 0)
        {
            _serviceOrderRepository.RemoveEstimateItems(replacedItems);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var estimate = serviceOrder.Estimate!;

        return new SaveEstimateResponse(
            estimate.Id,
            estimate.ServicesSubtotal,
            estimate.PartsSubtotal,
            estimate.Total,
            estimate.Items.Count);
    }
}

public sealed record SaveEstimateResponse(
    Guid Id,
    decimal ServicesSubtotal,
    decimal PartsSubtotal,
    decimal Total,
    int ItemCount);
