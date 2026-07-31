namespace SemBroncaAI.Garage.Application.Features.Garages.CreateGarage;

public sealed record CreateGarageCommand(
    string Name,
    string Document,
    string Phone,
    string Email);