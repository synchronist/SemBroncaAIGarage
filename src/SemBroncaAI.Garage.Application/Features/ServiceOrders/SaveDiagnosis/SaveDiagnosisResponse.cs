namespace SemBroncaAI.Garage.Application.Features.ServiceOrders.SaveDiagnosis;

public sealed record SaveDiagnosisResponse(
    Guid ServiceOrderId,
    string Description,
    string InternalNotes,
    DateTimeOffset UpdatedAt);