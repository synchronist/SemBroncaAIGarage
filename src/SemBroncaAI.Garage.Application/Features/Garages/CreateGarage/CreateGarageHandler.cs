using GarageEntity = global::SemBroncaAI.Garage.Domain.Entities.Garage;
using SemBroncaAI.Garage.Domain.Interfaces;

namespace SemBroncaAI.Garage.Application.Features.Garages.CreateGarage;

public sealed class CreateGarageHandler
{
    private readonly IGarageRepository _garageRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateGarageHandler(
        IGarageRepository garageRepository,
        IUnitOfWork unitOfWork)
    {
        _garageRepository = garageRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateGarageResponse> HandleAsync(
        CreateGarageCommand command,
        CancellationToken cancellationToken = default)
    {
        if (await _garageRepository.ExistsByDocumentAsync(command.Document, cancellationToken))
            throw new InvalidOperationException("Já existe uma oficina cadastrada com este documento.");

        var garage = new GarageEntity(
            command.Name,
            command.Document,
            command.Phone,
            command.Email);

        await _garageRepository.AddAsync(garage, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateGarageResponse(
            garage.Id,
            garage.Name,
            garage.Document,
            garage.Phone,
            garage.Email,
            garage.Active,
            garage.CreatedAt);
    }
}