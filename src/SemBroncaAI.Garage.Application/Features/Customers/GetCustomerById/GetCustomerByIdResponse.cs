namespace SemBroncaAI.Garage.Application.Features.Customers.GetCustomerById;

public sealed record GetCustomerByIdResponse(Guid Id, Guid GarageId, string Name, string Document, string Phone, string Email, bool Active, DateTime CreatedAt, IReadOnlyCollection<CustomerVehicleResponse> Vehicles);
public sealed record CustomerVehicleResponse(Guid Id, string Plate, string Brand, string Model, string Version, int Year, string Color, string Fuel, int Mileage, bool Active);
