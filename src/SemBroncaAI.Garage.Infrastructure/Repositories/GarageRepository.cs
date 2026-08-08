using Microsoft.EntityFrameworkCore;
using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Domain.Entities.Garage;
using SemBroncaAI.Garage.Infrastructure.Persistence;

namespace SemBroncaAI.Garage.Infrastructure.Repositories;

public sealed class GarageRepository : IGarageRepository
{
    private readonly GarageDbContext _dbContext;

    public GarageRepository(GarageDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        GarageEntity garage,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Garages.AddAsync(
            garage,
            cancellationToken);
    }

    public Task<bool> ExistsByDocumentAsync(
        string document,
        Guid? excludingGarageId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Garages.AnyAsync(
            garage => garage.Document == document &&
                      (!excludingGarageId.HasValue || garage.Id != excludingGarageId.Value),
            cancellationToken);
    }

    public Task<GarageEntity?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Garages.FirstOrDefaultAsync(garage => garage.Id == id, cancellationToken);

    public async Task<GarageEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Garages
            .AsNoTracking()
            .FirstOrDefaultAsync(
                garage => garage.Id == id,
                cancellationToken);

    }


    public async Task<IReadOnlyList<GarageEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Garages
            .AsNoTracking()
            .OrderBy(garage => garage.Name)
            .ToListAsync(cancellationToken);
    }
}
