using GarageEntity = global::SemBroncaAI.Garage.Domain.Entities.Garage;

namespace SemBroncaAI.Garage.Domain.Interfaces;

public interface IGarageRepository
{
    Task AddAsync(
        GarageEntity garage,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByDocumentAsync(
        string document,
        CancellationToken cancellationToken = default);
}