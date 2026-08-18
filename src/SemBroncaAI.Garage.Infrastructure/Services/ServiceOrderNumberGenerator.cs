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

        var values = await _context.Database
            .SqlQuery<int>($$"""
                INSERT INTO "ServiceOrderNumberSequences" ("GarageId", "LastNumber")
                VALUES (
                    {{garageId}},
                    COALESCE((SELECT MAX("Number") FROM "ServiceOrders" WHERE "GarageId" = {{garageId}}), 0) + 1)
                ON CONFLICT ("GarageId") DO UPDATE
                SET "LastNumber" = GREATEST(
                    "ServiceOrderNumberSequences"."LastNumber" + 1,
                    COALESCE((SELECT MAX("Number") FROM "ServiceOrders" WHERE "GarageId" = {{garageId}}), 0) + 1)
                RETURNING "LastNumber" AS "Value"
                """)
            .ToListAsync(cancellationToken);

        return values.Single();
    }
}
