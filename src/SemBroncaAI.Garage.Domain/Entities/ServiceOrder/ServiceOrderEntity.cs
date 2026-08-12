using SemBroncaAI.Garage.Domain.Common;
using SemBroncaAI.Garage.Domain.Entities.Garage;
using SemBroncaAI.Garage.Domain.Entities.Vehicle;

namespace SemBroncaAI.Garage.Domain.Entities.ServiceOrder;

public sealed class ServiceOrderEntity : Entity
{
    private readonly List<ServiceOrderHistoryEntity> _history = [];
    private readonly List<ServiceOrderEstimateApprovalEntity> _estimateApprovals = [];

    public Guid GarageId { get; private set; }

    public Guid VehicleId { get; private set; }
    public ServiceOrderDiagnosisEntity? Diagnosis { get; private set; }
    public ServiceOrderEstimateEntity? Estimate { get; private set; }

    public int Number { get; private set; }

    public ServiceOrderStatus Status { get; private set; }

    public string CustomerComplaint { get; private set; } = string.Empty;

    public int? Mileage { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public GarageEntity Garage { get; private set; } = default!;

    public VehicleEntity Vehicle { get; private set; } = default!;

    public IReadOnlyCollection<ServiceOrderHistoryEntity> History =>
        _history.AsReadOnly();
    public IReadOnlyCollection<ServiceOrderEstimateApprovalEntity> EstimateApprovals => _estimateApprovals.AsReadOnly();
    public ServiceOrderEstimateApprovalEntity? CurrentEstimateApproval => _estimateApprovals.OrderByDescending(x => x.CreatedAt).FirstOrDefault();

    private ServiceOrderEntity()
    {
    }

    public ServiceOrderEntity(
        Guid garageId,
        Guid vehicleId,
        int number,
        string customerComplaint,
        int mileage,
        Guid? actorId = null)
    {
        GarageId = Guard.AgainstEmpty(
            garageId,
            nameof(garageId));

        VehicleId = Guard.AgainstEmpty(
            vehicleId,
            nameof(vehicleId));

        Number = Guard.AgainstZeroOrNegative(
            number,
            nameof(number));

        CustomerComplaint = Guard.AgainstNullOrWhiteSpace(
            customerComplaint,
            nameof(customerComplaint));

        if (mileage < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mileage), "A quilometragem não pode ser negativa.");
        }

        Mileage = mileage;

        Status = ServiceOrderStatus.Received;
        CreatedAt = DateTimeOffset.UtcNow;

