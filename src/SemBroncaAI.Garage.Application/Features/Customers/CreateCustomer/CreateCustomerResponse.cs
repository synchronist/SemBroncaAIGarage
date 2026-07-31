namespace SemBroncaAI.Garage.Application.Features.Customers.CreateCustomer;

public sealed record CreateCustomerResponse(
    Guid Id,
    Guid GarageId,
    string Name,
    string Document,
    string Phone,
    string Email,
    bool Active,
    DateTime CreatedAt);