namespace SemBroncaAI.Garage.Application.Features.Lookup;

public sealed record LookupResultResponse(
    Guid VehicleId,
    Guid CustomerId,
    Guid GarageId,
    string Plate,
    string Brand,
    string Model,
    string Version,
    int Year,
    string Color,
    string Fuel,
    int Mileage,
    string CustomerName,
    string CustomerPhone,
    string CustomerDocument,
    string CustomerEmail);