        AddHistory(
            previousStatus: null,
            currentStatus: ServiceOrderStatus.Received,
            description: ServiceOrderMessages.Created,
            actorId);
    }

    public void StartDiagnosis(Guid? actorId = null)
    {
        EnsureStatus(ServiceOrderStatus.Received);

        ChangeStatus(
            ServiceOrderStatus.Diagnosis,
            ServiceOrderMessages.DiagnosisStarted,
            actorId);
    }

    public ServiceOrderEstimateApprovalEntity SendForApproval(string tokenHash, string protectedToken,
        DateTimeOffset expiresAt, DateTimeOffset now, Guid? actorId = null)
    {
        EnsureStatus(ServiceOrderStatus.Diagnosis);

        if (Diagnosis is null)
        {
            throw new InvalidOperationException(
                "Registre o diagnóstico antes de enviar a ordem para aprovação.");
        }

        if (Estimate is null || !Estimate.IsValid)
        {
            throw new InvalidOperationException(
                "Registre um orçamento válido antes de enviar a ordem para aprovação.");
        }

        foreach (var approval in _estimateApprovals) approval.Invalidate(now);
        var request = new ServiceOrderEstimateApprovalEntity(Id, tokenHash, protectedToken,
            expiresAt, Estimate.UpdatedAt, Estimate.Total, now);
        _estimateApprovals.Add(request);

        ChangeStatus(
            ServiceOrderStatus.WaitingApproval,
            ServiceOrderMessages.SentForApproval,
            actorId);
        return request;
    }

    public void StartService(Guid? actorId = null)
    {
        EnsureStatus(ServiceOrderStatus.WaitingApproval);

        if (CurrentEstimateApproval?.Status != EstimateApprovalStatus.Approved)
            throw new InvalidOperationException("O serviço só pode ser iniciado após a aprovação do orçamento pelo cliente.");

        ChangeStatus(
            ServiceOrderStatus.InProgress,
            ServiceOrderMessages.ServiceStarted,
            actorId);
    }

    public void WaitForParts(Guid? actorId = null)
    {
        EnsureStatus(ServiceOrderStatus.InProgress);

        ChangeStatus(
            ServiceOrderStatus.WaitingParts,
            ServiceOrderMessages.WaitingParts,
            actorId);
    }

    public void ResumeService(Guid? actorId = null)
    {
        EnsureStatus(ServiceOrderStatus.WaitingParts);

        ChangeStatus(
            ServiceOrderStatus.InProgress,
            ServiceOrderMessages.ServiceResumed,
            actorId);
    }

    public void Finish(Guid? actorId = null)
    {
        EnsureStatus(ServiceOrderStatus.InProgress);

        ChangeStatus(
            ServiceOrderStatus.Finished,
            ServiceOrderMessages.ServiceFinished,
            actorId);
    }

    public void Deliver(Guid? actorId = null)
    {
        EnsureStatus(ServiceOrderStatus.Finished);

        ChangeStatus(
            ServiceOrderStatus.Delivered,
            ServiceOrderMessages.VehicleDelivered,
            actorId);
    }

    public void Cancel(Guid? actorId = null)
    {
        EnsureStatus(
            ServiceOrderStatus.Received,
            ServiceOrderStatus.Diagnosis,
            ServiceOrderStatus.WaitingApproval,
            ServiceOrderStatus.InProgress,
            ServiceOrderStatus.WaitingParts,
            ServiceOrderStatus.Finished);

        foreach (var approval in _estimateApprovals) approval.Invalidate(DateTimeOffset.UtcNow);

        ChangeStatus(
            ServiceOrderStatus.Cancelled,
            ServiceOrderMessages.Cancelled,
            actorId);
    }

    private void ChangeStatus(
        ServiceOrderStatus newStatus,
        string description,
        Guid? actorId)
    {
        var previousStatus = Status;

        Status = newStatus;

        AddHistory(
            previousStatus,
            newStatus,
            description,
            actorId);
    }

    private void AddHistory(
        ServiceOrderStatus? previousStatus,
        ServiceOrderStatus currentStatus,
        string description,
        Guid? actorId)
    {
        var history = new ServiceOrderHistoryEntity(
            previousStatus,
            currentStatus,
            description,
            actorId);

        _history.Add(history);
    }

    private void EnsureStatus(
        params ServiceOrderStatus[] allowedStatuses)
    {
        if (!allowedStatuses.Contains(Status))
        {
            throw new InvalidOperationException(
                $"A ordem de serviço deve estar em um dos estados: " +
                $"{string.Join(", ", allowedStatuses)}.");
        }
    }
    public void SaveDiagnosis(
    string description,
    string? internalNotes = null,
    Guid? actorId = null)
    {
        EnsureStatus(ServiceOrderStatus.Diagnosis);

        if (Diagnosis is null)
        {
            Diagnosis = new ServiceOrderDiagnosisEntity(
                Id,
                description,
                internalNotes,
                actorId);

            return;
        }

        Diagnosis.Update(
            description,
            internalNotes,
            actorId);
    }

    public void SaveEstimate(
        IEnumerable<ServiceOrderEstimateItemData> items)
    {
        EnsureStatus(ServiceOrderStatus.Diagnosis);

        if (Diagnosis is null)
        {
            throw new InvalidOperationException(
                "Registre o diagnóstico antes de montar o orçamento.");
        }

        if (Estimate is null)
        {
            Estimate = new ServiceOrderEstimateEntity(Id, items);
            return;
        }

        foreach (var approval in _estimateApprovals) approval.Invalidate(DateTimeOffset.UtcNow);
        Estimate.Update(items);
    }

    public void ApproveEstimate(Guid approvalId, string? customerName, DateTimeOffset now)
    {
        EnsureStatus(ServiceOrderStatus.WaitingApproval);
        var approval = _estimateApprovals.SingleOrDefault(x => x.Id == approvalId)
            ?? throw new InvalidOperationException("Solicitação de aprovação não encontrada.");
        approval.Approve(customerName, now);
    }

    public void RejectEstimate(Guid approvalId, string? customerName, string? comment, DateTimeOffset now)
    {
        EnsureStatus(ServiceOrderStatus.WaitingApproval);
        var approval = _estimateApprovals.SingleOrDefault(x => x.Id == approvalId)
            ?? throw new InvalidOperationException("Solicitação de aprovação não encontrada.");
        approval.Reject(customerName, comment, now);
    }

    public void ReviseRejectedEstimate(Guid? actorId = null)
    {
        EnsureStatus(ServiceOrderStatus.WaitingApproval);
        var current = CurrentEstimateApproval
            ?? throw new InvalidOperationException("Não há solicitação de aprovação para revisar.");
        if (current.Status != EstimateApprovalStatus.Rejected && !current.IsExpired(DateTimeOffset.UtcNow))
            throw new InvalidOperationException("Somente um orçamento recusado ou expirado pode ser revisado.");
        foreach (var approval in _estimateApprovals) approval.Invalidate(DateTimeOffset.UtcNow);
        ChangeStatus(ServiceOrderStatus.Diagnosis, "Orçamento reaberto para revisão.", actorId);
    }
}
