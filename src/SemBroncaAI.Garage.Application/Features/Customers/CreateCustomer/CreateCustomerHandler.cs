using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Domain.Entities.Customer;
using SemBroncaAI.Garage.Domain.Interfaces;

namespace SemBroncaAI.Garage.Application.Features.Customers.CreateCustomer;

public sealed class CreateCustomerHandler
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IGarageRepository _garageRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCustomerHandler(
        ICustomerRepository customerRepository,
        IGarageRepository garageRepository,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _garageRepository = garageRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateCustomerResponse> HandleAsync(
        CreateCustomerCommand command,
        CancellationToken cancellationToken = default)
    {
        var garage = await _garageRepository.GetByIdAsync(
            command.GarageId,
            cancellationToken);

        if (garage is null)
        {
            throw new InvalidOperationException(
                "Oficina não encontrada.");
        }

        var documentAlreadyExists =
            await _customerRepository.ExistsByDocumentAsync(
                command.GarageId,
                command.Document.Trim(),
                null,
                cancellationToken);

        if (documentAlreadyExists)
        {
            throw new InvalidOperationException(
                "Já existe um cliente com esse documento nesta oficina.");
        }

        var customer = new CustomerEntity(
            command.GarageId,
            command.Name,
            command.Document,
            command.Phone,
            command.Email);

        await _customerRepository.AddAsync(
            customer,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateCustomerResponse(
            customer.Id,
            customer.GarageId,
            customer.Name,
            customer.Document,
            customer.Phone,
            customer.Email,
            customer.Active,
            customer.CreatedAt);
    }
}
