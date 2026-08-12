using SemBroncaAI.Garage.Domain.Interfaces;

namespace SemBroncaAI.Garage.Application.Features.ServiceOrders.RestoreServiceOrder;

public sealed class RestoreServiceOrderHandler(IServiceOrderRepository repository, IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(Guid serviceOrderId, Guid garageId, CancellationToken cancellationToken = default)
    {
        if (garageId == Guid.Empty) throw new ArgumentException("A oficina é obrigatória.", nameof(garageId));
        var order = await repository.GetByIdAsync(serviceOrderId, cancellationToken);
        if (order is null || order.GarageId != garageId)
            throw new InvalidOperationException("Ordem de serviço não encontrada.");

        order.Restore();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
