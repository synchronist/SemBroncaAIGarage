using System.Security.Claims;
using SemBroncaAI.Garage.Application.Abstractions.Security;
using SemBroncaAI.Garage.Infrastructure.Identity;

namespace SemBroncaAI.Garage.Api.Services;

public sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser, ICurrentGarage
{
    private ClaimsPrincipal Principal => accessor.HttpContext?.User ?? new ClaimsPrincipal();

    public bool IsAuthenticated => Principal.Identity?.IsAuthenticated == true;
    public Guid UserId => Guid.TryParse(Principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ? id : Guid.Empty;
    public Guid? GarageId => Guid.TryParse(Principal.FindFirstValue("garage_id"), out var id)
        ? id : null;
    public bool IsPlatformAdmin => Principal.IsInRole(ApplicationRoles.PlatformAdmin);
    public IReadOnlyCollection<string> Roles => Principal.FindAll(ClaimTypes.Role)
        .Select(claim => claim.Value).Distinct(StringComparer.Ordinal).ToArray();

    public Guid RequireGarageId()
    {
        if (!IsAuthenticated || UserId == Guid.Empty || IsPlatformAdmin || GarageId is null)
            throw new UnauthorizedAccessException("O usuário não possui contexto de oficina válido.");
        return GarageId.Value;
    }
}
