using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SemBroncaAI.Garage.Application.Abstractions.Security;

namespace SemBroncaAI.Garage.Infrastructure.Identity;

public sealed class ApplicationUserClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole<Guid>>(userManager, roleManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        if (user.GarageId is not null)
            identity.AddClaim(new Claim("garage_id", user.GarageId.Value.ToString()));
        var roles = identity.FindAll(identity.RoleClaimType).Select(claim => claim.Value);
        foreach (var permission in RolePermissionDefaults.ForRoles(roles))
            identity.AddClaim(new Claim(ApplicationPermissions.ClaimType, permission));
        return identity;
    }
}
