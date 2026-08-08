using SemBroncaAI.Garage.Domain.Common;
using SemBroncaAI.Garage.Domain.Entities.Garage;
using SemBroncaAI.Garage.Domain.Entities.Vehicle;

namespace SemBroncaAI.Garage.Domain.Entities.ServiceOrder;

public sealed class ServiceOrderEntity : Entity
{
    private readonly List<ServiceOrderHistoryEntity> _history = [];

    public Guid GarageId { get; private set; }

    public Guid VehicleId { get; private set; }
    public ServiceOrderDiagnosisEntity? Diagnosis { get; private set; }
    public ServiceOrderEstimateEntity? Estimate { get; private set; }

    public int Number { get; private set; }

    public ServiceOrderStatus Status { get; private set; }

    public string CustomerComplaint { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public GarageEntity Garage { get; private set; } = default!;

    public VehicleEntity Vehicle { get; private set; } = default!;

    public IReadOnlyCollection<ServiceOrderHistoryEntity> History =>
        _history.AsReadOnly();

    private ServiceOrderEntity()
    {
    }

    public ServiceOrderEntity(
        Guid garageId,
        Guid vehicleId,
        int number,
        string customerComplaint,
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

    public void SendForApproval(Guid? actorId = null)
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

        ChangeStatus(
            ServiceOrderStatus.WaitingApproval,
            ServiceOrderMessages.SentForApproval,
            actorId);
    }

    public void StartService(Guid? actorId = null)
    {
        EnsureStatus(ServiceOrderStatus.WaitingApproval);

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

        Estimate.Update(items);
    }
}
