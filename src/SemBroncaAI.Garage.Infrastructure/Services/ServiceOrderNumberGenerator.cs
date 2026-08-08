using Microsoft.EntityFrameworkCore;
using SemBroncaAI.Garage.Domain.Interfaces;
using SemBroncaAI.Garage.Infrastructure.Persistence;

namespace SemBroncaAI.Garage.Infrastructure.Services;

public sealed class ServiceOrderNumberGenerator
    : IServiceOrderNumberGenerator
{
    private readonly GarageDbContext _context;

    public ServiceOrderNumberGenerator(GarageDbContext context)
    {
        _context = context;
    }

    public async Task<int> GetNextAsync(
        Guid garageId,
        CancellationToken cancellationToken = default)
    {
        if (garageId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador da oficina é obrigatório.",
                nameof(garageId));
        }

        var lastNumber = await _context.ServiceOrders
            .Where(x => x.GarageId == garageId)
            .MaxAsync(
                x => (int?)x.Number,
                cancellationToken);

        return (lastNumber ?? 0) + 1;
    }
}