namespace SemBroncaAI.Garage.Application.Features.Garages.GetGarageSettings;

public sealed record GetGarageContextResponse(
    string Name, string Document, string Phone, string Email,
    string? PostalCode, string? Street, string? Number, string? Complement,
    string? Neighborhood, string? City, string? State,
    string? LogoStorageKey, string? PrimaryColor);

public sealed record GetGarageBrandingResponse(
    string Name, string? City, string? State, string? LogoStorageKey, string? PrimaryColor);
