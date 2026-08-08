namespace SemBroncaAI.Garage.Application.Features.Vehicles.ListVehicles;
public sealed record ListVehiclesQuery(Guid GarageId, string? Search, int Page = 1, int PageSize = 20);
public sealed record ListVehiclesResponse(int Page, int PageSize, int TotalItems, int TotalPages, IReadOnlyCollection<ListVehiclesItem> Items);
public sealed record ListVehiclesItem(Guid Id, string Plate, string Brand, string Model, string Version, int Year, string Color, string Fuel, int Mileage, Guid CustomerId, string CustomerName, bool Active);
