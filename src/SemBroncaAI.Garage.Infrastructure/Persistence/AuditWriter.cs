using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Application.Abstractions.Security;
using SemBroncaAI.Garage.Domain.Entities;

namespace SemBroncaAI.Garage.Infrastructure.Persistence;

public sealed class AuditWriter(GarageDbContext context, ICurrentUser currentUser) : IAuditWriter
{
    public void Add(Guid? garageId, string action, string entityType, string entityId, string? summary = null)
    {
        var actorContext = currentUser.Roles.OrderBy(x => x, StringComparer.Ordinal).FirstOrDefault() ?? "AuthenticatedUser";
        context.AuditEntries.Add(new AuditEntryEntity(
            DateTime.UtcNow, currentUser.UserId, actorContext, garageId, action, entityType, entityId, summary));
    }
}
