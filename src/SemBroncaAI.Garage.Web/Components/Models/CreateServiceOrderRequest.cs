namespace SemBroncaAI.Garage.Web.Models;

public sealed record CreateServiceOrderRequest(
    Guid GarageId,
    Guid VehicleId,
    string CustomerComplaint);