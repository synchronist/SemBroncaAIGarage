namespace SemBroncaAI.Garage.Web.Models;

public sealed record CustomerListModel(int Page, int PageSize, int TotalItems, int TotalPages, IReadOnlyCollection<CustomerListItemModel> Items);
public sealed record CustomerListItemModel(Guid Id, string Name, string Document, string Phone, string Email, bool Active, DateTime CreatedAt, int VehicleCount);
public sealed record CustomerDetailsModel(Guid Id, Guid GarageId, string Name, string Document, string Phone, string Email, bool Active, DateTime CreatedAt, IReadOnlyCollection<CustomerVehicleModel> Vehicles);
public sealed record CustomerVehicleModel(Guid Id, string Plate, string Brand, string Model, string Version, int Year, string Color, string Fuel, int Mileage, bool Active);
public sealed record SaveCustomerRequest(Guid GarageId, string Name, string Document, string Phone, string Email);
public sealed record SaveCustomerResponse(Guid Id, Guid GarageId, string Name, string Document, string Phone, string Email, bool Active, DateTime CreatedAt);
