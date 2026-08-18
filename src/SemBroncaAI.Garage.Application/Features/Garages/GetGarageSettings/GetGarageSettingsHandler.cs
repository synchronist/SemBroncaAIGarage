using SemBroncaAI.Garage.Application.Abstractions.Persistence;

namespace SemBroncaAI.Garage.Application.Features.Garages.GetGarageSettings;

public sealed class GetGarageSettingsHandler(IGarageRepository repository)
{
    public async Task<GetGarageSettingsResponse?> HandleAsync(Guid garageId, CancellationToken cancellationToken = default)
    {
        var garage = await repository.GetByIdAsync(garageId, cancellationToken);
        return garage is null ? null : new(garage.Id, garage.Name, garage.Document, garage.Phone, garage.Email,
            garage.PostalCode, garage.Street, garage.Number, garage.Complement, garage.Neighborhood, garage.City,
            garage.State, garage.LogoStorageKey, garage.PrimaryColor, garage.Active, garage.CreatedAt);
    }

    public async Task<GetGarageContextResponse?> HandleContextAsync(Guid garageId, CancellationToken cancellationToken = default)
    {
        var garage = await repository.GetByIdAsync(garageId, cancellationToken);
        return garage is null ? null : new(garage.Name, garage.Document, garage.Phone, garage.Email,
            garage.PostalCode, garage.Street, garage.Number, garage.Complement, garage.Neighborhood,
            garage.City, garage.State, garage.LogoStorageKey, garage.PrimaryColor);
    }

    public async Task<GetGarageBrandingResponse?> HandleBrandingAsync(Guid garageId, CancellationToken cancellationToken = default)
    {
        var garage = await repository.GetByIdAsync(garageId, cancellationToken);
        return garage is null ? null : new(garage.Name, garage.City, garage.State, garage.LogoStorageKey, garage.PrimaryColor);
    }
}
