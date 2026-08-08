namespace SemBroncaAI.Garage.Application.Features.ServiceOrders.SaveDiagnosis;

public sealed record SaveDiagnosisCommand(
    string Description,
    string? InternalNotes);