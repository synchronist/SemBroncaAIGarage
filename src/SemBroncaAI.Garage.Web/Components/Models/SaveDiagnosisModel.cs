namespace SemBroncaAI.Garage.Web.Models;

public sealed record SaveDiagnosisRequest(
    string Description,
    string? InternalNotes);

public sealed record SaveDiagnosisResponse(
    Guid ServiceOrderId,
    string Description,
    string InternalNotes,
    DateTimeOffset UpdatedAt);