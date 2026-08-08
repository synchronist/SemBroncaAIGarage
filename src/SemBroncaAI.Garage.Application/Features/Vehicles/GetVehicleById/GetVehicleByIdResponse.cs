using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;
namespace SemBroncaAI.Garage.Application.Features.Vehicles.GetVehicleById;
public sealed record GetVehicleByIdResponse(Guid Id, Guid GarageId, string Plate, string Brand, string Model, string Version, int Year, string Color, string Fuel, int Mileage, bool Active, DateTime CreatedAt, VehicleCustomerResponse Customer, IReadOnlyCollection<VehicleServiceOrderResponse> ServiceOrders);
public sealed record VehicleCustomerResponse(Guid Id, string Name, string Document, string Phone, string Email);
public sealed record VehicleServiceOrderResponse(Guid Id, int Number, ServiceOrderStatus Status, string CustomerComplaint, int? Mileage, DateTimeOffset CreatedAt);
