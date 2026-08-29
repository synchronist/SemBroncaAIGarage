using SemBroncaAI.Garage.Domain.Common;
using SemBroncaAI.Garage.Domain.Entities.Garage;
using SemBroncaAI.Garage.Domain.Entities.Vehicle;
using System.Text.Json;

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

    public DateTimeOffset? ArchivedAt { get; private set; }

    public DateTimeOffset? DigitalApprovalWaivedAt { get; private set; }

    public long Version { get; private set; }

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

        CustomerComplaint = Guard.RequiredWithMaximumLength(
            customerComplaint, FieldLengthLimits.CustomerComplaint, nameof(customerComplaint));

        if (mileage < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mileage), "A quilometragem não pode ser negativa.");
        }

        Mileage = mileage;

        Status = ServiceOrderStatus.Received;
        CreatedAt = DateTimeOffset.UtcNow;
        Version = 1;

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
        var snapshot = JsonSerializer.Serialize(Estimate.Items.Select(item => new EstimateApprovalSnapshotItem(
            item.Id, item.Description, item.Type, item.Quantity, item.UnitPrice, item.Total)));
        var request = new ServiceOrderEstimateApprovalEntity(Id, tokenHash, protectedToken,
            expiresAt, Estimate.UpdatedAt, Estimate.Total, snapshot, now);
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

        if (CurrentEstimateApproval?.Status is not (EstimateApprovalStatus.Approved or EstimateApprovalStatus.PartiallyApproved) &&
            DigitalApprovalWaivedAt is null)
            throw new InvalidOperationException("O serviço só pode ser iniciado após a aprovação do orçamento pelo cliente.");

        var items = Estimate?.Items ?? [];
        if (DigitalApprovalWaivedAt is not null)
        {
            if (items.Any(item => item.AuthorizationStatus != EstimateItemAuthorizationStatus.DigitalApprovalWaived))
                throw new InvalidOperationException("O escopo dispensado do aceite digital está inconsistente.");
        }
        else if (CurrentEstimateApproval?.Status == EstimateApprovalStatus.Approved)
        {
            if (items.Any(item => item.AuthorizationStatus != EstimateItemAuthorizationStatus.CustomerAuthorized))
                throw new InvalidOperationException("A autorização integral dos itens está inconsistente.");
        }
        else if (!items.Any(item => item.AuthorizationStatus == EstimateItemAuthorizationStatus.CustomerAuthorized) ||
                 items.Any(item => item.AuthorizationStatus is not (EstimateItemAuthorizationStatus.CustomerAuthorized or EstimateItemAuthorizationStatus.CustomerNotAuthorized)))
        {
            throw new InvalidOperationException("A autorização parcial dos itens está inconsistente.");
        }

        ChangeStatus(
            ServiceOrderStatus.InProgress,
            CurrentEstimateApproval?.Status == EstimateApprovalStatus.PartiallyApproved
                ? ServiceOrderMessages.PartiallyApprovedServiceStarted
                : ServiceOrderMessages.ServiceStarted,
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

    public void Archive(DateTimeOffset now)
    {
        if (ArchivedAt is not null) return;
        if (Status is not (ServiceOrderStatus.Delivered or ServiceOrderStatus.Cancelled))
            throw new InvalidOperationException("Somente ordens entregues ou canceladas podem ser arquivadas.");
        ArchivedAt = now;
    }

    public void Restore()
    {
        ArchivedAt = null;
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
        DigitalApprovalWaivedAt = null;
        Estimate.Update(items);
    }

    public void WaiveDigitalApproval(DateTimeOffset now, Guid? actorId = null)
    {
        EnsureStatus(ServiceOrderStatus.Diagnosis, ServiceOrderStatus.WaitingApproval);

        if (CurrentEstimateApproval is { IsActive: true, Status: not EstimateApprovalStatus.Pending })
            throw new InvalidOperationException("Uma decisão do cliente já foi registrada para este orçamento.");

        if (Diagnosis is null)
            throw new InvalidOperationException("Registre o diagnóstico antes de dispensar o aceite digital.");
        if (Estimate is null || !Estimate.IsValid)
            throw new InvalidOperationException("Registre um orçamento válido antes de dispensar o aceite digital.");

        foreach (var approval in _estimateApprovals) approval.Invalidate(now);
        foreach (var item in Estimate.Items) item.WaiveDigitalApproval();
        DigitalApprovalWaivedAt = now;
        ChangeStatus(ServiceOrderStatus.WaitingApproval, ServiceOrderMessages.DigitalApprovalWaived, actorId);
    }

    public void ApproveEstimate(Guid approvalId, string customerName, string customerDocument,
        string customerPhone, IReadOnlyCollection<Guid> approvedItemIds, string? comment,
        string? clientIp, string? userAgent, DateTimeOffset now)
    {
        EnsureStatus(ServiceOrderStatus.WaitingApproval);
        var approval = _estimateApprovals.SingleOrDefault(x => x.Id == approvalId)
            ?? throw new InvalidOperationException("Solicitação de aprovação não encontrada.");
        var items = JsonSerializer.Deserialize<EstimateApprovalSnapshotItem[]>(approval.EstimateSnapshotJson) ?? [];
        var selected = approvedItemIds.Distinct().ToHashSet();
        if (selected.Count == 0 || selected.Any(id => items.All(item => item.Id != id)))
            throw new InvalidOperationException("Selecione ao menos um item válido para aprovação.");
        var approvedTotal = items.Where(item => selected.Contains(item.Id)).Sum(item => item.Total);
        if (Estimate is null || Estimate.UpdatedAt != approval.EstimateUpdatedAt)
            throw new InvalidOperationException("O orçamento foi alterado e esta aprovação não pode ser aplicada.");
        foreach (var item in Estimate.Items) item.SetCustomerAuthorization(selected.Contains(item.Id));
        approval.Approve(customerName, customerDocument, customerPhone,
            JsonSerializer.Serialize(selected), approvedTotal, selected.Count < items.Length,
            comment, clientIp, userAgent, now);
    }

    public void RejectEstimate(Guid approvalId, string customerName, string customerDocument,
        string customerPhone, string? comment, string? clientIp, string? userAgent, DateTimeOffset now)
    {
        EnsureStatus(ServiceOrderStatus.WaitingApproval);
        var approval = _estimateApprovals.SingleOrDefault(x => x.Id == approvalId)
            ?? throw new InvalidOperationException("Solicitação de aprovação não encontrada.");
        if (Estimate is null || Estimate.UpdatedAt != approval.EstimateUpdatedAt)
            throw new InvalidOperationException("O orçamento foi alterado e esta decisão não pode ser aplicada.");
        foreach (var item in Estimate.Items) item.SetCustomerAuthorization(false);
        approval.Reject(customerName, customerDocument, customerPhone, comment, clientIp, userAgent, now);
    }

    public void ReviseRejectedEstimate(Guid? actorId = null)
    {
        EnsureStatus(ServiceOrderStatus.WaitingApproval);
        var current = CurrentEstimateApproval
            ?? throw new InvalidOperationException("Não há solicitação de aprovação para revisar.");
        if (current.Status != EstimateApprovalStatus.Rejected && !current.IsExpired(DateTimeOffset.UtcNow))
            throw new InvalidOperationException("Somente um orçamento recusado ou expirado pode ser revisado.");
        foreach (var approval in _estimateApprovals) approval.Invalidate(DateTimeOffset.UtcNow);
        DigitalApprovalWaivedAt = null;
        ChangeStatus(ServiceOrderStatus.Diagnosis, "Orçamento reaberto para revisão.", actorId);
    }
}
