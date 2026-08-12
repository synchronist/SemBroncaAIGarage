namespace SemBroncaAI.Garage.Application.Features.Garages.GetGarageSettings;

public sealed record GetGarageSettingsResponse(
    Guid Id, string Name, string Document, string Phone, string Email,
    string? PostalCode, string? Street, string? Number, string? Complement,
    string? Neighborhood, string? City, string? State, string? LogoStorageKey, string? PrimaryColor,
    bool Active, DateTime CreatedAt);
