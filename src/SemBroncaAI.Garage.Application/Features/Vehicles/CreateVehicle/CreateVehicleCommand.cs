namespace SemBroncaAI.Garage.Application.Features.Vehicles.CreateVehicle;

public sealed record CreateVehicleCommand(
    Guid GarageId,
    Guid CustomerId,
    string Plate,
    string Brand,
    string Model,
    string Version,
    int Year,
    string Color,
    string Fuel,
    int Mileage);