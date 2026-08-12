using SemBroncaAI.Garage.Application.Abstractions.Security;
using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;
using SemBroncaAI.Garage.Domain.Interfaces;

namespace SemBroncaAI.Garage.Application.Features.ServiceOrders.Approval;

public sealed class PublicApprovalHandler(IServiceOrderRepository repository,
    IApprovalTokenService tokenService, IUnitOfWork unitOfWork)
{
    public async Task<PublicApprovalResponse?> GetAsync(string token, string? logoUrl,
        CancellationToken cancellationToken = default)
    {
        var resolved = await ResolveAsync(token, cancellationToken);
        if (resolved is null) return null;
        var (order, approval) = resolved.Value;
        var estimate = order.Estimate!;
        var garage = order.Garage;
        return new(garage.Name, garage.Phone, garage.Email,
            garage.LogoStorageKey is null ? null : logoUrl, garage.PrimaryColor ?? "#F97316",
            order.Number, order.Vehicle.Customer.Name,
            $"{order.Vehicle.Brand} {order.Vehicle.Model}", order.Vehicle.Plate,
            order.Diagnosis?.Description, order.CustomerComplaint, order.Mileage,
            approval.Status, approval.CreatedAt, approval.ExpiresAt, approval.RespondedAt,
            approval.CustomerComment, estimate.ServicesSubtotal, estimate.PartsSubtotal,
            estimate.Total, estimate.Items.Select(x => new PublicApprovalItemResponse(
                x.Description, x.Type, x.Quantity, x.UnitPrice, x.Total)).ToArray());
    }

    public async Task<EstimateApprovalStatus?> RespondAsync(string token, bool approve,
        ApprovalDecisionRequest request, CancellationToken cancellationToken = default)
    {
        var resolved = await ResolveAsync(token, cancellationToken);
        if (resolved is null) return null;
        var (order, approval) = resolved.Value;
        if (approval.Status != EstimateApprovalStatus.Pending) return approval.Status;
        if (approval.IsExpired(DateTimeOffset.UtcNow)) throw new InvalidOperationException("Este link de aprovação expirou.");
        if (order.Estimate?.UpdatedAt != approval.EstimateUpdatedAt)
            throw new InvalidOperationException("Este orçamento foi alterado e o link não é mais válido.");
        if (approve) order.ApproveEstimate(approval.Id, request.CustomerName, DateTimeOffset.UtcNow);
        else order.RejectEstimate(approval.Id, request.CustomerName, request.Comment, DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return approval.Status;
    }

    private async Task<(ServiceOrderEntity Order, ServiceOrderEstimateApprovalEntity Approval)?> ResolveAsync(
        string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 200) return null;
        var hash = tokenService.Hash(token);
        var order = await repository.GetByApprovalTokenHashAsync(hash, cancellationToken);
        var approval = order?.EstimateApprovals.SingleOrDefault(x => x.TokenHash == hash);
        return order is null || approval is null || !approval.IsActive ||
            order.Estimate?.UpdatedAt != approval.EstimateUpdatedAt ? null : (order, approval);
    }
}
