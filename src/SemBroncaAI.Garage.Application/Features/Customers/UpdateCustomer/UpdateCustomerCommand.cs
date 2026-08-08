namespace SemBroncaAI.Garage.Application.Features.Customers.UpdateCustomer;

public sealed record UpdateCustomerCommand(Guid GarageId, string Name, string Document, string Phone, string Email);
public sealed record UpdateCustomerResponse(Guid Id, Guid GarageId, string Name, string Document, string Phone, string Email, bool Active, DateTime CreatedAt);
