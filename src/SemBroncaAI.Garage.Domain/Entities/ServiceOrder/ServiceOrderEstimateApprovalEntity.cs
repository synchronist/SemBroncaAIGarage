using SemBroncaAI.Garage.Domain.Common;

namespace SemBroncaAI.Garage.Domain.Entities.ServiceOrder;

public sealed class ServiceOrderEstimateApprovalEntity : Entity
{
    public Guid ServiceOrderId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public string ProtectedToken { get; private set; } = string.Empty;
    public EstimateApprovalStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RespondedAt { get; private set; }
    public DateTimeOffset? InvalidatedAt { get; private set; }
    public string? CustomerName { get; private set; }
    public string? CustomerComment { get; private set; }
    public DateTimeOffset EstimateUpdatedAt { get; private set; }
    public decimal EstimateTotal { get; private set; }

    private ServiceOrderEstimateApprovalEntity() { }

    internal ServiceOrderEstimateApprovalEntity(Guid serviceOrderId, string tokenHash,
        string protectedToken, DateTimeOffset expiresAt, DateTimeOffset estimateUpdatedAt,
        decimal estimateTotal, DateTimeOffset now)
    {
        ServiceOrderId = Guard.AgainstEmpty(serviceOrderId, nameof(serviceOrderId));
        TokenHash = Guard.AgainstNullOrWhiteSpace(tokenHash, nameof(tokenHash));
        ProtectedToken = Guard.AgainstNullOrWhiteSpace(protectedToken, nameof(protectedToken));
        if (expiresAt <= now) throw new ArgumentOutOfRangeException(nameof(expiresAt));
        Status = EstimateApprovalStatus.Pending;
        CreatedAt = now;
        ExpiresAt = expiresAt;
        EstimateUpdatedAt = estimateUpdatedAt;
        EstimateTotal = estimateTotal;
    }

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;
    public bool IsActive => InvalidatedAt is null;

    internal void Approve(string? customerName, DateTimeOffset now) => Respond(EstimateApprovalStatus.Approved, customerName, null, now);
    internal void Reject(string? customerName, string? comment, DateTimeOffset now) => Respond(EstimateApprovalStatus.Rejected, customerName, comment, now);

    internal void Invalidate(DateTimeOffset now)
    {
        InvalidatedAt ??= now;
    }

    private void Respond(EstimateApprovalStatus decision, string? customerName, string? comment, DateTimeOffset now)
    {
        if (!IsActive || Status != EstimateApprovalStatus.Pending)
            throw new InvalidOperationException("Este orçamento já recebeu uma resposta ou não está mais ativo.");
        if (IsExpired(now)) throw new InvalidOperationException("Este link de aprovação expirou.");
        if (customerName?.Trim().Length > 200) throw new ArgumentException("O nome deve ter no máximo 200 caracteres.", nameof(customerName));
        if (comment?.Trim().Length > 1000) throw new ArgumentException("A observação deve ter no máximo 1000 caracteres.", nameof(comment));
        Status = decision;
        RespondedAt = now;
        CustomerName = string.IsNullOrWhiteSpace(customerName) ? null : customerName.Trim();
        CustomerComment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
    }
}
