using SemBroncaAI.Garage.Domain.Interfaces;

namespace SemBroncaAI.Garage.Application.Features.ServiceOrders.Approval;

public sealed class ReviseEstimateHandler(IServiceOrderRepository repository, IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(Guid id, Guid? actorId = null, CancellationToken cancellationToken = default)
    {
        var order = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Ordem de serviço não encontrada.");
        order.ReviseRejectedEstimate(actorId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
