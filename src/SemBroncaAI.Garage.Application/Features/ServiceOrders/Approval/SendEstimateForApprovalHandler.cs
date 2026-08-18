using SemBroncaAI.Garage.Application.Abstractions.Security;
using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Domain.Interfaces;

namespace SemBroncaAI.Garage.Application.Features.ServiceOrders.Approval;

public sealed class SendEstimateForApprovalHandler(IServiceOrderRepository repository,
    IApprovalTokenService tokenService, IApprovalRequestPersistence persistence)
{
    public async Task<ApprovalSummaryResponse> HandleAsync(Guid id, Guid? actorId = null, CancellationToken cancellationToken = default)
    {
        var order = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Ordem de serviço não encontrada.");
        var token = tokenService.Create();
        var now = DateTimeOffset.UtcNow;
        var approval = order.SendForApproval(token.Hash, token.ProtectedValue, now.AddDays(7), now, actorId);
        var persistedApproval = await persistence.SaveAsync(approval, cancellationToken);
        var publicToken = persistedApproval.Id == approval.Id
            ? token.Value
            : tokenService.Unprotect(persistedApproval.ProtectedToken);

        return new(persistedApproval.Status, persistedApproval.CreatedAt, persistedApproval.ExpiresAt,
            null, null, null, publicToken);
    }
}
