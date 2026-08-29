using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Domain.Interfaces;
using SemBroncaAI.Garage.Domain.Common;

namespace SemBroncaAI.Garage.Application.Features.Customers.UpdateCustomer;

public sealed class UpdateCustomerHandler
{
    private readonly ICustomerRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    public UpdateCustomerHandler(ICustomerRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateCustomerResponse> HandleAsync(Guid id, UpdateCustomerCommand command, CancellationToken cancellationToken = default)
    {
        var customer = await _repository.GetByIdAsync(id, command.GarageId, cancellationToken)
            ?? throw new InvalidOperationException("Cliente não encontrado.");

        var document = BrazilianDocument.Normalize(command.Document);
        if (document.Length > 0 && await _repository.ExistsByDocumentAsync(command.GarageId, document, id, cancellationToken))
        {
            throw new InvalidOperationException("Já existe um cliente com esse documento nesta oficina.");
        }

        customer.Update(command.Name, command.Document, command.Phone, command.Email);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new UpdateCustomerResponse(customer.Id, customer.GarageId, customer.Name, customer.Document, customer.Phone, customer.Email, customer.Active, customer.CreatedAt);
    }
}
