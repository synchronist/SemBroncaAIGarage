using Microsoft.EntityFrameworkCore;
using GarageEntity = global::SemBroncaAI.Garage.Domain.Entities.Garage;
using SemBroncaAI.Garage.Domain.Interfaces;
using SemBroncaAI.Garage.Infrastructure.Persistence;

namespace SemBroncaAI.Garage.Infrastructure.Repositories;

public sealed class GarageRepository : IGarageRepository
{
    private readonly GarageDbContext _context;

    public GarageRepository(GarageDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        GarageEntity garage,
        CancellationToken cancellationToken = default)
    {
        await _context.Garages.AddAsync(garage, cancellationToken);
    }

    public async Task<bool> ExistsByDocumentAsync(
        string document,
        CancellationToken cancellationToken = default)
    {
        return await _context.Garages
            .AnyAsync(x => x.Document == document, cancellationToken);
    }
}