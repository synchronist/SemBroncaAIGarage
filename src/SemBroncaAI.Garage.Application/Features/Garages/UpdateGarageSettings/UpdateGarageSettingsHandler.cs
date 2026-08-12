using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Application.Features.Garages.GetGarageSettings;
using SemBroncaAI.Garage.Domain.Interfaces;

namespace SemBroncaAI.Garage.Application.Features.Garages.UpdateGarageSettings;

public sealed class UpdateGarageSettingsHandler(IGarageRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<GetGarageSettingsResponse> HandleAsync(Guid garageId, UpdateGarageSettingsCommand command, CancellationToken cancellationToken = default)
    {
        var garage = await repository.GetForUpdateAsync(garageId, cancellationToken)
            ?? throw new InvalidOperationException("Oficina não encontrada.");
        var document = command.Document.Trim();
        if (await repository.ExistsByDocumentAsync(document, garageId, cancellationToken))
            throw new InvalidOperationException("Já existe uma oficina cadastrada com este documento.");

        garage.UpdateSettings(command.Name, document, command.Phone, command.Email, command.PostalCode,
            command.Street, command.Number, command.Complement, command.Neighborhood, command.City, command.State);
        garage.UpdateBranding(garage.LogoStorageKey, command.PrimaryColor);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new(garage.Id, garage.Name, garage.Document, garage.Phone, garage.Email, garage.PostalCode,
            garage.Street, garage.Number, garage.Complement, garage.Neighborhood, garage.City, garage.State,
            garage.LogoStorageKey, garage.PrimaryColor, garage.Active, garage.CreatedAt);
    }
}
