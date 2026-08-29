using SemBroncaAI.Garage.Application.Abstractions.Security;
using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;
using SemBroncaAI.Garage.Domain.Interfaces;
using SemBroncaAI.Garage.Domain.Common;
using System.Text.Json;

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
        var garage = order.Garage;
        var snapshot = ReadSnapshot(approval, order.Estimate!);
        var services = snapshot.Where(x => x.Type == EstimateItemType.Service).Sum(x => x.Total);
        var parts = snapshot.Where(x => x.Type == EstimateItemType.Part).Sum(x => x.Total);
        var approvedIds = ReadApprovedIds(approval);
        return new PublicApprovalResponse(garage.Name, garage.Phone, garage.Email,
            garage.LogoStorageKey is null ? null : logoUrl, garage.PrimaryColor ?? "#F97316",
            order.Number, order.Vehicle.Customer.Name,
            $"{order.Vehicle.Brand} {order.Vehicle.Model}", order.Vehicle.Plate,
            order.Diagnosis?.Description, order.CustomerComplaint, order.Mileage,
            approval.Status, approval.CreatedAt, approval.ExpiresAt, approval.RespondedAt,
            approval.CustomerComment, services, parts,
            approval.EstimateTotal, snapshot.Select(x => new PublicApprovalItemResponse(
                x.Id, x.Description, x.Type, x.Quantity, x.UnitPrice, x.Total,
                approval.Status == EstimateApprovalStatus.Pending ? null : approvedIds.Contains(x.Id))).ToArray())
        {
            ApprovedTotal = approval.ApprovedTotal,
            ApprovalType = approval.Status == EstimateApprovalStatus.PartiallyApproved ? "Partial" :
                approval.Status == EstimateApprovalStatus.Approved ? "Total" : null,
            DeclaredApproverName = approval.CustomerName,
            DeclaredApproverDocumentMasked = MaskDocument(approval.CustomerDocument)
        };
    }

    public async Task<EstimateApprovalStatus?> RespondAsync(string token, bool approve,
        ApprovalDecisionRequest request, string? clientIp, string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var resolved = await ResolveAsync(token, cancellationToken);
        if (resolved is null) return null;
        var (order, approval) = resolved.Value;
        if (approval.Status != EstimateApprovalStatus.Pending) return approval.Status;
        if (approval.IsExpired(DateTimeOffset.UtcNow)) throw new InvalidOperationException("Este link de aprovação expirou.");
        if (order.Estimate?.UpdatedAt != approval.EstimateUpdatedAt)
            throw new InvalidOperationException("Este orçamento foi alterado e o link não é mais válido.");
        var name = request.CustomerName?.Trim() ?? string.Empty;
        if (name.Length < 5 || name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 2)
            throw new InvalidOperationException("Informe seu nome completo.");
        var document = BrazilianDocument.Normalize(request.CustomerDocument);
        if (document.Length != 11 || !BrazilianDocument.IsValid(document))
            throw new InvalidOperationException("Informe um CPF válido.");
        var phone = BrazilianPhone.Normalize(request.CustomerPhone);
        if (!BrazilianPhone.IsValid(phone))
            throw new InvalidOperationException("Informe um telefone válido.");
        if (approve) order.ApproveEstimate(approval.Id, name, document, phone,
            request.ApprovedItemIds ?? [], request.Comment, clientIp, userAgent, DateTimeOffset.UtcNow);
        else order.RejectEstimate(approval.Id, name, document, phone, request.Comment,
            clientIp, userAgent, DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return approval.Status;
    }

    private static EstimateApprovalSnapshotItem[] ReadSnapshot(ServiceOrderEstimateApprovalEntity approval,
        ServiceOrderEstimateEntity estimate)
    {
        var snapshot = JsonSerializer.Deserialize<EstimateApprovalSnapshotItem[]>(approval.EstimateSnapshotJson) ?? [];
        return snapshot.Length > 0 ? snapshot : estimate.Items.Select(x => new EstimateApprovalSnapshotItem(
            x.Id, x.Description, x.Type, x.Quantity, x.UnitPrice, x.Total)).ToArray();
    }

    private static HashSet<Guid> ReadApprovedIds(ServiceOrderEstimateApprovalEntity approval) =>
        (JsonSerializer.Deserialize<Guid[]>(approval.ApprovedItemIdsJson ?? "[]") ?? []).ToHashSet();

    private static string? MaskDocument(string? document) => document is { Length: 11 }
        ? $"***.{document.Substring(3, 3)}.{document.Substring(6, 3)}-**"
        : null;

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
