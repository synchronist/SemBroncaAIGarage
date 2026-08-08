namespace SemBroncaAI.Garage.Web.Models;

public sealed record LookupResult(
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