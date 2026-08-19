using Microsoft.AspNetCore.Identity;

namespace SemBroncaAI.Garage.Infrastructure.Identity;

public sealed class ApplicationIdentityRoleInitializer(RoleManager<IdentityRole<Guid>> roleManager)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        foreach (var role in ApplicationRoles.All)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await roleManager.RoleExistsAsync(role)) continue;

            var result = await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            if (!result.Succeeded)
                throw new InvalidOperationException($"Não foi possível inicializar a role {role}.");
        }
    }
}
