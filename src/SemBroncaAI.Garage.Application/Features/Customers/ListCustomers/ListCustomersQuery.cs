namespace SemBroncaAI.Garage.Application.Features.Customers.ListCustomers;

public sealed record ListCustomersQuery(Guid GarageId, string? Search, int Page = 1, int PageSize = 20);
public sealed record ListCustomersResponse(int Page, int PageSize, int TotalItems, int TotalPages, IReadOnlyCollection<ListCustomersItem> Items);
public sealed record ListCustomersItem(Guid Id, string Name, string Document, string Phone, string Email, bool Active, DateTime CreatedAt, int VehicleCount);
