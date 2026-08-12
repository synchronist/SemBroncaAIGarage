using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SemBroncaAI.Garage.Infrastructure.Persistence;

namespace SemBroncaAI.Garage.Infrastructure.Identity;

public sealed class IdentityDevelopmentSeedStore(
    GarageDbContext context,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager) : IDevelopmentIdentitySeedStore
{
    public Task<bool> GarageExistsAsync(Guid garageId, CancellationToken cancellationToken) =>
        context.Garages.AsNoTracking().AnyAsync(x => x.Id == garageId, cancellationToken);

    public async Task EnsureRoleAsync(string role, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (await roleManager.RoleExistsAsync(role)) return;
        EnsureSucceeded(await roleManager.CreateAsync(new IdentityRole<Guid>(role)), $"criar a role {role}");
    }

    public async Task<ApplicationUser?> FindUserAsync(string email, string userName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await userManager.FindByEmailAsync(email) ?? await userManager.FindByNameAsync(userName);
    }

    public async Task<ApplicationUser> CreateOwnerAsync(DevelopmentIdentitySeedOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = ApplicationUser.CreateGarageUser(
            options.OwnerName, options.OwnerEmail, options.OwnerUserName, options.GarageId);
        user.EmailConfirmed = true;
        user.LockoutEnabled = true;
        EnsureSucceeded(await userManager.CreateAsync(user, options.OwnerPassword), "criar o Owner de Development");
        return user;
    }

    public async Task EnsureOwnerRoleAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            EnsureSucceeded(await userManager.UpdateAsync(user), "confirmar o e-mail do Owner de Development");
        }
        if (await userManager.IsInRoleAsync(user, ApplicationRoles.Owner)) return;
        EnsureSucceeded(await userManager.AddToRoleAsync(user, ApplicationRoles.Owner), "associar a role Owner");
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (result.Succeeded) return;
        throw new InvalidOperationException($"Não foi possível {operation}: {string.Join("; ", result.Errors.Select(x => x.Description))}");
    }
}
