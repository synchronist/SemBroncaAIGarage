using SemBroncaAI.Garage.Application.Features.ServiceOrders.Approval;

namespace SemBroncaAI.Garage.Application.Features.ServiceOrders.SendForApproval;

public sealed class SendForApprovalHandler(SendEstimateForApprovalHandler inner)
{
    public Task<ApprovalSummaryResponse> HandleAsync(Guid serviceOrderId, Guid? actorId = null,
        CancellationToken cancellationToken = default) => inner.HandleAsync(serviceOrderId, cancellationToken);
}
