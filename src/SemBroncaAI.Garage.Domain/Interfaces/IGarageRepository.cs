using SemBroncaAI.Garage.Domain.Entities.Garage;

namespace SemBroncaAI.Garage.Application.Abstractions.Persistence;

public interface IGarageRepository
{
    Task AddAsync(
        GarageEntity garage,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByDocumentAsync(
        string document,
        Guid? excludingGarageId = null,
        CancellationToken cancellationToken = default);

    Task<GarageEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<GarageEntity?> GetForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GarageEntity>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
