namespace SemBroncaAI.Garage.Application.Features.Garages.UpdateGarageSettings;

public sealed record UpdateGarageSettingsCommand(
    string Name, string Document, string Phone, string Email,
    string? PostalCode, string? Street, string? Number, string? Complement,
    string? Neighborhood, string? City, string? State, string? PrimaryColor);
