namespace SemBroncaAI.Garage.Web.Models;

public sealed record CreateServiceOrderRequest(
    Guid VehicleId,
    string CustomerComplaint,
    int Mileage);
