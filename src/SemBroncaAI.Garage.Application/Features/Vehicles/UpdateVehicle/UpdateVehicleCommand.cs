namespace SemBroncaAI.Garage.Application.Features.Vehicles.UpdateVehicle;
public sealed record UpdateVehicleCommand(Guid GarageId, Guid CustomerId, string Plate, string Brand, string Model, string Version, int Year, string Color, string Fuel, int Mileage);
public sealed record UpdateVehicleResponse(Guid Id, Guid GarageId, Guid CustomerId, string Plate, string Brand, string Model, string Version, int Year, string Color, string Fuel, int Mileage, bool Active, DateTime CreatedAt);
