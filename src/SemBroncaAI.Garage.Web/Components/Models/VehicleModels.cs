namespace SemBroncaAI.Garage.Web.Models;
public sealed record VehicleListModel(int Page, int PageSize, int TotalItems, int TotalPages, IReadOnlyCollection<VehicleListItemModel> Items);
public sealed record VehicleListItemModel(Guid Id, string Plate, string Brand, string Model, string Version, int Year, string Color, string Fuel, int Mileage, Guid CustomerId, string CustomerName, bool Active);
public sealed record VehicleDetailsModel(Guid Id, Guid GarageId, string Plate, string Brand, string Model, string Version, int Year, string Color, string Fuel, int Mileage, bool Active, DateTime CreatedAt, VehicleCustomerModel Customer, IReadOnlyCollection<VehicleServiceOrderModel> ServiceOrders);
public sealed record VehicleCustomerModel(Guid Id, string Name, string Document, string Phone, string Email);
public sealed record VehicleServiceOrderModel(Guid Id, int Number, string Status, string CustomerComplaint, int? Mileage, DateTimeOffset CreatedAt);
public sealed record SaveVehicleRequest(Guid GarageId, Guid CustomerId, string Plate, string Brand, string Model, string Version, int Year, string Color, string Fuel, int Mileage);
public sealed record SaveVehicleResponse(Guid Id, Guid GarageId, Guid CustomerId, string Plate, string Brand, string Model, string Version, int Year, string Color, string Fuel, int Mileage, bool Active, DateTime CreatedAt);
