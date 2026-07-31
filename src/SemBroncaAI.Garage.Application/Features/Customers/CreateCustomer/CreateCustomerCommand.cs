namespace SemBroncaAI.Garage.Application.Features.Customers.CreateCustomer;

public sealed record CreateCustomerCommand(
    Guid GarageId,
    string Name,
    string Document,
    string Phone,
    string Email);