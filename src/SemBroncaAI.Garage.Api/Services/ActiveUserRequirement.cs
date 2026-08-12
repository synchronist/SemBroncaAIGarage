using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SemBroncaAI.Garage.Infrastructure.Identity;
using SemBroncaAI.Garage.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace SemBroncaAI.Garage.Api.Services;

public sealed class ActiveUserRequirement : IAuthorizationRequirement;

public sealed class ActiveUserAuthorizationHandler(
    UserManager<ApplicationUser> userManager,
    GarageDbContext dbContext,
    IOptions<IdentityOptions> identityOptions) : AuthorizationHandler<ActiveUserRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ActiveUserRequirement requirement)
    {
        var user = await userManager.GetUserAsync(context.User);
        if (user is null || !user.Active)
            return;

        var stampClaim = context.User.FindFirst(
            identityOptions.Value.ClaimsIdentity.SecurityStampClaimType)?.Value;
        var currentStamp = await userManager.GetSecurityStampAsync(user);
        if (string.IsNullOrEmpty(stampClaim) ||
            !string.Equals(stampClaim, currentStamp, StringComparison.Ordinal))
            return;

        var roles = await userManager.GetRolesAsync(user);
        if (roles.Contains(ApplicationRoles.PlatformAdmin, StringComparer.Ordinal))
        {
            if (user.GarageId is null)
                context.Succeed(requirement);
            return;
        }

        if (user.GarageId is not null && await dbContext.Garages.AsNoTracking()
            .AnyAsync(garage => garage.Id == user.GarageId.Value))
        {
            context.Succeed(requirement);
        }
    }
}
