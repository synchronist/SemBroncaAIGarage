using SemBroncaAI.Garage.Application.Abstractions.Security;
using SemBroncaAI.Garage.Domain.Interfaces;

namespace SemBroncaAI.Garage.Application.Features.ServiceOrders.Approval;

public sealed class SendEstimateForApprovalHandler(IServiceOrderRepository repository,
    IApprovalTokenService tokenService, IUnitOfWork unitOfWork)
{
    public async Task<ApprovalSummaryResponse> HandleAsync(Guid id, Guid? actorId = null, CancellationToken cancellationToken = default)
    {
        var order = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Ordem de serviço não encontrada.");
        var token = tokenService.Create();
        var now = DateTimeOffset.UtcNow;
        var approval = order.SendForApproval(token.Hash, token.ProtectedValue, now.AddDays(7), now, actorId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new(approval.Status, approval.CreatedAt, approval.ExpiresAt, null, null, null, token.Value);
    }
}
